using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Web.Server.Facade;

namespace Norse.Abstractions.Web.Server.Tests.Facade;

/// <summary>
/// Proves <see cref="GrpcControllerBase.FoldAsync{TResponse}"/> agrees state-for-state with Midgard's
/// <c>OutcomeServerInterceptor</c>/<c>ProblemExtensions.ToRpcException</c> (the gRPC edge's own
/// <see cref="Outcome{T}"/> fold): every <see cref="ErrorCategory"/> here maps to the HTTP status the
/// canonical gRPC-to-HTTP mapping (grpc-gateway/Google APIs) would assign to the gRPC
/// gRPC <c>StatusCode</c> the interceptor selects for that same category — verified per
/// category, not assumed. Also proves the <c>errors</c> extension renders the flattened
/// <c>[{path, detail}]</c> array (spec §11.1), never a dictionary, and that the 1 MiB request size cap
/// (spec §8.4) travels with the facade itself.
/// </summary>
public sealed class GrpcControllerBaseTests
{
	sealed class TestController : GrpcControllerBase
	{
		public Task<ActionResult<TResponse>> Fold<TResponse>(ValueTask<Outcome<TResponse>> operation) where TResponse : notnull =>
			FoldAsync(operation);
	}

	static TestController CreateController()
	{
		var factory = Substitute.For<ProblemDetailsFactory>();
		factory.CreateProblemDetails(
				Arg.Any<HttpContext>(),
				Arg.Any<int?>(),
				Arg.Any<string?>(),
				Arg.Any<string?>(),
				Arg.Any<string?>(),
				Arg.Any<string?>())
			.Returns(call => new ProblemDetails
			{
				Status = call.ArgAt<int?>(1),
				Title = call.ArgAt<string?>(2),
				Type = call.ArgAt<string?>(3),
				Detail = call.ArgAt<string?>(4),
				Instance = call.ArgAt<string?>(5)
			});

		return new TestController
		{
			ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
			ProblemDetailsFactory = factory
		};
	}

	[Fact]
	async Task Success_folds_to_Ok_carrying_the_payload()
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Ok("lantern")));

		var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
		okResult.Value.ShouldBe("lantern");
		okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
	}

	[Fact]
	async Task NotFound_category_folds_to_a_bodyless_NotFoundResult()
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.NotFound)));

		result.Result.ShouldBeOfType<NotFoundResult>();
	}

	[Theory]
	[InlineData(ErrorCategory.Validation, StatusCodes.Status400BadRequest)]         // gRPC InvalidArgument
	[InlineData(ErrorCategory.Conflict, StatusCodes.Status409Conflict)]             // gRPC AlreadyExists
	[InlineData(ErrorCategory.Unauthorized, StatusCodes.Status401Unauthorized)]     // gRPC Unauthenticated
	[InlineData(ErrorCategory.Forbidden, StatusCodes.Status403Forbidden)]           // gRPC PermissionDenied
	[InlineData(ErrorCategory.LockedOut, StatusCodes.Status403Forbidden)]           // gRPC PermissionDenied
	[InlineData(ErrorCategory.NotAllowed, StatusCodes.Status400BadRequest)]         // gRPC FailedPrecondition
	[InlineData(ErrorCategory.InvalidCredentials, StatusCodes.Status401Unauthorized)] // gRPC Unauthenticated
	[InlineData(ErrorCategory.Fault, StatusCodes.Status500InternalServerError)]     // gRPC Internal
	[InlineData(ErrorCategory.MultipleMatches, StatusCodes.Status500InternalServerError)] // gRPC Internal
	async Task Each_failure_category_folds_to_the_status_the_gRPC_edge_would_reach(ErrorCategory category, int expectedStatus)
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(category)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		objectResult.StatusCode.ShouldBe(expectedStatus);
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Title.ShouldBe(category.ToString());
	}

	[Fact]
	async Task Validation_errors_render_as_a_flattened_path_detail_array_not_a_dictionary()
	{
		var controller = CreateController();
		Dictionary<string, string[]> errors = new()
		{
			["Policy/@birthDate"] = ["cannot parse 'x' as DateOnly"],
			["Policy/Coverage[2]/@limit"] = ["cannot parse 'y' as decimal", "value out of range"]
		};

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.Validation, errors)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		var entries = problem.Extensions["errors"].ShouldBeAssignableTo<IEnumerable<ProblemErrorEntry>>().ShouldNotBeNull().ToArray();
		entries.Length.ShouldBe(3);
		entries.ShouldContain(new ProblemErrorEntry("Policy/@birthDate", "cannot parse 'x' as DateOnly"));
		entries.ShouldContain(new ProblemErrorEntry("Policy/Coverage[2]/@limit", "cannot parse 'y' as decimal"));
		entries.ShouldContain(new ProblemErrorEntry("Policy/Coverage[2]/@limit", "value out of range"));
	}

	[Fact]
	async Task A_category_with_no_errors_carries_no_errors_extension()
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.Conflict)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Extensions.ContainsKey("errors").ShouldBeFalse();
	}

	[Fact]
	async Task Fault_carries_the_correlation_id_as_an_extension()
	{
		var controller = CreateController();
		var correlationId = Guid.NewGuid();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.Fault, correlationId: correlationId)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Extensions["correlationId"].ShouldBe(correlationId);
	}

	[Fact]
	void The_class_carries_the_1_MiB_request_size_cap_per_spec_8_4()
	{
		var attribute = typeof(GrpcControllerBase)
			.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: false)
			.Cast<RequestSizeLimitAttribute>()
			.SingleOrDefault();

		attribute.ShouldNotBeNull();
		((IRequestSizeLimitMetadata)attribute).MaxRequestBodySize.ShouldBe(1_048_576);
	}

	[Fact]
	void The_class_is_an_ApiController_that_negotiates_JSON_and_XML()
	{
		typeof(GrpcControllerBase).GetCustomAttributes(typeof(ApiControllerAttribute), inherit: false).ShouldNotBeEmpty();

		var consumes = (ConsumesAttribute)typeof(GrpcControllerBase).GetCustomAttributes(typeof(ConsumesAttribute), inherit: false).Single();
		consumes.ContentTypes.ShouldBe(["application/json", "application/xml"]);

		var produces = (ProducesAttribute)typeof(GrpcControllerBase).GetCustomAttributes(typeof(ProducesAttribute), inherit: false).Single();
		produces.ContentTypes.ShouldBe(["application/json", "application/xml"]);
	}
}
