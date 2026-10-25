namespace Norse.Abstractions.Contracts.Tests;

public sealed class ProblemTests
{
	[Fact]
	void Erased_category_claims_the_next_explicit_value() =>
		((byte)ErrorCategory.Erased).ShouldBe((byte)11);

	[Fact]
	void Indeterminate_is_twelve_and_every_member_is_explicit()
	{
		((byte)ErrorCategory.Indeterminate).ShouldBe((byte)12);
		Enum.GetValues<ErrorCategory>().ShouldBe([
			ErrorCategory.Unspecified,
			ErrorCategory.Validation,
			ErrorCategory.NotFound,
			ErrorCategory.Conflict,
			ErrorCategory.LockedOut,
			ErrorCategory.InvalidCredentials,
			ErrorCategory.NotAllowed,
			ErrorCategory.Unauthorized,
			ErrorCategory.Forbidden,
			ErrorCategory.Fault,
			ErrorCategory.MultipleMatches,
			ErrorCategory.Erased,
			ErrorCategory.Indeterminate
		]);
		Enum.GetValues<ErrorCategory>()
			.Select(category => (byte)category)
			.ShouldBe(Enumerable.Range(0, 13).Select(value => (byte)value));
	}

	[Fact]
	void Problem_carries_an_optional_erasure_receipt()
	{
		ErasureReceipt receipt = new(Guid.NewGuid(), DateTimeOffset.UtcNow);
		Problem problem = new() { Category = ErrorCategory.Erased, Receipt = receipt };
		problem.Receipt.ShouldBe(receipt);
	}

	[Fact]
	void Receipt_defaults_to_null_for_every_other_category()
	{
		Problem problem = new() { Category = ErrorCategory.NotFound };
		problem.Receipt.ShouldBeNull();
	}

	[Fact]
	void Model_error_carries_the_single_message_under_the_empty_key()
	{
		var problem = Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password.");

		problem.Category.ShouldBe(ErrorCategory.InvalidCredentials);
		problem.Errors.ShouldHaveSingleItem();
		problem.Errors[string.Empty].ShouldBe(["Invalid email or password."]);
	}
}
