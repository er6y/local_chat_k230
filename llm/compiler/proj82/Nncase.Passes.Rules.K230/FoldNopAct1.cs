using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldNopAct1 : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEStore("st", "stCall", (GNNEStore _) => true, Nncase.PatternMatch.F.K230.IsGNNEActivation("gnneAct", "actCall", (GNNEActivation _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad((GNNELoad _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")), Nncase.PatternMatch.Utility.IsNone(), null, Nncase.PatternMatch.Utility.IsWildcard("inAShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("inBShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("outShiftbits"), Nncase.PatternMatch.Utility.IsTensorConst("deqAParams"), Nncase.PatternMatch.Utility.IsTensorConst("deqBParams"), Nncase.PatternMatch.Utility.IsWildcard("outChannels"), Nncase.PatternMatch.Utility.IsWildcard("is16Segment")));


	private Expr? GetReplace(Call actCall, GNNEActivation gnneAct, IMatchResult result, RunPassContext options, Expr input)
	{
		float[] pattern = new float[7] { 1f, 1f, 0f, 0f, 0f, 255f, 0f };
		float[] paramAct0 = gnneAct.ActParam.GetAct0Data;
		if (paramAct0.Length % pattern.Length == 0 && input.CheckedDataType == DataTypes.UInt8 && actCall.CheckedDataType == DataTypes.UInt8 && Enumerable.Range(0, paramAct0.Length / pattern.Length).All((int i) => pattern.SequenceEqual(paramAct0.Skip(i * pattern.Length).Take(pattern.Length))))
		{
			return input;
		}
		return null;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call actCall = (Call)__result["actCall"];
		GNNEActivation gnneAct = (GNNEActivation)__result["gnneAct"];
		Expr input = (Expr)__result["input"];
		return GetReplace(actCall, gnneAct, __result, __context, input);
	}
}
