namespace Norse.Abstractions.Migrations;

/// <summary>
/// Defines a contract for a migration contributor that can execute database schema changes.
/// </summary>
public interface IMigrationContributor
{
	/// <summary>
	/// Gets the name of the migration contributor.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Executes the migration asynchronously.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A task representing the asynchronous migration operation.</returns>
	Task MigrateAsync(CancellationToken cancellationToken);
}
