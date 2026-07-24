#pragma warning disable IDE0005 // Using directive is unnecessary
using System.Runtime.CompilerServices;
using Norse.Abstractions.Contracts;
using Norse.Primitives;
#pragma warning restore IDE0005

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

	[Fact]
	void OutcomeOfT_Default_ThrowsOnMatch_MalformedByConstruction()
	{
		var defaulted = default(Outcome<BoolResponse>);
		Should.Throw<SwitchExpressionException>(() => defaulted.Match(value => value.Value, _ => false));
	}
}
