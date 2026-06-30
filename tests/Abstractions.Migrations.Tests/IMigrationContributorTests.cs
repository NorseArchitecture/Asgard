namespace Norse.Abstractions.Migrations.Tests;

public sealed class IMigrationContributorTests
{
	[Fact]
	async Task MigrateAsync_invokes_concrete_implementation()
	{
		StubContributor stub = new();

		await stub.MigrateAsync(CancellationToken.None);

		stub.Invoked.ShouldBeTrue();
	}

	[Fact]
	void Name_returns_concrete_value()
	{
		StubContributor stub = new();

		stub.Name.ShouldBe("Stub");
	}

	sealed class StubContributor : IMigrationContributor
	{
		public string Name => "Stub";
		public bool Invoked { get; private set; }

		public Task MigrateAsync(CancellationToken cancellationToken)
		{
			Invoked = true;
			return Task.CompletedTask;
		}
	}
}
