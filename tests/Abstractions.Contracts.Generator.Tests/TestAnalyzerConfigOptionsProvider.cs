using Microsoft.CodeAnalysis.Diagnostics;

namespace Norse.Abstractions.Gateway.Generator.Tests;

sealed class TestAnalyzerConfigOptionsProvider(string emissionMode) : AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(emissionMode);
	public override AnalyzerConfigOptions GetOptions(Microsoft.CodeAnalysis.SyntaxTree tree) => GlobalOptions;
	public override AnalyzerConfigOptions GetOptions(Microsoft.CodeAnalysis.AdditionalText textFile) => GlobalOptions;

	sealed class TestOptions(string emissionMode) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			if (key == "build_property.NorseGatewayEmissionMode")
			{
				value = emissionMode;
				return true;
			}
			value = "";
			return false;
		}
	}
}
