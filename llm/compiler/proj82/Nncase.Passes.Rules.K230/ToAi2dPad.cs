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
public sealed class ToAi2dPad : GNNEQuantRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.K230.IsFakeAi2dPad("fakeAi2dPad", "call", (FakeAi2dPad p) => p.Mode != PadMode.Constant, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("value")), Nncase.PatternMatch.Utility.IsConst("outputRange"));


	private Expr? GetReplace(FakeAi2dPad fakeAi2dPad, Call call, Tensor<float> inputRange, Expr inputMarker, Expr outputMarker, Expr padding, float value, Expr outputRange)
	{
		MixQuantInfo mixQuantInfo = ((Marker)inputMarker).MixQuantInfo;
		DataType dataType = ((mixQuantInfo?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo.MarkerQuantType);
		QuantMode quantMode = ((!(dataType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((dataType == DataTypes.Int16) ? 12 : 8);
		QuantParam quantParam = QuantUtility.GetQuantParam(new ValueRange<float>(inputRange.ToArray<float>()[0], inputRange.ToArray<float>()[1]), bits, quantMode);
		Call input = Nncase.IR.F.Math.Quantize(inputMarker, quantParam, dataType);
		Call input2 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input);
		float num = value / quantParam.Scale + (float)quantParam.ZeroPoint;
		Call input3 = Nncase.IR.K230.F.Tensors.Ai2dPad(input2, padding, ((PrimType)dataType == DataTypes.UInt8) ? ((Expr)(byte)num) : (((PrimType)dataType == DataTypes.Int8) ? ((Expr)(sbyte)num) : (((PrimType)dataType == DataTypes.Int16) ? ((Expr)(short)num) : ((Expr)(Half)value))), quantParam.ZeroPoint, new QuantParam(quantParam.ZeroPoint, 1f / quantParam.Scale), (PrimType)dataType, fakeAi2dPad.Mode);
		Call call2 = Nncase.IR.K230.F.Tensors.GNNEStore((PrimType)dataType, input3);
		call2.CheckedType = new TensorType(dataType, call.CheckedShape);
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Math.Dequantize(call2, Tensor.FromScalar(new QuantParam(quantParam.ZeroPoint, quantParam.Scale)), call.CheckedDataType), outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeAi2dPad fakeAi2dPad = (FakeAi2dPad)__result["fakeAi2dPad"];
		Call call = (Call)__result["call"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr outputMarker = (Expr)__result["outputMarker"];
		Expr padding = (Expr)__result["padding"];
		float value = ((TensorConst)__result["value"]).Value.ToScalar<float>();
		Expr outputRange = (Expr)__result["outputRange"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeAi2dPad, call, inputRange, inputMarker, outputMarker, padding, value, outputRange);
	}
}
