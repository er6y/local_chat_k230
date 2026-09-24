using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldGNNEPdp0ReduceAndGNNEActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEActivation("act", "call2", (GNNEActivation _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad((GNNELoad _) => true, Nncase.PatternMatch.F.K230.IsGNNEStore("st", "stCall", (GNNEStore _) => true, Nncase.PatternMatch.F.K230.IsGNNEPdp0Reduce("pdp0", "call1", (GNNEPdp0Reduce _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("filter"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("dequantizeParam"), Nncase.PatternMatch.Utility.IsTensorConst("value"), Nncase.PatternMatch.Utility.IsTensorConst("shiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad"), Nncase.PatternMatch.F.K230.IsGNNELoadW((GNNELoadW _) => true, Nncase.PatternMatch.Utility.IsTensorConst("act1"))))), Nncase.PatternMatch.Utility.IsNone(), Nncase.PatternMatch.F.K230.IsGNNELoadW((GNNELoadW _) => true, Nncase.PatternMatch.Utility.IsTensorConst("act2")), Nncase.PatternMatch.Utility.IsTensorConst("inAShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("inBShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("outShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("deqAParams"), Nncase.PatternMatch.Utility.IsTensorConst("deqBParams"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsTensorConst("is16Segments"));


	private Expr? GetReplace(Expr pdp0, Expr input, Expr filter, Call call2, TensorConst stride, TensorConst padding, TensorConst dequantizeParam, TensorConst value, TensorConst shiftBits, TensorConst countIncludePad, GNNEActivation act, TensorConst deqAParams, TensorConst outChannels, TensorConst is16Segments, Expr stCall, RunPassContext context)
	{
		if (context.GetAnalysis<IExprUserAnalysisResult>()[stCall].Count() > 1)
		{
			return null;
		}
		ActParam2 actParam = GetReplaceHelper.FoldAct0WithAct1(((GNNEPdp0Reduce)pdp0).ActParam, is16Segments, outChannels, (ActParam2)act.ActParam, deqAParams);
		if (actParam == null)
		{
			return null;
		}
		Call act2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam.ToAct0Data());
		return Nncase.IR.K230.F.Tensors.GNNEPdp0Reduce(((GNNEPdp0Reduce)pdp0).ReduceOp, (PrimType)call2.CheckedDataType, actParam, input, filter, stride, padding, dequantizeParam, value, shiftBits, countIncludePad, act2);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr pdp = (Expr)__result["pdp0"];
		Expr input = (Expr)__result["input"];
		Expr filter = (Expr)__result["filter"];
		Call call = (Call)__result["call2"];
		TensorConst stride = (TensorConst)__result["stride"];
		TensorConst padding = (TensorConst)__result["padding"];
		TensorConst dequantizeParam = (TensorConst)__result["dequantizeParam"];
		TensorConst value = (TensorConst)__result["value"];
		TensorConst shiftBits = (TensorConst)__result["shiftBits"];
		TensorConst countIncludePad = (TensorConst)__result["countIncludePad"];
		GNNEActivation act = (GNNEActivation)__result["act"];
		TensorConst deqAParams = (TensorConst)__result["deqAParams"];
		TensorConst outChannels = (TensorConst)__result["outChannels"];
		TensorConst is16Segments = (TensorConst)__result["is16Segments"];
		Expr stCall = (Expr)__result["stCall"];
		return GetReplace(pdp, input, filter, call, stride, padding, dequantizeParam, value, shiftBits, countIncludePad, act, deqAParams, outChannels, is16Segments, stCall, __context);
	}
}
