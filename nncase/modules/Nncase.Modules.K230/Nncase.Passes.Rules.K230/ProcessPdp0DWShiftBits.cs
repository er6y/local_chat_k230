using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessPdp0DWShiftBits : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEPdp0DW("dw", "dwCall", (GNNEPdp0DW _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsWildcard("weights"), Nncase.PatternMatch.Utility.IsWildcard("weightsBias"), Nncase.PatternMatch.Utility.IsWildcard("weightsBiasQint8"), null, Nncase.PatternMatch.Utility.IsWildcard("actQint8"), Nncase.PatternMatch.Utility.IsWildcard("deqBias"), Nncase.PatternMatch.Utility.IsTensorConst("shiftBits"), Nncase.PatternMatch.Utility.IsWildcard("shiftBitsQint8"), Nncase.PatternMatch.Utility.IsWildcard("qint8Qp"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("stride"), Nncase.PatternMatch.Utility.IsWildcard("dilation"), Nncase.PatternMatch.Utility.IsWildcard("groups"), Nncase.PatternMatch.Utility.IsWildcard("is16Quant"), Nncase.PatternMatch.Utility.IsWildcard("padValue"), Nncase.PatternMatch.Utility.IsWildcard("weightsQint8"));


	private Expr? GetReplace(GNNEPdp0DW dw, IMatchResult result, RunPassContext options)
	{
		ActParam2 actParam = new ActParam2(dw.ActParam);
		int num = actParam.FusedShiftBits();
		Expr expr = GetReplaceHelper.LoadAct0(actParam);
		Call expr2 = new Call(dw, (Expr)result["input"], (Expr)result["weights"], (Expr)result["weightsBias"], (Expr)result["weightsBiasQint8"], expr, (Expr)result["actQint8"], (Expr)result["deqBias"], num, (Expr)result["shiftBitsQint8"], (Expr)result["qint8Qp"], (Expr)result["padding"], (Expr)result["stride"], (Expr)result["dilation"], (Expr)result["groups"], (Expr)result["is16Quant"], (Expr)result["padValue"], (Expr)result["weightsQint8"]);
		return GetReplaceHelper.SuppressPattern(options, expr2, Pattern);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEPdp0DW dw = (GNNEPdp0DW)__result["dw"];
		return GetReplace(dw, __result, __context);
	}
}
