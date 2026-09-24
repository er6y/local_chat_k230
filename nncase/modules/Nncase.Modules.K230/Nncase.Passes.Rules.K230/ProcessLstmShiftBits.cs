using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessLstmShiftBits : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNELSTM("lstm", "lstmCall", (GNNELSTM _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsWildcard("wXc"), Nncase.PatternMatch.Utility.IsWildcard("actXc"), Nncase.PatternMatch.Utility.IsWildcard("wRc"), Nncase.PatternMatch.Utility.IsWildcard("actRc0"), Nncase.PatternMatch.Utility.IsWildcard("actRc1"), Nncase.PatternMatch.Utility.IsWildcard("initH"), Nncase.PatternMatch.Utility.IsWildcard("initC"), Nncase.PatternMatch.Utility.IsWildcard("segFittingParamFt"), Nncase.PatternMatch.Utility.IsWildcard("segFittingParamGt"), Nncase.PatternMatch.Utility.IsWildcard("wXcQarg"), Nncase.PatternMatch.Utility.IsWildcard("wRcQarg"), Nncase.PatternMatch.Utility.IsWildcard("actBin"), Nncase.PatternMatch.Utility.IsWildcard("actBinQ"), Nncase.PatternMatch.Utility.IsWildcard("ifDeqBias"), Nncase.PatternMatch.Utility.IsWildcard("xcShiftBits"), Nncase.PatternMatch.Utility.IsWildcard("hDeqBias0"), Nncase.PatternMatch.Utility.IsWildcard("hDeqBias1"), Nncase.PatternMatch.Utility.IsWildcard("cShiftBits"), Nncase.PatternMatch.Utility.IsWildcard("rcShiftBits0"), Nncase.PatternMatch.Utility.IsWildcard("rcShiftBits1"), Nncase.PatternMatch.Utility.IsWildcard("outHShiftBits"), Nncase.PatternMatch.Utility.IsWildcard("outCShiftBits"), Nncase.PatternMatch.Utility.IsWildcard("hasStatic"), Nncase.PatternMatch.Utility.IsWildcard("outputSize"));


	private Expr? GetReplace(GNNELSTM lstm, IMatchResult result, RunPassContext options)
	{
		ActParam2 actParam = new ActParam2(lstm.ActivationParamXc);
		int num = actParam.FusedShiftBits();
		Expr expr = GetReplaceHelper.LoadAct0(actParam);
		ActParam2 actParam2 = new ActParam2(lstm.ActivationParamRc0);
		int num2 = actParam2.FusedShiftBits();
		Expr expr2 = GetReplaceHelper.LoadAct0(actParam2);
		ActParam2 actParam3 = new ActParam2(lstm.ActivationParamRc1);
		int num3 = actParam3.FusedShiftBits();
		Expr expr3 = GetReplaceHelper.LoadAct0(actParam3);
		Call expr4 = new Call(lstm, (Expr)result["input"], (Expr)result["wXc"], expr, (Expr)result["wRc"], expr2, expr3, (Expr)result["initH"], (Expr)result["initC"], (Expr)result["segFittingParamFt"], (Expr)result["segFittingParamGt"], (Expr)result["wXcQarg"], (Expr)result["wRcQarg"], (Expr)result["actBin"], (Expr)result["actBinQ"], (Expr)result["ifDeqBias"], num, (Expr)result["hDeqBias0"], (Expr)result["hDeqBias1"], (Expr)result["cShiftBits"], num2, num3, (Expr)result["outHShiftBits"], (Expr)result["outCShiftBits"], (Expr)result["hasStatic"], (Expr)result["outputSize"]);
		return GetReplaceHelper.SuppressPattern(options, expr4, Pattern);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNELSTM lstm = (GNNELSTM)__result["lstm"];
		return GetReplace(lstm, __result, __context);
	}
}
