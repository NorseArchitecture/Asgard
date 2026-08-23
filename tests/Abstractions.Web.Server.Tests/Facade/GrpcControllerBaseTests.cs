using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Norse.Abstractions.Components.Authorization;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Web.Server.Facade;

namespace Norse.Abstractions.Web.Server.Tests.Facade;

/// <summary>
///     Proves <see cref="GrpcControllerBase.FoldAsync{TResponse}" /> agrees state-for-state with Midgard's
///     <c>OutcomeServerInterceptor</c>/<c>ProblemExtensions.ToRpcException</c> (the gRPC edge's own
///     <see cref="Outcome{T}" /> fold): every <see cref="ErrorCategory" /> here maps to the HTTP status the
///     canonical gRPC-to-HTTP mapping (grpc-gateway/Google APIs) would assign to the gRPC
///     gRPC <c>StatusCode</c> the interceptor selects for that same category — verified per
///     category, not assumed. Also proves the <c>errors</c> extension renders the flattened
///     <c>[{path, detail}]</c> array (spec §11.1), never a dictionary, and that the 1 MiB request size cap
///     (spec §8.4) travels with the facade itself.
/// </summary>
public sealed class GrpcControllerBaseTests
{
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
	async Task NotFound_category_folds_to_a_bare_404_with_no_body()
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.NotFound)));

		var bare = result.Result.ShouldBeOfType<StatusCodeResult>();
		bare.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
	}

	[Theory]
	[InlineData(ErrorCategory.Validation, StatusCodes.Status400BadRequest)] // gRPC InvalidArgument
	[InlineData(ErrorCategory.Conflict, StatusCodes.Status409Conflict)] // gRPC AlreadyExists
	[InlineData(ErrorCategory.Forbidden, StatusCodes.Status403Forbidden)] // gRPC PermissionDenied
	[InlineData(ErrorCategory.LockedOut, StatusCodes.Status403Forbidden)] // gRPC PermissionDenied
	[InlineData(ErrorCategory.NotAllowed, StatusCodes.Status403Forbidden)] // gRPC PermissionDenied
	[InlineData(ErrorCategory.Fault, StatusCodes.Status500InternalServerError)] // gRPC Internal
	[InlineData(ErrorCategory.MultipleMatches, StatusCodes.Status500InternalServerError)] // gRPC Internal
	async Task Each_failure_category_folds_to_the_status_the_gRPC_edge_would_reach(ErrorCategory category,
		int expectedStatus)
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(category)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		objectResult.StatusCode.ShouldBe(expectedStatus);
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Title.ShouldBe(category.ToString());
	}

	[Theory]
	[InlineData(ErrorCategory.Unauthorized)] // gRPC Unauthenticated -- silent, no body (401 explains nothing)
	[InlineData(ErrorCategory.InvalidCredentials)] // gRPC Unauthenticated -- silent, no body
	async Task Silent_categories_fold_to_a_bare_401_with_no_body(ErrorCategory category)
	{
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(category)));

		var bare = result.Result.ShouldBeOfType<StatusCodeResult>();
		bare.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
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
		var entries = problem.Extensions["errors"].ShouldBeAssignableTo<IEnumerable<ProblemErrorEntry>>()
			.ShouldNotBeNull().ToArray();
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
	async Task A_failure_result_negotiates_to_the_RFC_9457_problem_media_types()
	{
		// ToProblemResult must set ContentTypes itself: without them, content negotiation considers
		// every registered output formatter for the ProblemDetails body — an XML-negotiated failure
		// would route into XmlContractOutputFormatter (no shape for ProblemDetails, throws) and a
		// plain-JSON failure would lose the problem+json signal.
		var controller = CreateController();

		var result = await controller.Fold(ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.Conflict)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		objectResult.ContentTypes.ShouldBe(["application/problem+json", "application/problem+xml"]);
	}

	[Fact]
	async Task Fault_carries_the_correlation_id_as_an_extension()
	{
		var controller = CreateController();
		var correlationId = Guid.NewGuid();

		var result =
			await controller.Fold(
				ValueTask.FromResult(Outcome<string>.Err(ErrorCategory.Fault, correlationId: correlationId)));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Extensions["correlationId"].ShouldBe(correlationId);
	}

	[Fact]
	async Task Erased_folds_to_410_gone_with_receipt_extensions()
	{
		var controller = CreateController();
		ErasureReceipt receipt = new(Guid.NewGuid(), new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));
		Outcome<string> outcome = new(new Failed(new Problem { Category = ErrorCategory.Erased, Receipt = receipt }));

		var result = await controller.Fold(ValueTask.FromResult(outcome));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		objectResult.StatusCode.ShouldBe(StatusCodes.Status410Gone);
		var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
		problem.Extensions["receipt"].ShouldBe(receipt.ReceiptId);
		problem.Extensions["severedAt"].ShouldBe("2026-08-03T12:00:00.0000000+00:00");
	}

	[Fact]
	async Task Erased_without_a_receipt_still_folds_to_410_gone()
	{
		var controller = CreateController();
		Outcome<string> outcome = new(new Failed(new Problem { Category = ErrorCategory.Erased }));

		var result = await controller.Fold(ValueTask.FromResult(outcome));

		var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
		objectResult.StatusCode.ShouldBe(StatusCodes.Status410Gone);
		objectResult.Value.ShouldBeOfType<ProblemDetails>().Extensions.ShouldNotContainKey("receipt");
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
	void The_class_requires_the_Machine_policy()
	{
		var attribute = typeof(GrpcControllerBase)
			.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
			.Cast<AuthorizeAttribute>()
			.SingleOrDefault();

		attribute.ShouldNotBeNull();
		attribute.Policy.ShouldBe(NorsePolicies.Machine);
	}

	[Fact]
	void The_class_carries_no_Swashbuckle_era_media_type_attributes()
	{
		typeof(GrpcControllerBase).GetCustomAttributes(typeof(ApiControllerAttribute), inherit: false)
			.ShouldNotBeEmpty();

		// Deliberately NO class-level [Consumes] or [Produces] -- Swashbuckle-era prior art. [Consumes]
		// is actively harmful: it doubles as IAcceptsMetadata, and endpoint routing's
		// AcceptsMatcherPolicy partitions the match DFA on request Content-Type -- a class-level stamp
		// made every bodyless GET facade action unroutable (404, no candidates) for any request without
		// a Content-Type header, proven live on Yggdrasil's composition root. [Produces] duplicated
		// what the host's formatters already own: input policing is 415 via UnsupportedContentTypeFilter,
		// output negotiation is formatter registration order plus ReturnHttpNotAcceptable, and failure
		// results set their own RFC 9457 content types in ToProblemResult.
		typeof(GrpcControllerBase).GetCustomAttributes(typeof(ConsumesAttribute), inherit: false)
			.ShouldBeEmpty();
		typeof(GrpcControllerBase).GetCustomAttributes(typeof(ProducesAttribute), inherit: false)
			.ShouldBeEmpty();
	}

	sealed class TestController : GrpcControllerBase
	{
		public Task<ActionResult<TResponse>> Fold<TResponse>(ValueTask<Outcome<TResponse>> operation)
			where TResponse : notnull =>
			FoldAsync(operation);
	}
}
