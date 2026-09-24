using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Quantization;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class SquantFineTuneFakeConv2DWeights : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv", "call", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue"));


	private Expr? GetReplace(FakeConv2D fakeConv, Marker inputMarker, Marker weightsMarker, TensorConst weights, TensorConst weightsRange, Tensor<float> act, Expr padding, Expr stride, Expr dilation, int groups, Expr padValue, RunPassContext options)
	{
		if (!base.CompileSession.CompileOptions.QuantizeOptions.UseSquant)
		{
			return null;
		}
		if (weightsMarker.MixQuantInfo == null)
		{
			weightsMarker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		}
		Tensor<float> inputWeights = Tensor.From(weights.Value.ToArray<float>(), weights.CheckedShape);
		TensorConst tensorConst = ((weightsMarker.MixQuantInfo.U8FineTunedWeightsRangesByChannel == null) ? weightsRange : weightsMarker.MixQuantInfo.U8FineTunedWeightsRangesByChannel);
		TensorConst tensorConst2 = ((weightsMarker.MixQuantInfo.I8FineTunedWeightsRangesByChannel == null) ? weightsRange : weightsMarker.MixQuantInfo.I8FineTunedWeightsRangesByChannel);
		TensorConst obj = ((((K230Target.K230MixQuantInfo)weightsMarker.MixQuantInfo).I16FineTunedWeightsRangesByChannel == null) ? weightsRange : ((K230Target.K230MixQuantInfo)weightsMarker.MixQuantInfo).I16FineTunedWeightsRangesByChannel);
		Tensor<float> inputWeightsRanges = Tensor.From(tensorConst.Value.ToArray<float>(), weightsRange.CheckedShape);
		Tensor<float> inputWeightsRanges2 = Tensor.From(tensorConst2.Value.ToArray<float>(), weightsRange.CheckedShape);
		Tensor<float> inputWeightsRanges3 = Tensor.From(obj.Value.ToArray<float>(), weightsRange.CheckedShape);
		Tensor<float> tensor = QuantAlgorithmUtility.SquantWeights(inputWeights, inputWeightsRanges, weights.CheckedShape, QuantMode.UnsignedMode, 8, isByChannel: true);
		Tensor<float> tensor2 = QuantAlgorithmUtility.SquantWeights(inputWeights, inputWeightsRanges2, weights.CheckedShape, QuantMode.SignedSymmetricMode, 8, isByChannel: true);
		Tensor<float> tensor3 = QuantAlgorithmUtility.SquantWeights(inputWeights, inputWeightsRanges3, weights.CheckedShape, QuantMode.SignedSymmetricMode, 16, isByChannel: true);
		weightsMarker.MixQuantInfo.U8FineTunedWeights = new TensorConst(tensor);
		weightsMarker.MixQuantInfo.I8FineTunedWeights = new TensorConst(tensor2);
		((K230Target.K230MixQuantInfo)weightsMarker.MixQuantInfo).I16FineTunedWeights = new TensorConst(tensor3);
		weightsMarker.MixQuantInfo.DoSquant = true;
		Call call = Nncase.IR.K230.F.Tensors.FakeConv2D(inputMarker, weightsMarker, act, padding, stride, dilation, groups, padValue, fakeConv.ActParam);
		options.MatchOptions.SuppressPattern(call, Pattern);
		return call;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeConv2D fakeConv = (FakeConv2D)__result["fakeConv"];
		Marker inputMarker = (Marker)__result["inputMarker"];
		Marker weightsMarker = (Marker)__result["weightsMarker"];
		TensorConst weights = (TensorConst)__result["weights"];
		TensorConst weightsRange = (TensorConst)__result["weightsRange"];
		Tensor<float> act = ((TensorConst)__result["act"]).Value.Cast<float>();
		Expr padding = (Expr)__result["padding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Expr padValue = (Expr)__result["padValue"];
		return GetReplace(fakeConv, inputMarker, weightsMarker, weights, weightsRange, act, padding, stride, dilation, groups, padValue, __context);
	}
}
