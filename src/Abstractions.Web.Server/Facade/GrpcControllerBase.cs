using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Norse.Abstractions.Components.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Facade;

/// <summary>
///     The shared base for every hand-authored REST facade controller (Futhark spec §4) — each injects the
///     gRPC service <em>interface</em> directly and runs it in-process, no protobuf on the path, the same
///     mediator pipeline underneath as the gRPC transport. <see cref="FoldAsync{TResponse}" /> is the single
///     landing site for the <c>Outcome&lt;T&gt;</c> → <see cref="ActionResult{TValue}" /> fold, built from
///     <see cref="ControllerBase" /> natives only (<see cref="ControllerBase.Ok(object)" />/
///     <see cref="ControllerBase.NotFound()" />/<c>ControllerBase.Problem</c>) — never a Midgard
///     reference. A failure result explicitly sets its own <see cref="ObjectResult.ContentTypes" /> to the
///     RFC 9457 media types (<c>application/problem+json</c>/<c>application/problem+xml</c>) — otherwise
///     content negotiation would consider the plain media types for a failure body too, and an
///     XML-negotiated failure would route into a contract formatter with no shape for it. Rendering itself is the
///     host-registered formatters' job (Midgard's <c>ProblemXmlWriter</c>/<c>ProblemXmlOutputFormatter</c>
///     and MVC's built-in <c>application/problem+json</c> support).
///     This fold is the text-channel counterpart to Midgard's <c>OutcomeServerInterceptor</c> (the gRPC
///     edge's own <c>Outcome&lt;T&gt;</c> fold, via <c>ProblemExtensions.ToRpcException</c>) and the two are
///     required to agree state-for-state: every <see cref="ErrorCategory" /> below maps to the HTTP status the
///     canonical gRPC-to-HTTP mapping (the well-known table grpc-gateway and Google's own APIs use — 200/400/
///     401/403/404/409/410/500 etc.) would assign to the gRPC <c>StatusCode</c> the interceptor selects for
///     that same category — including <see cref="ErrorCategory.Erased" />, which folds to 410 Gone from the
///     gRPC edge's <c>NotFound</c> (its <c>ErrorInfo.Reason</c> carries the authoritative "Erased"
///     discriminator that distinguishes it from a plain <see cref="ErrorCategory.NotFound" />). Both edges
///     project from the same declared source, <see cref="TransportDispositions" />, so the two can no
///     longer disagree by construction.
/// </summary>
[ApiController]
[Authorize(Policy = NorsePolicies.Machine)]
// Deliberately no class-level [Consumes] or [Produces] -- both are Swashbuckle-era prior art with no
// job left here, and [Consumes] is actively harmful: it doubles as IAcceptsMetadata, and endpoint
// routing's AcceptsMatcherPolicy partitions the match DFA on request Content-Type, making every
// bodyless GET facade action unroutable (404) for requests without a Content-Type header. Media types
// are the host's formatters' jurisdiction on both directions: input policing is 415 via
// UnsupportedContentTypeFilter, output negotiation (including the JSON-by-default order and the honest
// 406) is formatter registration order plus ReturnHttpNotAcceptable, and failure results set their own
// RFC 9457 content types in ToProblemResult. The OpenAPI document derives its media types from those
// same formatters, so it stays honest without attribute duplication.
[RequestSizeLimit(1_048_576)] // spec §8.4 — the 1 MiB body cap is declared at the facade, not host config: a formatter (Task 9) cannot enforce body size on its own.
public abstract class GrpcControllerBase : ControllerBase
{
	/// <summary>
	///     Folds a gRPC-service-shaped <see cref="Outcome{T}" /> operation into an <see cref="ActionResult{TValue}" />:
	///     success → <see cref="ControllerBase.Ok(object)" />; every failure category folds via
	///     <see cref="TransportDispositions.For(ErrorCategory)" /> — a category whose disposition does not
	///     permit a body (<see cref="ErrorCategory.NotFound" />, <see cref="ErrorCategory.Unauthorized" />,
	///     <see cref="ErrorCategory.InvalidCredentials" />) folds to a bare <see cref="StatusCodeResult" />,
	///     no body; every other category folds to problem details (spec §11) via <c>ControllerBase.Problem</c>,
	///     carrying an <c>errors</c> extension — <see cref="ProblemErrorEntry" /> entries flattened from
	///     <see cref="Problem.Errors" /> — when any are present, and a <c>correlationId</c> extension when
	///     <see cref="Problem.CorrelationId" /> is set (populated only for <see cref="ErrorCategory.Fault" />),
	///     with RFC 9457 content negotiation (<c>application/problem+json</c>/<c>application/problem+xml</c>).
	/// </summary>
	protected async Task<ActionResult<TResponse>> FoldAsync<TResponse>(ValueTask<Outcome<TResponse>> operation)
		where TResponse : notnull
	{
		var outcome = await operation.ConfigureAwait(false);
		return outcome.Match<ActionResult<TResponse>>(
			success => Ok(success),
			problem => ToResult(problem));
	}

	ActionResult ToResult(Problem problem)
	{
		var disposition = TransportDispositions.For(problem.Category);

		// The silent categories and the bodyless 404 share one exit, and it is the only exit that can
		// produce them: there is no branch below that could attach a body to a disposition which does not
		// permit one. That is the structural half of the "401 explains nothing" ruling -- not a
		// convention a future edit could forget.
		if (!disposition.BodyPermitted)
			return new StatusCodeResult(disposition.HttpStatus);

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

		var result = Problem(statusCode: disposition.HttpStatus, title: problem.Category.ToString(),
			extensions: extensions);
		result.ContentTypes.Add("application/problem+json");
		result.ContentTypes.Add("application/problem+xml");
		return result;
	}
}
