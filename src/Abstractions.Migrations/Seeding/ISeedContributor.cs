namespace Norse.Abstractions.Migrations.Seeding;

/// <summary>
/// Defines a contract for a seed contributor that bootstraps required data after all migrations
/// have completed.
/// </summary>
public interface ISeedContributor
{
	/// <summary>
	/// Gets the name of the seed contributor.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Seeds data asynchronously. Invoked on every startup; the contributor is responsible for its
	/// own idempotency (e.g. checking whether a row already exists before writing it).
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A task representing the asynchronous seed operation.</returns>
	Task SeedAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Registers any services this contributor's <see cref="SeedAsync"/> needs beyond its own
	/// constructor-injected <c>DbContext</c> (e.g. <c>UserManager</c>, an OpenIddict application
	/// manager). The default implementation is a no-op; a contributor needs to override this only
	/// if it requires additional registered services.
	/// </summary>
	/// <param name="services">The service collection to register into.</param>
	static virtual void ConfigureServices(IServiceCollection services) { }
}
