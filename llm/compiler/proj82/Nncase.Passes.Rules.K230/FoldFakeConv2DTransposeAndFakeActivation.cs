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
public sealed class FoldFakeConv2DTransposeAndFakeActivation : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeActivation("fakeAct", "call2", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("range", Nncase.PatternMatch.F.K230.IsFakeConv2DTranspose("fakeConvTranspose", "call1", (FakeConv2DTranspose _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act1"), Nncase.PatternMatch.Utility.IsTensorConst("outputShape"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("outputPadding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue")), Nncase.PatternMatch.Utility.IsTensorConst("fakeActInputRange")), Nncase.PatternMatch.Utility.IsNone(), Nncase.PatternMatch.Utility.IsTensorConst("act2"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsTensorConst("inAShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("inBShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("outShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("is16Segments"));


	private Expr? GetReplace(Expr fakeConvTranspose, Expr input, Expr inputMarker, Expr weights, Expr weightsMarker, TensorConst act1, TensorConst outputShape, TensorConst padding, TensorConst outputPadding, TensorConst stride, TensorConst dilation, TensorConst groups, TensorConst padValue, FakeActivation fakeAct, TensorConst act2, int outChannels, TensorConst is16Segments, Expr range, Call call1, Call call2, RunPassContext context)
	{
		if (context.Driver is DataflowPass && context.GetAnalysis<IExprUserAnalysisResult>()[range].Count() > 1)
		{
			return null;
		}
		if (call1.CheckedShape != call2.CheckedShape)
		{
			return null;
		}
		if (fakeAct.ActParam is ActParam16)
		{
			return null;
		}
		ActParam2 actParam = new ActParam2(outChannels);
		for (int i = 0; i < outChannels; i++)
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
		Tensor<float> tensor = new Tensor<float>(actParam2.GetAct0Data, new int[2] { outChannels, 7 });
		return Nncase.IR.K230.F.Tensors.FakeConv2DTranspose(inputMarker, weightsMarker, tensor, outputShape, padding, outputPadding, stride, dilation, groups, padValue, actParam2).InheritMetaData(call2);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr fakeConvTranspose = (Expr)__result["fakeConvTranspose"];
		Expr input = (Expr)__result["input"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr weights = (Expr)__result["weights"];
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		TensorConst act = (TensorConst)__result["act1"];
		TensorConst outputShape = (TensorConst)__result["outputShape"];
		TensorConst padding = (TensorConst)__result["padding"];
		TensorConst outputPadding = (TensorConst)__result["outputPadding"];
		TensorConst stride = (TensorConst)__result["stride"];
		TensorConst dilation = (TensorConst)__result["dilation"];
		TensorConst groups = (TensorConst)__result["groups"];
		TensorConst padValue = (TensorConst)__result["padValue"];
		FakeActivation fakeAct = (FakeActivation)__result["fakeAct"];
		TensorConst act2 = (TensorConst)__result["act2"];
		int outChannels = ((TensorConst)__result["outChannels"]).Value.ToScalar<int>();
		TensorConst is16Segments = (TensorConst)__result["is16Segments"];
		Expr range = (Expr)__result["range"];
		Call call = (Call)__result["call1"];
		Call call2 = (Call)__result["call2"];
		return GetReplace(fakeConvTranspose, input, inputMarker, weights, weightsMarker, act, outputShape, padding, outputPadding, stride, dilation, groups, padValue, fakeAct, act2, outChannels, is16Segments, range, call, call2, __context);
	}
}
