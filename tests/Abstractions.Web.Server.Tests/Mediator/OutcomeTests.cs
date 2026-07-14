using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class OutcomeTests
{
	[Fact]
	void Ok_sets_IsSuccess_true_and_no_problem()
	{
		var outcome = Outcome.Ok();

		outcome.IsSuccess.ShouldBeTrue();
		outcome.Problem.ShouldBeNull();
	}

	[Fact]
	void Err_sets_IsSuccess_false_and_carries_the_category()
	{
		var outcome = Outcome.Err(ErrorCategory.Conflict);

		outcome.IsSuccess.ShouldBeFalse();
		outcome.Problem.ShouldNotBeNull();
		outcome.Problem.Category.ShouldBe(ErrorCategory.Conflict);
	}

	[Fact]
	void Err_carries_field_keyed_errors_when_provided()
	{
		var errors = new Dictionary<string, string[]> { ["Email"] = ["'Email' must not be empty."] };

		var outcome = Outcome.Err(ErrorCategory.Validation, errors);

		outcome.Problem!.Errors["Email"].ShouldBe(["'Email' must not be empty."]);
	}

	[Fact]
	void Generic_Ok_carries_the_value()
	{
		var outcome = Outcome<int>.Ok(42);

		outcome.IsSuccess.ShouldBeTrue();
		outcome.Value.ShouldBe(42);
	}

	[Fact]
	void Generic_Err_carries_no_value()
	{
		var outcome = Outcome<int>.Err(ErrorCategory.NotFound);

		outcome.IsSuccess.ShouldBeFalse();
		outcome.Value.ShouldBe(0);
		outcome.Problem!.Category.ShouldBe(ErrorCategory.NotFound);
	}
}
