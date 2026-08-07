using ProtoBuf.Meta;

namespace Norse.Abstractions.Contracts.Tests;

public sealed class BoolResponseTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	void Round_trips_through_protobuf_net_the_platforms_actual_wire_serializer(bool value)
	{
		// BoolResponse crosses the wire for real — IAuthenticationService.EmailExists's
		// Outcome<BoolResponse> response (Heimdall's AuthN.Services) — so this proves the
		// [DataContract]/[DataMember(Order = 1)] shape protobuf-net needs to serialize the type at
		// all is actually present, not merely asserted in a doc comment. A fresh RuntimeTypeModel
		// (never RuntimeTypeModel.Default, which is process-wide static state other tests could
		// pollute or be polluted by) is enough here: BoolResponse carries no Guid/identifier
		// members, so it needs none of Midgard's IdentifierSerializers/CompatibilityLevel-300 sweep
		// to round-trip correctly — that sweep only affects Guid-shaped members and the model's
		// compatibility level, neither of which this contract has.
		var model = RuntimeTypeModel.Create();
		var original = new BoolResponse { Value = value };

		using MemoryStream stream = new();
		model.Serialize(stream, original);
		var payload = stream.ToArray();

		var roundTripped = (BoolResponse)model.Deserialize(new MemoryStream(payload), null, typeof(BoolResponse))!;

		roundTripped.ShouldBe(original);
	}
}
