using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToFakeConv2D : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.NN.IsConv2D("conv", "call", (Conv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("bias"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("fusedClamp")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(Call call, Conv2D conv, Expr input, TensorConst weights, TensorConst bias, Expr stride, Expr padding, Expr dilation, int groups, Tensor<float> fusedClamp, Tensor<float> weightsRange, Tensor<float> inputRange, Tensor<float> outputRange, Marker inputMarker, Marker outputMarker, Marker weightsMarker)
	{
		if (System.Math.Abs(inputRange[new int[1]]) >= 65504f || System.Math.Abs(inputRange[new int[1] { 1 }]) >= 65504f || System.Math.Abs(outputRange[new int[1]]) >= 65504f || System.Math.Abs(outputRange[new int[1] { 1 }]) >= 65504f)
		{
			return null;
		}
		if (weightsRange.ToArray().Max() - weightsRange.ToArray().Min() > 255f)
		{
			return null;
		}
		CompileOptions compileOptions = base.CompileSession.CompileOptions;
		if (compileOptions.QuantizeOptions.QuantScheme != string.Empty && weightsMarker?.MixQuantInfo?.MarkerQuantType != DataTypes.Int8 && weightsMarker?.MixQuantInfo?.MarkerQuantType != DataTypes.UInt8 && weightsMarker?.MixQuantInfo?.MarkerQuantType != DataTypes.Int16)
		{
			return null;
		}
		bool splitWeightsToAct = GNNETypePatternUtility.IsDepthWise(input, weights, groups) && (compileOptions.QuantizeOptions.QuantScheme == string.Empty || !compileOptions.QuantizeOptions.QuantSchemeStrictMode);
		float[] array = weights.Value.ToArray<float>();
		ActParam2 fakeConvActParam = ActParam2.GetFakeConvActParam(weights, bias, splitWeightsToAct, array);
		int fixedValue = weights.CheckedShape[0].FixedValue;
		for (int i = 0; i < fixedValue; i++)
		{
			fakeConvActParam.FusedClamp[i].Min = fusedClamp[new int[1]];
			fakeConvActParam.FusedClamp[i].Max = fusedClamp[new int[1] { 1 }];
		}
		Marker weights2 = Nncase.IR.F.Math.RangeOfMarker(Tensor.From(array, weights.CheckedShape), weightsRange).With(null, null, null, adaQuantInfo: weightsMarker.AdaQuantInfo, mixQuantInfo: weightsMarker.MixQuantInfo);
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.FakeConv2D(inputMarker, weights2, fakeConvActParam.ToFakeActData(), padding, stride, dilation, groups, 0f, fakeConvActParam), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Conv2D conv = (Conv2D)__result["conv"];
		Expr input = (Expr)__result["input"];
		TensorConst weights = (TensorConst)__result["weights"];
		TensorConst bias = (TensorConst)__result["bias"];
		Expr stride = (Expr)__result["stride"];
		Expr padding = (Expr)__result["padding"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Tensor<float> fusedClamp = ((TensorConst)__result["fusedClamp"]).Value.Cast<float>();
		Tensor<float> weightsRange = ((TensorConst)__result["weightsRange"]).Value.Cast<float>();
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		Marker inputMarker = (Marker)__result["inputMarker"];
		Marker outputMarker = (Marker)__result["outputMarker"];
		Marker weightsMarker = (Marker)__result["weightsMarker"];
		return GetReplace(call, conv, input, weights, bias, stride, padding, dilation, groups, fusedClamp, weightsRange, inputRange, outputRange, inputMarker, outputMarker, weightsMarker);
	}
}
