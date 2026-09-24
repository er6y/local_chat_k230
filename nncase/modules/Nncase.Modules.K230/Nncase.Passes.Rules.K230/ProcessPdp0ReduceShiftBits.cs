using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessPdp0ReduceShiftBits : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEPdp0Reduce("pdp", "pdpCall", (GNNEPdp0Reduce _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsWildcard("filter"), Nncase.PatternMatch.Utility.IsWildcard("stride"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("dequantParam"), Nncase.PatternMatch.Utility.IsWildcard("value"), Nncase.PatternMatch.Utility.IsWildcard("shiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad"));


	private Expr? GetReplace(GNNEPdp0Reduce pdp, IMatchResult result, RunPassContext options)
	{
		ActParam2 actParam = new ActParam2(pdp.ActParam);
		int num = actParam.FusedShiftBits();
		Expr expr = GetReplaceHelper.LoadAct0(actParam);
		Call expr2 = new Call(pdp, (Expr)result["input"], (Expr)result["filter"], (Expr)result["stride"], (Expr)result["padding"], (Expr)result["dequantParam"], (Expr)result["value"], num, (Expr)result["countIncludePad"], expr);
		return GetReplaceHelper.SuppressPattern(options, expr2, Pattern);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEPdp0Reduce pdp = (GNNEPdp0Reduce)__result["pdp"];
		return GetReplace(pdp, __result, __context);
	}
}
