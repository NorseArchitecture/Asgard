using FluentValidation;
using FluentValidation.Results;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
///     The pass-through adapter that lets a <see cref="CommandRequest{TRequest,TResponse}" /> reuse its
///     wrapped wire type's own validators unchanged — resolved and run against
///     <see cref="CommandRequest{TRequest,TResponse}.Request" />, with every <see cref="ValidationFailure" />
///     forwarded verbatim. Property names are never re-prefixed with <c>Request.</c> — Blazilla's
///     client-side field matching binds the wire DTO's own property names, and this adapter's whole
///     purpose is making the server-side wrapper invisible to that matching. Registered automatically
///     by <c>AddNorse{Realm}Handlers()</c> for every handled request that derives
///     <see cref="CommandRequest{TRequest,TResponse}" /> — never hand-written.
/// </summary>
/// <typeparam name="TCommand">The wrapper command type being validated.</typeparam>
/// <typeparam name="TRequest">The wire DTO <typeparamref name="TCommand" /> wraps.</typeparam>
/// <typeparam name="TResponse">The handler's payload type.</typeparam>
public sealed class CommandRequestValidator<TCommand, TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
	:
		AbstractValidator<TCommand>
	where TCommand : CommandRequest<TRequest, TResponse>
	where TRequest : notnull
	where TResponse : notnull
{
	/// <summary>
	///     Runs every registered <see cref="IValidator{TRequest}" /> against the wrapped wire request and
	///     aggregates their failures verbatim — an empty validator collection is a valid command, exactly
	///     as an empty collection is a valid request in <c>ValidationBehavior</c> (absence is <c>[]</c>,
	///     not an error).
	/// </summary>
	public override async Task<ValidationResult> ValidateAsync(ValidationContext<TCommand> context,
		CancellationToken cancellation = default)
	{
		List<ValidationFailure> failures = [];
		foreach (var validator in validators)
		{
			var result = await validator.ValidateAsync(context.InstanceToValidate.Request, cancellation)
				.ConfigureAwait(false);
			failures.AddRange(result.Errors);
		}

		return new ValidationResult(failures);
	}

	/// <summary>
	///     This adapter only ever runs behind the async mediator pipeline (<c>ValidationBehavior</c>
	///     calls <see cref="ValidateAsync(ValidationContext{TCommand}, CancellationToken)" /> exclusively)
	///     — the wrapped wire validators may carry async rules (Blazilla's async validation mode), so
	///     there is no synchronous equivalent to fall back to. Failing loudly here beats silently
	///     reporting "valid" for a command whose wire validator never actually ran.
	/// </summary>
	public override ValidationResult Validate(ValidationContext<TCommand> context) =>
		throw new NotSupportedException(
			$"{nameof(CommandRequestValidator<,,>)} is async-only — call {nameof(ValidateAsync)} instead.");
}
