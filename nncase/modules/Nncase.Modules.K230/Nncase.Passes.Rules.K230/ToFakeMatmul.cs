using System;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToFakeMatmul : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.Math.IsMatMul("matmul", "call", (MatMul _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputAMarker", Nncase.PatternMatch.Utility.IsWildcard("inputA")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("inputARange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("inputBMarker", Nncase.PatternMatch.Utility.IsWildcard("inputB")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("inputBRange"))), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(Marker inputAMarker, Expr inputA, Marker inputBMarker, Expr inputB, Marker outputMarker, Call call, Tensor<float> inputARange, Tensor<float> inputBRange, Tensor<float> outputRange)
	{
		if (inputAMarker.MixQuantInfo != null && (inputAMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float16 || inputAMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		if (inputBMarker.MixQuantInfo != null && (inputBMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float16 || inputBMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(inputAMarker.CheckedShape.ToValueArray(), 0, array, array.Length - inputAMarker.CheckedShape.Count, inputAMarker.CheckedShape.Count);
		int[] array2 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(inputBMarker.CheckedShape.ToValueArray(), 0, array2, array2.Length - inputBMarker.CheckedShape.Count, inputBMarker.CheckedShape.Count);
		int[] array3 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(call.CheckedShape.ToValueArray(), 0, array3, array3.Length - call.CheckedShape.Count, call.CheckedShape.Count);
		ActParam2 actParam = new ActParam2(array3[1] * array3[2], new QuantParam(0, 1f));
		Marker inputA2 = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(inputAMarker, array), inputARange).With(null, null, null, adaQuantInfo: inputAMarker.AdaQuantInfo, mixQuantInfo: inputAMarker.MixQuantInfo);
		Marker inputB2 = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(inputBMarker, array2), inputBRange).With(null, null, null, adaQuantInfo: inputBMarker.AdaQuantInfo, mixQuantInfo: inputBMarker.MixQuantInfo);
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.FakeMatMul(inputA2, inputB2, actParam.ToFakeActData(), actParam), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo), call.CheckedShape), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Marker inputAMarker = (Marker)__result["inputAMarker"];
		Expr inputA = (Expr)__result["inputA"];
		Marker inputBMarker = (Marker)__result["inputBMarker"];
		Expr inputB = (Expr)__result["inputB"];
		Marker outputMarker = (Marker)__result["outputMarker"];
		Call call = (Call)__result["call"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Tensor<float> inputBRange = ((TensorConst)__result["inputBRange"]).Value.Cast<float>();
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		return GetReplace(inputAMarker, inputA, inputBMarker, inputB, outputMarker, call, inputARange, inputBRange, outputRange);
	}
}
