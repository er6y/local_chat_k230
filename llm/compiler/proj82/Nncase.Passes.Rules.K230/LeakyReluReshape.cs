using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class LeakyReluReshape : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.NN.IsLeakyRelu("leaky", "leakyCall", (LeakyRelu _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("alpha")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	public Expr? GetReplace(LeakyRelu leaky, Call leakyCall, TensorConst alpha, Expr input, Expr outputMarker, Tensor<float> outputRange)
	{
		Call leakyCall2 = leakyCall;
		if (leakyCall2.CheckedShape.Count < 2 || leakyCall2.CheckedShape.ToValueArray()[^1] != 1 || leakyCall2.CheckedShape.ToValueArray()[^2] == 1)
		{
			return null;
		}
		int[] array = leakyCall2.CheckedShape.ToValueArray();
		array[^1] = leakyCall2.CheckedShape.ToValueArray()[^2];
		array[^2] = leakyCall2.CheckedShape.ToValueArray()[^1];
		if (leakyCall2.CheckedShape.Size < 65536)
		{
			array = leakyCall2.CheckedShape.ToValueArray().Select((int dim, int idx) => (idx < leakyCall2.CheckedShape.Count - 1) ? 1 : leakyCall2.CheckedShape.Size).ToArray();
		}
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.NN.LeakyRelu(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(input, array), ((Marker)input).Attribute).With(null, null, null, adaQuantInfo: ((Marker)input).AdaQuantInfo, mixQuantInfo: ((Marker)input).MixQuantInfo), alpha), outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo), leakyCall2.CheckedShape), outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		LeakyRelu leaky = (LeakyRelu)__result["leaky"];
		Call leakyCall = (Call)__result["leakyCall"];
		TensorConst alpha = (TensorConst)__result["alpha"];
		Expr input = (Expr)__result["input"];
		Expr outputMarker = (Expr)__result["outputMarker"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		return GetReplace(leaky, leakyCall, alpha, input, outputMarker, outputRange);
	}
}
