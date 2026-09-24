using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessAct1ShiftBits : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEActivation("gnneAct", "actCall", (GNNEActivation _) => true, Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsWildcard("inputB"), null, Nncase.PatternMatch.Utility.IsWildcard("inAShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("inBShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("outShiftbits"), Nncase.PatternMatch.Utility.IsTensorConst("deqAParams"), Nncase.PatternMatch.Utility.IsTensorConst("deqBParams"), Nncase.PatternMatch.Utility.IsWildcard("outChannels"), Nncase.PatternMatch.Utility.IsWildcard("is16Segment"));


	private Expr? GetReplace(GNNEActivation gnneAct, IMatchResult result, RunPassContext options)
	{
		ActParamBase obj = ((gnneAct.ActParam is ActParam2 other) ? ((ActParamBase)new ActParam2(other)) : ((ActParamBase)new ActParam16((ActParam16)gnneAct.ActParam)));
		sbyte act1ShiftBits = ShiftBitsHelper.GetAct1ShiftBits(obj);
		obj.FusedShiftBits(act1ShiftBits);
		Expr expr = GetReplaceHelper.LoadAct1(obj);
		Call expr2 = new Call(gnneAct, (Expr)result["inputA"], (Expr)result["inputB"], expr, (Expr)result["inAShiftbits"], (Expr)result["inBShiftbits"], (int)act1ShiftBits, (Expr)result["deqAParams"], (Expr)result["deqBParams"], (Expr)result["outChannels"], (Expr)result["is16Segment"]);
		return GetReplaceHelper.SuppressPattern(options, expr2, Pattern);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEActivation gnneAct = (GNNEActivation)__result["gnneAct"];
		return GetReplace(gnneAct, __result, __context);
	}
}
