using FluentValidation;
using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class CommandRequestValidatorTests
{
	[Fact]
	async Task Forwards_a_single_child_validators_failures_with_unprefixed_property_names()
	{
		CommandRequestValidator<LoginCommand, LoginWire, string> validator = new([new EmailValidator()]);
		var command = new LoginCommand(new LoginWire("", "irrelevant"));

		var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(f => f.PropertyName == "Email" && f.ErrorMessage == "Email is required.");
	}

	[Fact]
	async Task Aggregates_failures_across_multiple_child_validators()
	{
		CommandRequestValidator<LoginCommand, LoginWire, string> validator = new([
			new EmailValidator(), new PasswordValidator()
		]);
		var command = new LoginCommand(new LoginWire("", "short"));

		var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

		result.Errors.ShouldContain(f => f.PropertyName == "Email");
		result.Errors.ShouldContain(f => f.PropertyName == "Password");
	}

	[Fact]
	async Task Runs_async_child_rules()
	{
		CommandRequestValidator<LoginCommand, LoginWire, string> validator = new([new AsyncEmailValidator()]);
		var command = new LoginCommand(new LoginWire("not-an-email", "irrelevant"));

		var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(f => f.PropertyName == "Email" && f.ErrorMessage == "Email must contain '@'.");
	}

	[Fact]
	async Task Is_valid_when_there_are_no_child_validators()
	{
		CommandRequestValidator<LoginCommand, LoginWire, string> validator = new([]);
		var command = new LoginCommand(new LoginWire("", ""));

		var result = await validator.ValidateAsync(command, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	async Task The_adapter_runs_async_rules_declared_on_the_wire_type()
	{
		// Direct proof the async rule actually ran (a side-effecting flag) rather than inferring it
		// from a validation-result shape — the guarantee Task 10's default-set async rule rides on:
		// "single source of validation truth, run twice" only holds because this adapter's plain
		// ValidateAsync reaches every rule FluentValidation supports, sync or async, unchanged.
		var called = false;
		InlineValidator<LoginWire> wireValidator = [];
		wireValidator.RuleFor(w => w.Email).CustomAsync(async (_, _, _) =>
		{
			called = true;
			await Task.Yield();
		});
		CommandRequestValidator<LoginCommand, LoginWire, string> adapter = new([wireValidator]);
		var command = new LoginCommand(new LoginWire("irrelevant@example.com", "irrelevant"));

		await adapter.ValidateAsync(new ValidationContext<LoginCommand>(command),
			TestContext.Current.CancellationToken);

		called.ShouldBeTrue();
	}

	[Fact]
	void Sync_Validate_throws_NotSupportedException()
	{
		CommandRequestValidator<LoginCommand, LoginWire, string> validator = new([]);
		var command = new LoginCommand(new LoginWire("", ""));

		Should.Throw<NotSupportedException>(() => validator.Validate(command));
	}

	sealed record LoginWire(string Email, string Password);

	sealed record LoginCommand(LoginWire Request) : CommandRequest<LoginWire, string>(Request);

	sealed class EmailValidator : AbstractValidator<LoginWire>
	{
		public EmailValidator() =>
			RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
	}

	sealed class PasswordValidator : AbstractValidator<LoginWire>
	{
		public PasswordValidator() =>
			RuleFor(x => x.Password).MinimumLength(8).WithMessage("Password is too short.");
	}

	sealed class AsyncEmailValidator : AbstractValidator<LoginWire>
	{
		public AsyncEmailValidator() =>
			RuleFor(x => x.Email).MustAsync(async (email, cancellationToken) =>
			{
				await Task.Yield();
				return email.Contains('@', StringComparison.Ordinal);
			}).WithMessage("Email must contain '@'.");
	}
}
