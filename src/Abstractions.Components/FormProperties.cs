using Microsoft.AspNetCore.Components.Forms;

namespace Norse.Abstractions.Components;

/// <summary>
///     Keys Norse stamps into <see cref="EditContext.Properties" />.
/// </summary>
static class FormProperties
{
	/// <summary>
	///     Set by <see cref="FormValidator" /> on initialization and required by
	///     <see cref="OutcomeFormComponentBase" /> before dispatch. Norse-owned because Blazilla exposes no
	///     durable "a validator is attached" signal — its own context key exists only mid-flight and is
	///     removed on completion, and a form with no validator validates to <c>true</c> with zero messages,
	///     indistinguishable from a genuinely valid one. The marker tracks attachment for the
	///     <see cref="EditContext" />'s lifetime only — it is stamped once, on initialization, and never
	///     unstamped. A <see cref="FormValidator" /> rendered conditionally (<c>@if</c>) over a cached,
	///     longer-lived <see cref="EditContext" /> leaves the marker <c>true</c> after Blazilla's validator
	///     has unsubscribed, so the dispatch guard passes while validation is vacuous again — that shape is
	///     unsupported. Unstamping on <c>Dispose</c> was considered and rejected: it has the symmetric
	///     failure on re-show.
	/// </summary>
	internal const string ValidatorAttached = "__Norse_FormValidatorAttached";
}
