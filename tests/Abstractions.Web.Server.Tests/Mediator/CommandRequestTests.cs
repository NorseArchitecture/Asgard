using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class CommandRequestTests
{
	[Fact]
	void Derived_command_is_assignable_to_ICommandRequest_of_its_response_type()
	{
		ICommandRequest<string> command = new PingCommand(new PingWire("hello"));

		command.ShouldBeAssignableTo<ICommandRequest<string>>();
	}

	[Fact]
	void Request_property_carries_the_wrapped_instance()
	{
		var wire = new PingWire("hello");
		var command = new PingCommand(wire);

		command.Request.ShouldBe(wire);
	}

	sealed record PingWire(string Message);

	sealed record PingCommand(PingWire Request) : CommandRequest<PingWire, string>(Request);
}
