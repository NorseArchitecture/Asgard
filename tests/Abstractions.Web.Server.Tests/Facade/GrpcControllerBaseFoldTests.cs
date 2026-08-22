using Microsoft.AspNetCore.Mvc;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Web.Server.Facade;

namespace Norse.Abstractions.Web.Server.Tests.Facade;

public sealed class GrpcControllerBaseFoldTests
{
	sealed class Probe : GrpcControllerBase
	{
		public Task<ActionResult<string>> Fold(Outcome<string> outcome) =>
			FoldAsync(new ValueTask<Outcome<string>>(outcome));
	}

	static Outcome<string> Failure(ErrorCategory category) =>
		Outcome<string>.Err(category, new Dictionary<string, string[]> { [""] = ["leaked detail"] });

	[Theory]
	[InlineData(ErrorCategory.Unauthorized)]
	[InlineData(ErrorCategory.InvalidCredentials)]
	async Task Silent_categories_fold_to_a_bare_status_with_no_body(ErrorCategory category)
	{
		var result = await new Probe().Fold(Failure(category));

		var bare = result.Result.ShouldBeOfType<StatusCodeResult>();
		bare.StatusCode.ShouldBe(401);
	}

	[Fact]
	async Task NotAllowed_folds_to_403_with_a_body()
	{
		var result = await new Probe().Fold(Failure(ErrorCategory.NotAllowed));

		var problem = result.Result.ShouldBeOfType<ObjectResult>();
		problem.StatusCode.ShouldBe(403);
	}

	[Fact]
	async Task Every_category_folds_to_its_declared_http_status()
	{
		foreach (var category in Enum.GetValues<ErrorCategory>().Where(c => c != ErrorCategory.Unspecified))
		{
			var expected = TransportDispositions.For(category).HttpStatus;
			var result = await new Probe().Fold(Failure(category));

			var status = result.Result switch
			{
				// NotFoundResult derives from StatusCodeResult, so a dedicated arm for it here would be
				// unreachable (CS8510) -- the StatusCodeResult arm above already catches it with the
				// same StatusCode value.
				StatusCodeResult bare => bare.StatusCode,
				ObjectResult obj => obj.StatusCode,
				_ => -1
			};
			status.ShouldBe(expected, $"{category} folded to {status}, expected {expected}");
		}
	}
}
