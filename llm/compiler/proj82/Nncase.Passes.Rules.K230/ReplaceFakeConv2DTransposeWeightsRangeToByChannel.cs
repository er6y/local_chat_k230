using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Targets;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReplaceFakeConv2DTransposeWeightsRangeToByChannel : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeConv2DTranspose("fakeConvTranspose", "call", (FakeConv2DTranspose _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("outputShape"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("outputPadding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue"));


	private Expr? GetReplace(FakeConv2DTranspose fakeConvTranspose, Marker inputMarker, Marker weightsMarker, Expr input, Expr inputRange, Expr weights, Tensor<float> act, Expr outputShape, Expr padding, Expr outputPadding, Expr stride, Expr dilation, int groups, Expr padValue, RunPassContext options)
	{
		if (weightsMarker.MixQuantInfo == null)
		{
			weightsMarker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		}
		float[] array = ((TensorConst)weights).Value.ToArray<float>();
		int num = weights.CheckedShape[0].FixedValue * groups;
		TensorConst range = new TensorConst(Tensor.From(QuantUtility.GetWeightsRangesByChannel(array, num).ToArray(), new int[2] { num, 2 }));
		Marker marker = Nncase.IR.F.Math.RangeOfMarker(weights, range);
		if (marker.MixQuantInfo == null)
		{
			marker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		}
		marker.MixQuantInfo = weightsMarker.MixQuantInfo;
		Call call = Nncase.IR.K230.F.Tensors.FakeConv2DTranspose(inputMarker, marker, act, outputShape, padding, outputPadding, stride, dilation, groups, padValue, fakeConvTranspose.ActParam);
		options.MatchOptions.SuppressPattern(call, Pattern);
		return call;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeConv2DTranspose fakeConvTranspose = (FakeConv2DTranspose)__result["fakeConvTranspose"];
		Marker inputMarker = (Marker)__result["inputMarker"];
		Marker weightsMarker = (Marker)__result["weightsMarker"];
		Expr input = (Expr)__result["input"];
		Expr inputRange = (Expr)__result["inputRange"];
		Expr weights = (Expr)__result["weights"];
		Tensor<float> act = ((TensorConst)__result["act"]).Value.Cast<float>();
		Expr outputShape = (Expr)__result["outputShape"];
		Expr padding = (Expr)__result["padding"];
		Expr outputPadding = (Expr)__result["outputPadding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Expr padValue = (Expr)__result["padValue"];
		return GetReplace(fakeConvTranspose, inputMarker, weightsMarker, input, inputRange, weights, act, outputShape, padding, outputPadding, stride, dilation, groups, padValue, __context);
	}
}
