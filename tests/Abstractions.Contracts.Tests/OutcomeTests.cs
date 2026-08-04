using Norse.Primitives;

namespace Norse.Abstractions.Contracts.Tests;

public sealed class OutcomeTests
{
	[Fact]
	void OutcomeOfUnit_Ok_IsTheVoidSuccessShape()
	{
		// Explicit Outcome<Unit> here — this test doesn't need the alias in scope to prove the type
		// itself works; the alias (GlobalUsings.Outcome.cs, copied into consumers as needed, e.g.
		// Task 12) is an ergonomic spelling concern for downstream call sites, not this type's own test.
		var outcome = Outcome<Unit>.Ok(Unit.Value);
		var matched = outcome switch { Success<Unit> => true, Failed => false };
		matched.ShouldBeTrue();
	}

	[Fact]
	void OutcomeOfT_Ok_TryGetValue_UnwrapsSuccessWithoutBoxing()
	{
		var outcome = Outcome<BoolResponse>.Ok(new BoolResponse { Value = true });
		outcome.TryGetValue(out Success<BoolResponse> success).ShouldBeTrue();
		success.Value.Value.ShouldBeTrue();
	}

	[Fact]
	void OutcomeOfT_Err_CarriesCategoryAndCorrelationId()
	{
		var correlationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
		var outcome = Outcome<BoolResponse>.Err(
			ErrorCategory.Fault,
			errors: new Dictionary<string, string[]>(),
			correlationId: correlationId);

		outcome.TryGetValue(out Failed failed).ShouldBeTrue();
		failed.Problem.Category.ShouldBe(ErrorCategory.Fault);
		failed.Problem.CorrelationId.ShouldBe(correlationId);
	}

	[Fact]
	void OutcomeOfT_Err_CarriesReceipt_WhenErased()
	{
		var receipt = new ErasureReceipt(Guid.Parse("22222222-2222-2222-2222-222222222222"), DateTimeOffset.UtcNow);
		var outcome = Outcome<BoolResponse>.Err(ErrorCategory.Erased, receipt: receipt);

		outcome.TryGetValue(out Failed failed).ShouldBeTrue();
		failed.Problem.Receipt.ShouldBe(receipt);
	}

	[Fact]
	void OutcomeOfT_Err_ReceiptDefaultsToNull_WhenOmitted()
	{
		var outcome = Outcome<BoolResponse>.Err(ErrorCategory.NotFound);

		outcome.TryGetValue(out Failed failed).ShouldBeTrue();
		failed.Problem.Receipt.ShouldBeNull();
	}

	[Fact]
	void OutcomeOfT_Match_ExhaustiveOverBothCases()
	{
		var success = Outcome<BoolResponse>.Ok(new BoolResponse { Value = true });
		var failure = Outcome<BoolResponse>.Err(ErrorCategory.NotFound);

		success.Match(value => value.Value, _ => false).ShouldBeTrue();
		failure.Match(value => value.Value, problem => problem.Category == ErrorCategory.NotFound).ShouldBeTrue();
	}

	[Fact]
	void ErrorCategory_HasNineMembers_ExplicitValues()
	{
		((byte)ErrorCategory.Validation).ShouldBe((byte)1);
		((byte)ErrorCategory.NotFound).ShouldBe((byte)2);
		((byte)ErrorCategory.Conflict).ShouldBe((byte)3);
		((byte)ErrorCategory.LockedOut).ShouldBe((byte)4);
		((byte)ErrorCategory.InvalidCredentials).ShouldBe((byte)5);
		((byte)ErrorCategory.NotAllowed).ShouldBe((byte)6);
		((byte)ErrorCategory.Unauthorized).ShouldBe((byte)7);
		((byte)ErrorCategory.Forbidden).ShouldBe((byte)8);
		((byte)ErrorCategory.Fault).ShouldBe((byte)9);
	}

	[Fact]
	void OutcomeOfT_TryGetValue_ReturnsFalse_ForTheOtherCase()
	{
		var success = Outcome<BoolResponse>.Ok(new BoolResponse { Value = true });
		success.TryGetValue(out Failed _).ShouldBeFalse();

		var failure = Outcome<BoolResponse>.Err(ErrorCategory.NotFound);
		failure.TryGetValue(out Success<BoolResponse> _).ShouldBeFalse();
	}

	// The struct-era "default(Outcome<T>) throws SwitchExpressionException on first consumption"
	// test no longer applies — Outcome<T> is a class now (spec §9 amendment), so
	// default(Outcome<BoolResponse>) is simply null, and "null is never a legitimate third state"
	// is enforced by the nullable reference type system itself (a compile-time warning/error on
	// any dereference), not a runtime SwitchExpressionException. Every real construction path
	// (Ok/Err/the implicit lift) always returns a non-null instance — verified below.

	[Fact]
	void OutcomeOfT_Ok_NeverReturnsNull()
	{
		var outcome = Outcome<BoolResponse>.Ok(new BoolResponse { Value = true });
		outcome.ShouldNotBeNull();
	}

	[Fact]
	void OutcomeOfT_Err_NeverReturnsNull()
	{
		var outcome = Outcome<BoolResponse>.Err(ErrorCategory.Fault);
		outcome.ShouldNotBeNull();
	}

	[Fact]
	void OutcomeOfT_ImplicitLift_NeverReturnsNull()
	{
		Outcome<BoolResponse> outcome = new BoolResponse { Value = true };
		outcome.ShouldNotBeNull();
	}

	// protobuf-net's SetSurrogate contract (verified against its own SurrogateForObjectUsage.cs
	// example) requires both conversion operators between a reference-typed real type and its
	// surrogate to pass null through unchanged — its deserializer round-trips a default/no-existing-
	// value merge target through these operators before populating it. Real application code can
	// never hit this branch (T is notnull), but the wire path depends on it, proven end-to-end via a
	// real hosted gRPC call in Midgard's Infrastructure.Web.Server.Tests.

	[Fact]
	void OutcomeOfT_ExplicitUnwrap_OfNull_ReturnsDefault_DoesNotThrow()
	{
		Outcome<BoolResponse>? outcome = null;
		var unwrapped = (BoolResponse?)outcome!;
		unwrapped.ShouldBeNull();
	}

	[Fact]
	void OutcomeOfT_ImplicitLift_OfNull_ReturnsNull_DoesNotThrow()
	{
		BoolResponse? value = null;
		Outcome<BoolResponse>? outcome = value!;
		outcome.ShouldBeNull();
	}
}
