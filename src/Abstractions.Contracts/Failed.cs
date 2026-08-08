namespace Norse.Abstractions.Contracts;

/// <summary>
///     The failure case of <see cref="Outcome{T}" />. Named <c>Failed</c>, not <c>Failure</c>, to avoid
///     colliding with <see cref="Norse.Primitives.Failure" /> (Svartalfheim's <c>ParseFailure</c>-shaped
///     case type, unrelated) when both namespaces are open in the same file.
/// </summary>
/// <param name="Problem">The error detail.</param>
public readonly record struct Failed(Problem Problem);
