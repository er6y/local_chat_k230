using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldFakeMatMulAndFakeActivation : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeActivation("fakeAct", "call2", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("range", Nncase.PatternMatch.F.K230.IsFakeMatMul("fakeMatMul", "call1", (FakeMatMul _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act1")), Nncase.PatternMatch.Utility.IsTensorConst("fakeActInputRange")), Nncase.PatternMatch.Utility.IsNone(), Nncase.PatternMatch.Utility.IsTensorConst("act2"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsTensorConst("inAShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("inBShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("outShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("is16Segments"));


	private Expr? GetReplace(Expr fakeMatMul, Expr input, Expr inputMarker, Expr weights, Expr weightsMarker, TensorConst act1, FakeActivation fakeAct, TensorConst act2, TensorConst outChannels, TensorConst is16Segments, Expr range, Call call1, Call call2, RunPassContext context)
	{
		if (context.Driver is DataflowPass && context.GetAnalysis<IExprUserAnalysisResult>()[range].Count() > 1)
		{
			return null;
		}
		if (call1.CheckedShape != call2.CheckedShape)
		{
			return null;
		}
		ActParam2 actParam = new ActParam2(act1.CheckedShape[0].FixedValue);
		for (int i = 0; i < outChannels.Value.ToScalar<int>(); i++)
		{
			actParam.Ks[0, i] = act1.Value.ToArray<float>()[i * 7];
			actParam.Ks[1, i] = act1.Value.ToArray<float>()[i * 7 + 1];
			actParam.Bs[0, i] = act1.Value.ToArray<float>()[i * 7 + 2];
			actParam.Bs[1, i] = act1.Value.ToArray<float>()[i * 7 + 3];
			actParam.FusedClamp[i].Min = act1.Value.ToArray<float>()[i * 7 + 4];
			actParam.FusedClamp[i].Max = act1.Value.ToArray<float>()[i * 7 + 5];
			actParam.Xs[0, i] = act1.Value.ToArray<float>()[i * 7 + 6];
		}
		ActParam2 actParam2 = GetReplaceHelper.FoldAct0WithAct1(actParam, is16Segments, outChannels, (ActParam2)fakeAct.ActParam);
		if (actParam2 == null)
		{
			return null;
		}
		Tensor<float> tensor = new Tensor<float>(actParam2.GetAct0Data, new int[2]
		{
			act1.CheckedShape[0].FixedValue,
			7
		});
		return Nncase.IR.K230.F.Tensors.FakeMatMul(inputMarker, weightsMarker, tensor, actParam2).InheritMetaData(call2);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr fakeMatMul = (Expr)__result["fakeMatMul"];
		Expr input = (Expr)__result["input"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr weights = (Expr)__result["weights"];
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		TensorConst act = (TensorConst)__result["act1"];
		FakeActivation fakeAct = (FakeActivation)__result["fakeAct"];
		TensorConst act2 = (TensorConst)__result["act2"];
		TensorConst outChannels = (TensorConst)__result["outChannels"];
		TensorConst is16Segments = (TensorConst)__result["is16Segments"];
		Expr range = (Expr)__result["range"];
		Call call = (Call)__result["call1"];
		Call call2 = (Call)__result["call2"];
		return GetReplace(fakeMatMul, input, inputMarker, weights, weightsMarker, act, fakeAct, act2, outChannels, is16Segments, range, call, call2, __context);
	}
}
