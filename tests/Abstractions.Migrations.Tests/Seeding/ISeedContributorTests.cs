using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Migrations.Seeding;

namespace Norse.Abstractions.Migrations.Tests.Seeding;

public sealed class ISeedContributorTests
{
	[Fact]
	async Task SeedAsync_invokes_concrete_implementation()
	{
		StubSeedContributor stub = new();

		await stub.SeedAsync(CancellationToken.None);

		stub.Invoked.ShouldBeTrue();
	}

	[Fact]
	void Name_returns_concrete_value()
	{
		StubSeedContributor stub = new();

		stub.Name.ShouldBe("Stub");
	}

	[Fact]
	void ConfigureServices_is_callable_as_static_interface_member_with_explicit_override()
	{
		ServiceCollection services = new();

		StubSeedContributor.ConfigureServices(services);

		services.ShouldBeEmpty();
	}

	[Fact]
	void ConfigureServices_can_be_omitted_by_contributors_that_need_nothing()
	{
		StubSeedContributorWithoutOverride stub = new();

		stub.Name.ShouldBe("StubNoOverride");
	}

	sealed class StubSeedContributor : ISeedContributor
	{
		public string Name => "Stub";
		public bool Invoked { get; private set; }

		public Task SeedAsync(CancellationToken cancellationToken)
		{
			Invoked = true;
			return Task.CompletedTask;
		}

		public static void ConfigureServices(IServiceCollection services) { }
	}

	sealed class StubSeedContributorWithoutOverride : ISeedContributor
	{
		public string Name => "StubNoOverride";

		public Task SeedAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
