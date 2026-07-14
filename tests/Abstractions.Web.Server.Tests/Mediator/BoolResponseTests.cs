using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class BoolResponseTests
{
	[Fact]
	void Value_round_trips()
	{
		var response = new BoolResponse { Value = true };

		response.Value.ShouldBeTrue();
	}
}
