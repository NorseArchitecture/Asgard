namespace Norse.Abstractions.Contracts.Tests;

public sealed class ProblemTests
{
	[Fact]
	void Erased_category_claims_the_next_explicit_value()
	{
		((byte)ErrorCategory.Erased).ShouldBe((byte)11);
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
}
