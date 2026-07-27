using System.Reflection;
using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests;

public sealed class BehaviorAttributeTests
{
	[Fact]
	void BehaviorAttribute_TargetsClassAndMethod_NotInterface()
	{
		var usage = typeof(BehaviorAttribute).GetCustomAttribute<AttributeUsageAttribute>();
		usage.ShouldNotBeNull();
		usage.ValidOn.HasFlag(AttributeTargets.Class).ShouldBeTrue();
		usage.ValidOn.HasFlag(AttributeTargets.Method).ShouldBeTrue();
		usage.ValidOn.HasFlag(AttributeTargets.Interface).ShouldBeFalse();
	}

	[Fact]
	void BehaviorAttribute_StoresBehaviorTypeAndAfter()
	{
		BehaviorAttribute attribute = new(typeof(string), after: typeof(int));
		attribute.BehaviorType.ShouldBe(typeof(string));
		attribute.After.ShouldBe(typeof(int));
	}
}
