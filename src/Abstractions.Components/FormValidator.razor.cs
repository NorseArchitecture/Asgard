using Microsoft.AspNetCore.Components.Forms;

namespace Norse.Abstractions.Components;

/// <summary>
///     The only validator markup a Norse form writes: place <c>&lt;FormValidator/&gt;</c> inside an
///     <see cref="EditForm" /> and nothing else attaches validation. It wires Blazilla's
///     <c>FluentValidator</c> pass onto the cascaded <see cref="EditContext" /> and stamps the marker
///     <see cref="OutcomeFormComponentBase" /> requires before dispatch — a form rendered without it
///     fails loudly at submit rather than validating vacuously. <c>AsyncMode</c> is fixed on and
///     deliberately not a parameter: setting it false against an async rule makes Blazilla report the
///     form valid and then throw from an <c>async void</c> handler.
/// </summary>
public sealed partial class FormValidator;
