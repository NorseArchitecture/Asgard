using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class ReauthenticationLandingAttributeTests
{
	public static TheoryData<string> NonLocalPaths =>
	[
		"",
		"Account/Manage/Passkeys",
		"//evil.example",
		"/\\evil.example",
		"/\t/evil.example",
		"https://evil.example/",
	];

	[Fact]
	void A_local_path_is_kept() =>
		new ReauthenticationLandingAttribute("/Account/Manage/Passkeys").Path.ShouldBe("/Account/Manage/Passkeys");

	[Theory]
	[MemberData(nameof(NonLocalPaths))]
	void A_non_local_path_is_refused(string path) =>
		Should.Throw<ArgumentException>(() => new ReauthenticationLandingAttribute(path));
}
