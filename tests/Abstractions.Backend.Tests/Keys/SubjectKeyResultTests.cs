using System.Runtime.CompilerServices;
using Norse.Abstractions.Backend.Keys;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend.Tests.Keys;

public sealed class SubjectKeyResultTests
{
	[Fact]
	void Match_routes_the_available_case()
	{
		var result = SubjectKeyResult.Available([1, 2, 3]);
		result.Match(key => key.Length, _ => -1, () => -2).ShouldBe(3);
	}

	[Fact]
	void Match_routes_the_destroyed_case_with_its_receipt()
	{
		ErasureReceipt receipt = new(Guid.NewGuid(), DateTimeOffset.UtcNow);
		var result = SubjectKeyResult.Destroyed(receipt);
		result.Match(_ => Guid.Empty, r => r.ReceiptId, () => Guid.Empty).ShouldBe(receipt.ReceiptId);
	}

	[Fact]
	void Match_routes_the_missing_case()
	{
		SubjectKeyResult.Missing.Match(_ => "available", _ => "destroyed", () => "missing").ShouldBe("missing");
	}

	[Fact]
	void Match_throws_on_the_malformed_default()
	{
		SubjectKeyResult malformed = default;
		Should.Throw<SwitchExpressionException>(() => malformed.Match(_ => 0, _ => 0, () => 0));
	}

	[Fact]
	void Available_rejects_an_empty_key()
	{
		Should.Throw<ArgumentException>(() => SubjectKeyResult.Available([]));
	}
}
