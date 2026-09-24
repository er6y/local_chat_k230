using System;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToAi2dResize : GNNEQuantRule
{
	public override Pattern Pattern { get; }

	private Expr? GetReplace(FakeAi2dResize resize, Call call, Tensor<float> inputRange, Expr input, Expr inputMarker, Expr newSize, Expr outputRange)
	{
		MixQuantInfo mixQuantInfo = ((Marker)inputMarker).MixQuantInfo;
		DataType dataType = ((mixQuantInfo?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo.MarkerQuantType);
		QuantMode quantMode = ((!(dataType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((dataType == DataTypes.Int16) ? 12 : 8);
		QuantParam quantParam = QuantUtility.GetQuantParam(new ValueRange<float>(inputRange.ToArray<float>()[0], inputRange.ToArray<float>()[1]), bits, quantMode);
		if (input.CheckedDataType == DataTypes.Float32)
		{
			Call input2 = Nncase.IR.F.Math.Quantize(input, quantParam, dataType);
			Call input3 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input2);
			Call input4 = Nncase.IR.K230.F.Tensors.Ai2dResize((PrimType)dataType, resize.ResizeMethod, resize.AlignCorners, resize.HalfPixelCenters, input3, newSize, quantParam.ZeroPoint, new QuantParam(quantParam.ZeroPoint, 1f / quantParam.Scale));
			Call call2 = Nncase.IR.F.Math.Dequantize(Nncase.IR.K230.F.Tensors.GNNEStore(dataType, input4), Tensor.FromScalar(new QuantParam(quantParam.ZeroPoint, quantParam.Scale)), call.CheckedDataType);
			call2.CheckedType = new TensorType(call.CheckedDataType, call.CheckedShape);
			return Nncase.IR.F.Math.RangeOfMarker(call2, outputRange);
		}
		if (input.CheckedDataType == DataTypes.UInt8 || input.CheckedDataType == DataTypes.Int8)
		{
			Call input5 = Nncase.IR.K230.F.Tensors.Ai2dResize((PrimType)dataType, resize.ResizeMethod, resize.AlignCorners, resize.HalfPixelCenters, Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input), newSize, quantParam.ZeroPoint, new QuantParam(quantParam.ZeroPoint, 1f / quantParam.Scale));
			return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.GNNEStore(dataType, input5), outputRange);
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeAi2dResize resize = (FakeAi2dResize)__result["resize"];
		Call call = (Call)__result["call"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Expr input = (Expr)__result["input"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr newSize = (Expr)__result["newSize"];
		Expr outputRange = (Expr)__result["outputRange"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(resize, call, inputRange, input, inputMarker, newSize, outputRange);
	}

	public ToAi2dResize()
	{
		Func<FakeAi2dResize, bool> condition = (FakeAi2dResize _) => true;
		Pattern = Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.F.K230.IsFakeAi2dResize("resize", "call", condition, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsTensorConst("newSize"))with
		{
			TypePattern = TypePatternUtility.HasRank(4)
		}, Nncase.PatternMatch.Utility.IsConst("outputRange"));
	}
}
