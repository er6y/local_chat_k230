using Nncase.IR;
using Nncase.IR.F;
using Nncase.PatternMatch;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReplaceMarker : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("inputRange"));


	public Expr? GetReplace(Marker inputMarker, Expr input, Tensor<float> inputRange, RunPassContext options)
	{
		Marker marker = Math.RangeOfMarker(input, inputRange);
		marker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		if (inputMarker.MixQuantInfo != null)
		{
			marker.MixQuantInfo.DoSquant = inputMarker.MixQuantInfo.DoSquant;
			marker.MixQuantInfo.HasBindedMixQuantInfo = inputMarker.MixQuantInfo.HasBindedMixQuantInfo;
			marker.MixQuantInfo.MarkerQuantType = inputMarker.MixQuantInfo.MarkerQuantType;
			marker.MixQuantInfo.QuantParameter = inputMarker.MixQuantInfo.QuantParameter;
			marker.MixQuantInfo.U8FineTunedWeights = inputMarker.MixQuantInfo.U8FineTunedWeights;
			marker.MixQuantInfo.U8FineTunedWeightsRangesByChannel = inputMarker.MixQuantInfo.U8FineTunedWeightsRangesByChannel;
			marker.MixQuantInfo.I8FineTunedWeights = inputMarker.MixQuantInfo.I8FineTunedWeights;
			marker.MixQuantInfo.I8FineTunedWeightsRangesByChannel = inputMarker.MixQuantInfo.I8FineTunedWeightsRangesByChannel;
		}
		options.MatchOptions.SuppressPattern(marker, Pattern);
		return marker;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Marker inputMarker = (Marker)__result["inputMarker"];
		Expr input = (Expr)__result["input"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		return GetReplace(inputMarker, input, inputRange, __context);
	}
}
