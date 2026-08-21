namespace Norse.Abstractions.Contracts.Tests;

public sealed class TransportDispositionsTests
{
	[Theory]
	[InlineData(ErrorCategory.Unauthorized, 401, 16, false)]
	[InlineData(ErrorCategory.InvalidCredentials, 401, 16, false)]
	[InlineData(ErrorCategory.LockedOut, 403, 7, true)]
	[InlineData(ErrorCategory.NotAllowed, 403, 7, true)]
	[InlineData(ErrorCategory.Forbidden, 403, 7, true)]
	[InlineData(ErrorCategory.Validation, 400, 3, true)]
	[InlineData(ErrorCategory.Conflict, 409, 6, true)]
	[InlineData(ErrorCategory.NotFound, 404, 5, false)]
	[InlineData(ErrorCategory.Erased, 410, 5, true)]
	[InlineData(ErrorCategory.Fault, 500, 13, true)]
	[InlineData(ErrorCategory.MultipleMatches, 500, 13, true)]
	[InlineData(ErrorCategory.Unspecified, 500, 2, false)]
	void Declares_the_ruled_disposition_for(ErrorCategory category, int http, int grpc, bool bodyPermitted)
	{
		var disposition = TransportDispositions.For(category);

		disposition.HttpStatus.ShouldBe(http);
		disposition.GrpcStatus.ShouldBe(grpc);
		disposition.BodyPermitted.ShouldBe(bodyPermitted);
	}

	[Fact]
	void No_member_escapes_the_table()
	{
		foreach (var category in Enum.GetValues<ErrorCategory>())
			Should.NotThrow(() => TransportDispositions.For(category));
	}

	[Fact]
	void Silent_categories_never_permit_a_body()
	{
		TransportDispositions.For(ErrorCategory.Unauthorized).BodyPermitted.ShouldBeFalse();
		TransportDispositions.For(ErrorCategory.InvalidCredentials).BodyPermitted.ShouldBeFalse();
	}
}
