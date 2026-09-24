using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Quantization.K230;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToGNNEMatMul : GNNEDIFQuantRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.F.K230.IsFakeMatMul("fakeMatmul", "call", (FakeMatMul _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputAMarker", Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsConst("inputARange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("inputBMarker", Nncase.PatternMatch.Utility.IsWildcard("inputB"), Nncase.PatternMatch.Utility.IsConst("inputBRange"))), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(FakeMatMul fakeMatmul, Call call, Expr inputA, Tensor<float> inputARange, Expr inputAMarker, Expr inputB, Tensor<float> inputBRange, Expr inputBMarker, Tensor<float> outputRange)
	{
		ActParam2 actParam = new ActParam2(fakeMatmul.ActParam2);
		MixQuantInfo mixQuantInfo = ((Marker)inputAMarker).MixQuantInfo;
		MixQuantInfo mixQuantInfo2 = ((Marker)inputBMarker).MixQuantInfo;
		DataType dataType = ((mixQuantInfo?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo.MarkerQuantType);
		if (dataType == DataTypes.Int16)
		{
			dataType = DataTypes.UInt8;
		}
		DataType dataType2 = ((mixQuantInfo2?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo2.MarkerQuantType);
		Call inputA2 = LoadQuantInA(inputARange, dataType, inputAMarker);
		Call inputB2 = ((inputB is TensorConst) ? LoadInB((TensorConst)inputB, inputBRange.AsValueRange(), dataType2) : LoadQuantInB(inputB, inputBRange, dataType2, inputBMarker));
		Call inputABias = LoadInABias(inputA, inputARange.AsValueRange(), dataType);
		Call act = LoadAct(actParam, inputARange.AsValueRange(), inputBRange.AsValueRange(), dataType, dataType2);
		Call input = Nncase.IR.K230.F.Tensors.GNNEMatMul(actParam, DataTypes.Float16, inputA2, inputB2, act, inputABias, 0, 0, 0, Enumerable.Repeat(GetInBDeqQuantParam(dataType2, inputBRange.AsValueRange()).ZeroPoint, call.CheckedShape[1].FixedValue).ToArray());
		Call call2 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, input);
		call2.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
		return Nncase.IR.F.Math.RangeOfMarker(call2, outputRange);
	}

	private Call LoadQuantInA(Tensor<float> inARange, DataType quanType, Expr prevInputMarker)
	{
		DeQuantizeParam inADeqQuantParam = GetInADeqQuantParam(quanType, inARange.AsValueRange());
		Call input = Nncase.IR.F.Math.Quantize(prevInputMarker, Tensor.FromScalar(new QuantParam(inADeqQuantParam.ZeroPoint, inADeqQuantParam.Scale)), quanType);
		return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)quanType, input);
	}

	private Call LoadQuantInB(Expr prevInput, Tensor<float> inBRange, DataType quanType, Expr prevInputMarker)
	{
		DeQuantizeParam inBDeqQuantParam = GetInBDeqQuantParam(quanType, inBRange.AsValueRange());
		int num = 4 - prevInput.CheckedShape.Rank;
		if (num > 0)
		{
			Nncase.IR.F.Tensors.Unsqueeze(prevInput, Enumerable.Range(0, num).ToArray());
		}
		Call input = Nncase.IR.F.Math.Quantize(prevInputMarker, Tensor.FromScalar(new QuantParam(inBDeqQuantParam.ZeroPoint, inBDeqQuantParam.Scale)), quanType);
		return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)quanType, input);
	}

	private Call LoadInABias(Expr prevInput, ValueRange<float> inARange, DataType quanType)
	{
		DeQuantizeParam inADeqQuantParam = GetInADeqQuantParam(quanType, inARange);
		byte[] array = new byte[prevInput.CheckedShape[1].FixedValue * prevInput.CheckedShape[2].FixedValue];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = checked((byte)inADeqQuantParam.ZeroPoint);
		}
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, array);
	}

	private Call LoadInB(TensorConst inputb, ValueRange<float> inBRange, DataType quanType)
	{
		if (quanType == DataTypes.Int8)
		{
			Tensor<sbyte> tensor = new Tensor<sbyte>(GetQuantInBI8(inputb, inBRange, quanType), inputb.CheckedShape);
			return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)quanType, tensor);
		}
		if (quanType == DataTypes.UInt8)
		{
			Tensor<byte> tensor2 = new Tensor<byte>(GetQuantInBu8(inputb, inBRange, quanType), inputb.CheckedShape);
			return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)quanType, tensor2);
		}
		if (quanType == DataTypes.Int16)
		{
			Tensor<short> tensor3 = new Tensor<short>(GetQuantInBi16(inputb, inBRange, quanType), inputb.CheckedShape);
			return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)quanType, tensor3);
		}
		throw new NotSupportedException("Incalid in_b_quant_type");
	}

	private byte[] GetQuantInBu8(TensorConst inputB, ValueRange<float> inBRange, DataType quanType)
	{
		Shape checkedShape = inputB.CheckedShape;
		float[] array = inputB.Value.ToArray<float>();
		byte[] array2 = new byte[array.Length];
		QuantizeParam inBQuantParam = GetInBQuantParam(quanType, inBRange);
		for (int i = 0; i < checkedShape[1].FixedValue; i++)
		{
			for (int j = 0; j < checkedShape[2].FixedValue; j++)
			{
				for (int k = 0; k < checkedShape[3].FixedValue; k++)
				{
					int num = i * checkedShape[2].FixedValue * checkedShape[3].FixedValue + j * checkedShape[3].FixedValue + k;
					array2[num] = Quantize<byte>(array[num], inBQuantParam);
				}
			}
		}
		return array2;
	}

	private sbyte[] GetQuantInBI8(TensorConst inputB, ValueRange<float> inBRange, DataType quanType)
	{
		Shape checkedShape = inputB.CheckedShape;
		float[] array = inputB.Value.ToArray<float>();
		sbyte[] array2 = new sbyte[array.Length];
		QuantizeParam inBQuantParam = GetInBQuantParam(quanType, inBRange);
		for (int i = 0; i < checkedShape[1].FixedValue; i++)
		{
			for (int j = 0; j < checkedShape[2].FixedValue; j++)
			{
				for (int k = 0; k < checkedShape[3].FixedValue; k++)
				{
					int num = i * checkedShape[2].FixedValue * checkedShape[3].FixedValue + j * checkedShape[3].FixedValue + k;
					array2[num] = (sbyte)System.Math.Clamp(Convert.ToInt32(Quantize<sbyte>(array[num], inBQuantParam)), -127, 127);
				}
			}
		}
		return array2;
	}

	private short[] GetQuantInBi16(TensorConst inputB, ValueRange<float> inBRange, DataType quanType)
	{
		Shape checkedShape = inputB.CheckedShape;
		float[] array = inputB.Value.ToArray<float>();
		short[] array2 = new short[array.Length];
		QuantizeParam inBQuantParam = GetInBQuantParam(quanType, inBRange);
		for (int i = 0; i < checkedShape[1].FixedValue; i++)
		{
			for (int j = 0; j < checkedShape[2].FixedValue; j++)
			{
				for (int k = 0; k < checkedShape[3].FixedValue; k++)
				{
					int num = i * checkedShape[2].FixedValue * checkedShape[3].FixedValue + j * checkedShape[3].FixedValue + k;
					array2[num] = Quantize<short>(array[num], inBQuantParam);
				}
			}
		}
		return array2;
	}

	private T Quantize<T>(float data, QuantizeParam qp)
	{
		return (T)Convert.ChangeType(System.Math.Clamp((int)System.Math.Round(data * qp.Scale + (float)qp.ZeroPoint), Convert.ToInt32(typeof(T).GetField("MinValue").GetValue(null)), Convert.ToInt32(typeof(T).GetField("MaxValue").GetValue(null))), typeof(T));
	}

	private Call LoadAct(ActParam2 actParam, ValueRange<float> inARange, ValueRange<float> inBRange, DataType quanTypeA, DataType quanTypeB)
	{
		DeQuantizeParam inADeqQuantParam = GetInADeqQuantParam(quanTypeA, inARange);
		DeQuantizeParam inBDeqQuantParam = GetInBDeqQuantParam(quanTypeB, inBRange);
		actParam.FusedChannelScale(Enumerable.Repeat(inADeqQuantParam.Scale, actParam.Channels).ToArray());
		actParam.FusedChannelScale(Enumerable.Repeat(inBDeqQuantParam.Scale, actParam.Channels).ToArray());
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam.ToAct0Data());
	}

	private DeQuantizeParam GetInADeqQuantParam(DataType quantType, ValueRange<float> inARange)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((quantType == DataTypes.Int16) ? 12 : 8);
		return Nncase.Quantization.K230.Utility.GetDeqParam(QuantUtility.GetQuantParam(inARange, bits, quantMode));
	}

	private QuantizeParam GetInBQuantParam(DataType quantType, ValueRange<float> inBRange)
	{
		return Nncase.Quantization.K230.Utility.GetQuantParamFromDeqParam(GetInBDeqQuantParam(quantType, inBRange));
	}

	private DeQuantizeParam GetInBDeqQuantParam(DataType quantType, ValueRange<float> inBRange)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((quantType == DataTypes.Int16) ? 12 : 8);
		return Nncase.Quantization.K230.Utility.GetDeqParam(QuantUtility.GetQuantParam(inBRange, bits, quantMode));
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeMatMul fakeMatmul = (FakeMatMul)__result["fakeMatmul"];
		Call call = (Call)__result["call"];
		Expr inputA = (Expr)__result["inputA"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Expr inputAMarker = (Expr)__result["inputAMarker"];
		Expr inputB = (Expr)__result["inputB"];
		Tensor<float> inputBRange = ((TensorConst)__result["inputBRange"]).Value.Cast<float>();
		Expr inputBMarker = (Expr)__result["inputBMarker"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeMatmul, call, inputA, inputARange, inputAMarker, inputB, inputBRange, inputBMarker, outputRange);
	}
}
