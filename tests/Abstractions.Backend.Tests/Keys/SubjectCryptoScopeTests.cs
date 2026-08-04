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
}
