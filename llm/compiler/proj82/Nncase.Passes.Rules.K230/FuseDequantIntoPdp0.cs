using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FuseDequantIntoPdp0 : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEPdp0Reduce("pdp0_reduce", "ldCall", (GNNEPdp0Reduce _) => true, Math.IsDequantize("deq", "deqCall", (Dequantize _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst()), Nncase.PatternMatch.Utility.IsTensorConst("filter"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("depuantparams"), Nncase.PatternMatch.Utility.IsTensorConst("value"), Nncase.PatternMatch.Utility.IsTensorConst("shiftbits"), Nncase.PatternMatch.Utility.IsTensorConst("count_include_pad"), Nncase.PatternMatch.Utility.IsTensorConst("act"));


	private Expr? GetReplace(GNNEPdp0Reduce pdp0_reduce, Call ldCall, Dequantize deq, Call deqCall, Expr input, Expr filter, Expr stride, Expr padding, Expr depuantparams, Expr value, Expr shiftbits, Expr count_include_pad, Expr act)
	{
		Call input2 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)deq.TargetType, input);
		return Nncase.IR.K230.F.Tensors.GNNEPdp0Reduce(pdp0_reduce.ReduceOp, pdp0_reduce.DestType, pdp0_reduce.ActParam, input2, filter, stride, padding, depuantparams, value, shiftbits, count_include_pad, act);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEPdp0Reduce pdp0_reduce = (GNNEPdp0Reduce)__result["pdp0_reduce"];
		Call ldCall = (Call)__result["ldCall"];
		Dequantize deq = (Dequantize)__result["deq"];
		Call deqCall = (Call)__result["deqCall"];
		Expr input = (Expr)__result["input"];
		Expr filter = (Expr)__result["filter"];
		Expr stride = (Expr)__result["stride"];
		Expr padding = (Expr)__result["padding"];
		Expr depuantparams = (Expr)__result["depuantparams"];
		Expr value = (Expr)__result["value"];
		Expr shiftbits = (Expr)__result["shiftbits"];
		Expr count_include_pad = (Expr)__result["count_include_pad"];
		Expr act = (Expr)__result["act"];
		return GetReplace(pdp0_reduce, ldCall, deq, deqCall, input, filter, stride, padding, depuantparams, value, shiftbits, count_include_pad, act);
	}
}
