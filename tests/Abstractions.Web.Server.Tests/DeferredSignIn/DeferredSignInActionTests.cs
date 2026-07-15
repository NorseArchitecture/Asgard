using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Norse.Abstractions.Web.Server.DeferredSignIn;

namespace Norse.Abstractions.Web.Server.Tests.DeferredSignIn;

public sealed class DeferredSignInActionTests
{
	[Fact]
	void Constructor_round_trips_all_properties()
	{
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "buvy")]));
		var properties = new AuthenticationProperties { IsPersistent = true };

		var action = new DeferredSignInAction("Identity.Application", SignOut: false, principal, properties);

		action.Scheme.ShouldBe("Identity.Application");
		action.SignOut.ShouldBeFalse();
		action.Principal.ShouldBeSameAs(principal);
		action.Properties.ShouldBeSameAs(properties);
	}
}
