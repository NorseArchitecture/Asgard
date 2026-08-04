using Norse.Abstractions.Backend.Keys;

namespace Norse.Abstractions.Backend.Tests.Keys;

public sealed class SubjectCryptoScopeTests
{
	[Fact]
	void Current_subject_is_null_outside_any_scope() =>
		SubjectCryptoScope.CurrentSubject.ShouldBeNull();

	[Fact]
	void Begin_establishes_and_dispose_restores_the_ambient_subject()
	{
		var outer = Guid.NewGuid();
		var inner = Guid.NewGuid();
		using (SubjectCryptoScope.Begin(outer))
		{
			SubjectCryptoScope.CurrentSubject.ShouldBe(outer);
			using (SubjectCryptoScope.Begin(inner))
				SubjectCryptoScope.CurrentSubject.ShouldBe(inner);
			SubjectCryptoScope.CurrentSubject.ShouldBe(outer);
		}
		SubjectCryptoScope.CurrentSubject.ShouldBeNull();
	}

	[Fact]
	async Task Ambient_subject_flows_across_await()
	{
		var subject = Guid.NewGuid();
		using (SubjectCryptoScope.Begin(subject))
		{
			await Task.Yield();
			SubjectCryptoScope.CurrentSubject.ShouldBe(subject);
		}
	}

	[Fact]
	void Disposing_a_scope_twice_is_a_no_op_and_does_not_corrupt_the_ambient_value()
	{
		var subject = Guid.NewGuid();
		var scope = SubjectCryptoScope.Begin(subject);
		scope.Dispose();
		SubjectCryptoScope.CurrentSubject.ShouldBeNull();

		Should.NotThrow(scope.Dispose);
		SubjectCryptoScope.CurrentSubject.ShouldBeNull();
	}

	[Fact]
	void Disposing_an_outer_scope_while_an_inner_scope_is_still_open_throws()
	{
		var outer = Guid.NewGuid();
		var inner = Guid.NewGuid();
		var outerScope = SubjectCryptoScope.Begin(outer);
		using var innerScope = SubjectCryptoScope.Begin(inner);

		Should.Throw<InvalidOperationException>(outerScope.Dispose);
	}
}
