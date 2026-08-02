namespace Norse.Abstractions.Web.Server.Facade;

/// <summary>
/// One field-level failure entry in a problem response's <c>errors</c> extension member (Futhark spec
/// §11.1) — the <c>[{path, detail}]</c> array shape, deliberately not <c>ValidationProblemDetails</c>'
/// dictionary: paths repeat, and a dictionary needs value-array ceremony for no benefit.
/// <see cref="GrpcControllerBase"/> populates this from a failed <c>Outcome&lt;T&gt;</c>'s
/// <c>Problem.Errors</c>; Midgard's <c>ModelState</c>-driven 400 factory and RFC 9457 XML writer both
/// render this exact type — JSON and XML negotiate the identical payload shape by construction, never
/// by two independently maintained shapes that could drift.
/// </summary>
/// <param name="Path">
/// The failing member's path — the Futhark §11.2 grammar (<c>Policy/Coverage[2]/@limit</c>) for XML
/// read failures, the <c>ModelState</c> key for MVC binding/validation failures.
/// </param>
/// <param name="Detail">The human-readable failure detail.</param>
public sealed record ProblemErrorEntry(string Path, string Detail);
