using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessMatmulShiftBits : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEMatMul("mm", "mmCall", (GNNEMatMul _) => true, Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsWildcard("inputB"), null, Nncase.PatternMatch.Utility.IsWildcard("inputABias"), Nncase.PatternMatch.Utility.IsWildcard("inputAShiftbits"), Nncase.PatternMatch.Utility.IsTensorConst("inputBShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("shiftbits"), Nncase.PatternMatch.Utility.IsWildcard("deqBBias"));


	private Expr? GetReplace(GNNEMatMul mm, IMatchResult result, RunPassContext options)
	{
		ActParam2 actParam = new ActParam2(mm.ActParam);
		int num = actParam.FusedShiftBits();
		Expr expr = GetReplaceHelper.LoadAct0(actParam);
		Call expr2 = new Call(mm, (Expr)result["inputA"], (Expr)result["inputB"], expr, (Expr)result["inputABias"], (Expr)result["inputAShiftbits"], (Expr)result["inputBShiftbits"], num, (Expr)result["deqBBias"]);
		return GetReplaceHelper.SuppressPattern(options, expr2, Pattern);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEMatMul mm = (GNNEMatMul)__result["mm"];
		return GetReplace(mm, __result, __context);
	}
}
