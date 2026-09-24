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
public sealed class FoldFakeConv2DAndFakeActivation : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeActivation("fakeAct", "call2", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("range", Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv", "call1", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act1"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue")), Nncase.PatternMatch.Utility.IsTensorConst("fakeActInputRange")), Nncase.PatternMatch.Utility.IsNone(), Nncase.PatternMatch.Utility.IsTensorConst("act2"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsTensorConst("inAShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("inBShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("outShiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("is16Segments"));


	private Expr? GetReplace(Expr fakeConv, Expr input, Expr inputMarker, Expr weights, Expr weightsMarker, TensorConst act1, TensorConst padding, TensorConst stride, TensorConst dilation, TensorConst groups, TensorConst padValue, FakeActivation fakeAct, TensorConst act2, TensorConst outChannels, TensorConst is16Segments, Expr range, Call call1, Call call2, RunPassContext context)
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
		ActParam2 actParam = GetReplaceHelper.FoldAct0WithAct1(((FakeConv2D)fakeConv).ActParam, is16Segments, outChannels, (ActParam2)fakeAct.ActParam);
		if (actParam == null)
		{
			return null;
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct0Data, new int[2]
		{
			weights.CheckedShape[0].FixedValue,
			7
		});
		return Nncase.IR.K230.F.Tensors.FakeConv2D(inputMarker, weightsMarker, tensor, padding, stride, dilation, groups, padValue, actParam).InheritMetaData(call2);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr fakeConv = (Expr)__result["fakeConv"];
		Expr input = (Expr)__result["input"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr weights = (Expr)__result["weights"];
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		TensorConst act = (TensorConst)__result["act1"];
		TensorConst padding = (TensorConst)__result["padding"];
		TensorConst stride = (TensorConst)__result["stride"];
		TensorConst dilation = (TensorConst)__result["dilation"];
		TensorConst groups = (TensorConst)__result["groups"];
		TensorConst padValue = (TensorConst)__result["padValue"];
		FakeActivation fakeAct = (FakeActivation)__result["fakeAct"];
		TensorConst act2 = (TensorConst)__result["act2"];
		TensorConst outChannels = (TensorConst)__result["outChannels"];
		TensorConst is16Segments = (TensorConst)__result["is16Segments"];
		Expr range = (Expr)__result["range"];
		Call call = (Call)__result["call1"];
		Call call2 = (Call)__result["call2"];
		return GetReplace(fakeConv, input, inputMarker, weights, weightsMarker, act, padding, stride, dilation, groups, padValue, fakeAct, act2, outChannels, is16Segments, range, call, call2, __context);
	}
}
