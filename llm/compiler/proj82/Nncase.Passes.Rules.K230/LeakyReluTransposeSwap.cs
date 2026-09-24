using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Math;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class LeakyReluTransposeSwap : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("leakyMarker", Nncase.PatternMatch.F.NN.IsLeakyRelu("leaky", "leakyCall", (LeakyRelu _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("transMarker", Nncase.PatternMatch.F.Tensors.IsTranspose(Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("perm", TypePatternUtility.IsIntegral())), Nncase.PatternMatch.Utility.IsTensorConst("transRange")), Nncase.PatternMatch.Utility.IsTensorConst("alpha")), Nncase.PatternMatch.Utility.IsTensorConst("leakyRange"));


	public Expr? GetReplace(LeakyRelu leaky, Call leakyCall, TensorConst alpha, Expr input, Expr leakyMarker, Tensor<float> leakyRange, Expr transMarker, Tensor<float> transRange, Expr perm)
	{
		if (((Marker)input).Target is Call call && call.Target is Binary)
		{
			return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Transpose(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.NN.LeakyRelu(input, alpha), leakyRange).With(null, null, null, adaQuantInfo: ((Marker)leakyMarker).AdaQuantInfo, mixQuantInfo: ((Marker)leakyMarker).MixQuantInfo), perm), transRange).With(null, null, null, adaQuantInfo: ((Marker)transMarker).AdaQuantInfo, mixQuantInfo: ((Marker)transMarker).MixQuantInfo);
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		LeakyRelu leaky = (LeakyRelu)__result["leaky"];
		Call leakyCall = (Call)__result["leakyCall"];
		TensorConst alpha = (TensorConst)__result["alpha"];
		Expr input = (Expr)__result["input"];
		Expr leakyMarker = (Expr)__result["leakyMarker"];
		Tensor<float> leakyRange = ((TensorConst)__result["leakyRange"]).Value.Cast<float>();
		Expr transMarker = (Expr)__result["transMarker"];
		Tensor<float> transRange = ((TensorConst)__result["transRange"]).Value.Cast<float>();
		Expr perm = (Expr)__result["perm"];
		return GetReplace(leaky, leakyCall, alpha, input, leakyMarker, leakyRange, transMarker, transRange, perm);
	}
}
