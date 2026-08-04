using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Facade;

/// <summary>
/// The shared base for every hand-authored REST facade controller (Futhark spec §4) — each injects the
/// gRPC service <em>interface</em> directly and runs it in-process, no protobuf on the path, the same
/// mediator pipeline underneath as the gRPC transport. <see cref="FoldAsync{TResponse}"/> is the single
/// landing site for the <c>Outcome&lt;T&gt;</c> → <see cref="ActionResult{TValue}"/> fold, built from
/// <see cref="ControllerBase"/> natives only (<see cref="ControllerBase.Ok(object)"/>/
/// <see cref="ControllerBase.NotFound()"/>/<c>ControllerBase.Problem</c>) — never a Midgard
/// reference. A failure result explicitly sets its own <see cref="ObjectResult.ContentTypes"/> to the
/// RFC 9457 media types (<c>application/problem+json</c>/<c>application/problem+xml</c>) — otherwise
/// the class-level <see cref="ConsumesAttribute"/>/<see cref="ProducesAttribute"/> pair back-fills the
/// plain ones onto every response that doesn't set its own, including this one. Rendering itself is the
/// host-registered formatters' job (Midgard's <c>ProblemXmlWriter</c>/<c>ProblemXmlOutputFormatter</c>
/// and MVC's built-in <c>application/problem+json</c> support).
///
/// This fold is the text-channel counterpart to Midgard's <c>OutcomeServerInterceptor</c> (the gRPC
/// edge's own <c>Outcome&lt;T&gt;</c> fold, via <c>ProblemExtensions.ToRpcException</c>) and the two are
/// required to agree state-for-state: every <see cref="ErrorCategory"/> below maps to the HTTP status the
/// canonical gRPC-to-HTTP mapping (the well-known table grpc-gateway and Google's own APIs use — 200/400/
/// 401/403/404/409/410/500 etc.) would assign to the gRPC <c>StatusCode</c> the interceptor selects for
/// that same category — including <see cref="ErrorCategory.Erased"/>, which folds to 410 Gone from the
/// gRPC edge's <c>NotFound</c> (its <c>ErrorInfo.Reason</c> carries the authoritative "Erased"
/// discriminator that distinguishes it from a plain <see cref="ErrorCategory.NotFound"/>). Verified
/// category by category against <c>ProblemExtensions.cs</c>, not assumed.
/// </summary>
[ApiController]
[Consumes("application/json", "application/xml")]
[Produces("application/json", "application/xml")]
[RequestSizeLimit(1_048_576)] // spec §8.4 — the 1 MiB body cap is declared at the facade, not host config: a formatter (Task 9) cannot enforce body size on its own.
public abstract class GrpcControllerBase : ControllerBase
{
	/// <summary>
	/// Folds a gRPC-service-shaped <see cref="Outcome{T}"/> operation into an <see cref="ActionResult{TValue}"/>:
	/// success → <see cref="ControllerBase.Ok(object)"/>; <see cref="ErrorCategory.NotFound"/> →
	/// <see cref="ControllerBase.NotFound()"/> (no body); every other failure category → problem details
	/// (spec §11) via <c>ControllerBase.Problem</c>, carrying an <c>errors</c> extension —
	/// <see cref="ProblemErrorEntry"/> entries flattened from <see cref="Problem.Errors"/> — when any are
	/// present, and a <c>correlationId</c> extension when <see cref="Problem.CorrelationId"/> is set
	/// (populated only for <see cref="ErrorCategory.Fault"/>).
	/// </summary>
	protected async Task<ActionResult<TResponse>> FoldAsync<TResponse>(ValueTask<Outcome<TResponse>> operation)
		where TResponse : notnull
	{
		var outcome = await operation.ConfigureAwait(false);
		return outcome.Match<ActionResult<TResponse>>(
			success => Ok(success),
			problem => problem.Category == ErrorCategory.NotFound ? NotFound() : ToProblemResult(problem));
	}

	ObjectResult ToProblemResult(Problem problem)
	{
		var statusCode = problem.Category switch
		{
			ErrorCategory.Validation => StatusCodes.Status400BadRequest,               // gRPC InvalidArgument
			ErrorCategory.Conflict => StatusCodes.Status409Conflict,                   // gRPC AlreadyExists
			ErrorCategory.Unauthorized => StatusCodes.Status401Unauthorized,           // gRPC Unauthenticated
			ErrorCategory.Forbidden or ErrorCategory.LockedOut => StatusCodes.Status403Forbidden, // gRPC PermissionDenied
			ErrorCategory.NotAllowed => StatusCodes.Status400BadRequest,               // gRPC FailedPrecondition
			ErrorCategory.InvalidCredentials => StatusCodes.Status401Unauthorized,     // gRPC Unauthenticated
			ErrorCategory.Fault => StatusCodes.Status500InternalServerError,           // gRPC Internal
			ErrorCategory.MultipleMatches => StatusCodes.Status500InternalServerError, // gRPC Internal
			ErrorCategory.Erased => StatusCodes.Status410Gone,                         // gRPC NotFound — ErrorInfo.Reason carries the authoritative "Erased"
			_ => StatusCodes.Status500InternalServerError                             // gRPC Unknown — the Unspecified sentinel; never a real emitted category.
		};

		Dictionary<string, object?>? extensions = null;
		if (problem.Errors.Count > 0)
		{
			extensions = new Dictionary<string, object?>
			{
				["errors"] = problem.Errors
					.SelectMany(entry => entry.Value.Select(message => new ProblemErrorEntry(entry.Key, message)))
					.ToArray()
			};
		}

		if (problem.CorrelationId is { } correlationId)
		{
			extensions ??= [];
			extensions["correlationId"] = correlationId;
		}

		if (problem.Receipt is { } receipt)
		{
			extensions ??= [];
			extensions["receipt"] = receipt.ReceiptId;
			extensions["severedAt"] = receipt.SeveredAt.ToString("O", CultureInfo.InvariantCulture);
		}

		var result = Problem(statusCode: statusCode, title: problem.Category.ToString(), extensions: extensions);

		// The class-level [Produces("application/json", "application/xml")] back-fills ContentTypes on
		// any ObjectResult that doesn't already set them — a failure result must set its own RFC 9457
		// media types here, before that attribute gets the chance to lock it to the plain ones. Left to
		// the attribute, an XML-negotiated failure would route into XmlContractOutputFormatter, which
		// carries no shape for ProblemDetails and throws.
		result.ContentTypes.Add("application/problem+json");
		result.ContentTypes.Add("application/problem+xml");
		return result;
	}
}
