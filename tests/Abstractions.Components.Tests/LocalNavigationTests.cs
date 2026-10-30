namespace Norse.Abstractions.Components.Tests;

public sealed class LocalNavigationTests
{
	public static TheoryData<string> Locals =>
	[
		"/Account/Login?returnUrl=%2FAccount%2FManage%2FPasskeys",
		"/app/x",
		$"/{new string('a', 2047)}",
	];

	public static TheoryData<string> Refused =>
	[
		"",
		"x",
		"//evil.example",
		"/\\evil.example",
		"/\t/evil.example",
		"/\n/evil.example",
		"/\r/evil.example",
		"/\u0085/evil.example",
		" /x",
		"/%2F/evil.example",
		"/%5Cevil.example",
		"/%09/evil.example",
		"https://evil.example/",
		$"/{new string('a', 2048)}",
	];

	[Theory]
	[MemberData(nameof(Locals))]
	void A_single_slash_rooted_path_is_local(string url) =>
		LocalNavigation.IsLocal(url).ShouldBeTrue();

	[Theory]
	[MemberData(nameof(Refused))]
	void Anything_a_browser_could_resolve_off_site_is_refused(string url) =>
		LocalNavigation.IsLocal(url).ShouldBeFalse();

	[Fact]
	void Null_is_refused() =>
		LocalNavigation.IsLocal(null).ShouldBeFalse();
}
