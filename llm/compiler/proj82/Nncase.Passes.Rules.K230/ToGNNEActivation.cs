using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToGNNEActivation : GNNEActLowerRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.K230.IsFakeActivation("fakeActivation", "call", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputAMarker", Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsTensorConst("inputARange")), Nncase.PatternMatch.Utility.IsAlt(Nncase.PatternMatch.Utility.IsRangeOfMarker("inputBMarker", Nncase.PatternMatch.Utility.IsWildcard("inputB"), Nncase.PatternMatch.Utility.IsTensorConst("inputBRange")), Nncase.PatternMatch.Utility.IsNone()), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard("is16Segment")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	public Expr? GetReplace(FakeActivation fakeActivation, Call call, Expr inputA, Tensor<float> inputARange, Tensor<int> outChannels, Expr is16Segment, Expr inputAMarker, Expr outputMarker, IMatchResult result, Expr outputRange)
	{
		Call act = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, fakeActivation.ActParam.ToAct1Data());
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(inputA.CheckedShape.ToValueArray(), 0, array, array.Length - inputA.CheckedShape.Count, inputA.CheckedShape.Count);
		int[] array2 = new int[4] { 1, 1, 1, 1 };
		int[] array3 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(call.CheckedShape.ToValueArray(), 0, array3, array3.Length - call.CheckedShape.Count, call.CheckedShape.Count);
		bool flag = array3.Length != call.CheckedShape.Count;
		try
		{
			Expr expr = (Expr)result["inputB"];
			Expr expr2 = (Expr)result["inputBMarker"];
			Tensor value = ((TensorConst)result["inputBRange"]).Value;
			Call call2 = Nncase.IR.F.Tensors.Reshape(inputAMarker, array);
			Call call3 = Nncase.IR.F.Tensors.Reshape(expr2, array2);
			Array.Copy(expr.CheckedShape.ToValueArray(), 0, array2, array2.Length - expr.CheckedShape.Count, expr.CheckedShape.Count);
			if (expr is TensorConst)
			{
				MixQuantInfo mixQuantInfo = ((Marker)inputAMarker).MixQuantInfo;
				DataType dataType = ((mixQuantInfo?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo.MarkerQuantType);
				QuantMode quantMode = ((!(dataType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
				int bits = ((dataType == DataTypes.Int16) ? 12 : 8);
				ValueRange<float> range = new ValueRange<float>(inputARange.ToArray<float>()[0], inputARange.ToArray<float>()[1]);
				QuantParam quantParam = ((dataType == DataTypes.Float16 || dataType == DataTypes.Float32 || dataType == DataTypes.Int16) ? new QuantParam(0, 1f) : QuantUtility.GetQuantParam(range, bits, quantMode));
				Call input = ((dataType == DataTypes.Float16 || dataType == DataTypes.Float32 || dataType == DataTypes.Int16) ? call2 : Nncase.IR.F.Math.Quantize(call2, new QuantParam(quantParam.ZeroPoint, quantParam.Scale), dataType));
				Call inputa = ((dataType == DataTypes.Float16 || dataType == DataTypes.Float32 || dataType == DataTypes.Int16) ? Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, call2) : Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input));
				if (base.InputA is TensorConst)
				{
					inputa = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, Const.FromTensor(Tensor.From(((TensorConst)base.InputA).Value.ToArray<float>(), array)));
				}
				Call inputb = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, call3);
				if (base.InputB is TensorConst)
				{
					inputb = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, Const.FromTensor(Tensor.From(((TensorConst)base.InputB).Value.ToArray<float>(), array2)));
				}
				if (!GetReplaceHelper.ToScalar<bool>(is16Segment))
				{
					Call call4 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa, inputb, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
					{
						new DeQuantizeParam(quantParam.ZeroPoint, quantParam.Scale)
					}), Tensor.FromArray(new DeQuantizeParam[1]
					{
						new DeQuantizeParam(0, 1f)
					}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
					call4.CheckedType = new TensorType(DataTypes.Float32, array3);
					Call call5 = Nncase.IR.F.Tensors.Reshape(call4, call.CheckedShape);
					call5.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
					return Nncase.IR.F.Math.RangeOfMarker(flag ? call5 : call4, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
				}
				Call call6 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa, inputb, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(quantParam.ZeroPoint, quantParam.Scale)
				}), Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(0, 1f)
				}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
				call6.CheckedType = new TensorType(DataTypes.Float32, array3);
				Call call7 = Nncase.IR.F.Tensors.Reshape(call6, call.CheckedShape);
				call7.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
				return Nncase.IR.F.Math.RangeOfMarker(flag ? call7 : call6, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
			}
			MixQuantInfo mixQuantInfo2 = ((Marker)inputAMarker).MixQuantInfo;
			DataType dataType2 = ((mixQuantInfo2?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo2.MarkerQuantType);
			QuantMode quantMode2 = ((!(dataType2 == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			int bits2 = ((dataType2 == DataTypes.Int16) ? 12 : 8);
			MixQuantInfo mixQuantInfo3 = ((Marker)expr2).MixQuantInfo;
			DataType dataType3 = ((mixQuantInfo3?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo3.MarkerQuantType);
			QuantMode quantMode3 = ((!(dataType3 == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			int bits3 = ((dataType3 == DataTypes.Int16) ? 12 : 8);
			ValueRange<float> range2 = new ValueRange<float>(inputARange.ToArray<float>()[0], inputARange.ToArray<float>()[1]);
			QuantParam quantParam2 = ((dataType2 == DataTypes.Float16 || dataType2 == DataTypes.Float32 || dataType2 == DataTypes.Int16) ? new QuantParam(0, 1f) : QuantUtility.GetQuantParam(range2, bits2, quantMode2));
			Call input2 = ((dataType2 == DataTypes.Float16 || dataType2 == DataTypes.Float32 || dataType2 == DataTypes.Int16) ? call2 : Nncase.IR.F.Math.Quantize(call2, new QuantParam(quantParam2.ZeroPoint, quantParam2.Scale), dataType2));
			Call inputa2 = ((dataType2 == DataTypes.Float16 || dataType2 == DataTypes.Float32 || dataType2 == DataTypes.Int16) ? Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, call2) : Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType2, input2));
			ValueRange<float> range3 = new ValueRange<float>(value.ToArray<float>()[0], value.ToArray<float>()[1]);
			QuantParam quantParam3 = ((dataType3 == DataTypes.Float16 || dataType3 == DataTypes.Float32 || dataType3 == DataTypes.Int16) ? new QuantParam(0, 1f) : QuantUtility.GetQuantParam(range3, bits3, quantMode3));
			Call input3 = ((dataType3 == DataTypes.Float16 || dataType3 == DataTypes.Float32 || dataType3 == DataTypes.Int16) ? call3 : Nncase.IR.F.Math.Quantize(call3, new QuantParam(quantParam3.ZeroPoint, quantParam3.Scale), dataType3));
			Call inputb2 = ((dataType3 == DataTypes.Float16 || dataType3 == DataTypes.Float32 || dataType3 == DataTypes.Int16) ? Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, call3) : Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType3, input3));
			if (!GetReplaceHelper.ToScalar<bool>(is16Segment))
			{
				Call call8 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa2, inputb2, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(quantParam2.ZeroPoint, quantParam2.Scale)
				}), Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(quantParam3.ZeroPoint, quantParam3.Scale)
				}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
				call8.CheckedType = new TensorType(DataTypes.Float32, array3);
				Call call9 = Nncase.IR.F.Tensors.Reshape(call8, call.CheckedShape);
				call9.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
				return Nncase.IR.F.Math.RangeOfMarker(flag ? call9 : call8, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
			}
			Call call10 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa2, inputb2, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
			{
				new DeQuantizeParam(quantParam2.ZeroPoint, quantParam2.Scale)
			}), Tensor.FromArray(new DeQuantizeParam[1]
			{
				new DeQuantizeParam(quantParam3.ZeroPoint, quantParam3.Scale)
			}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
			call10.CheckedType = new TensorType(DataTypes.Float32, array3);
			Call call11 = Nncase.IR.F.Tensors.Reshape(call10, call.CheckedShape);
			call11.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
			return Nncase.IR.F.Math.RangeOfMarker(flag ? call11 : call10, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
		}
		catch (KeyNotFoundException)
		{
			MixQuantInfo mixQuantInfo4 = ((Marker)inputAMarker).MixQuantInfo;
			DataType dataType4 = ((mixQuantInfo4?.MarkerQuantType == null) ? base.QuantType : mixQuantInfo4.MarkerQuantType);
			QuantMode quantMode4 = ((!(dataType4 == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			int bits4 = ((dataType4 == DataTypes.Int16) ? 12 : 8);
			Marker marker = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(inputAMarker, array), inputARange);
			ValueRange<float> range4 = new ValueRange<float>(inputARange.ToArray<float>()[0], inputARange.ToArray<float>()[1]);
			QuantParam quantParam4 = ((dataType4 == DataTypes.Float16 || dataType4 == DataTypes.Float32 || dataType4 == DataTypes.Int16) ? new QuantParam(0, 1f) : QuantUtility.GetQuantParam(range4, bits4, quantMode4));
			Expr input4 = ((dataType4 == DataTypes.Float16 || dataType4 == DataTypes.Float32 || dataType4 == DataTypes.Int16) ? ((Expr)marker) : ((Expr)Nncase.IR.F.Math.Quantize(marker, new QuantParam(quantParam4.ZeroPoint, quantParam4.Scale), dataType4)));
			Call inputa3 = ((dataType4 == DataTypes.Float16 || dataType4 == DataTypes.Float32 || dataType4 == DataTypes.Int16) ? Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, marker) : Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType4, input4));
			if (!GetReplaceHelper.ToScalar<bool>(is16Segment))
			{
				Call call12 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa3, None.Default, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(quantParam4.ZeroPoint, quantParam4.Scale)
				}), Tensor.FromArray(new DeQuantizeParam[1]
				{
					new DeQuantizeParam(0, 1f)
				}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
				call12.CheckedType = new TensorType(DataTypes.Float32, array3);
				Call call13 = Nncase.IR.F.Tensors.Reshape(call12, call.CheckedShape);
				call13.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
				return Nncase.IR.F.Math.RangeOfMarker(flag ? call13 : call12, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
			}
			Call call14 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(inputa3, None.Default, act, 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
			{
				new DeQuantizeParam(quantParam4.ZeroPoint, quantParam4.Scale)
			}), Tensor.FromArray(new DeQuantizeParam[1]
			{
				new DeQuantizeParam(0, 1f)
			}), outChannels, fakeActivation.Type, is16Segment, DataTypes.Float16, fakeActivation.ActParam, array3));
			call14.CheckedType = new TensorType(DataTypes.Float32, array3);
			Call call15 = Nncase.IR.F.Tensors.Reshape(call14, call.CheckedShape);
			call15.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
			return Nncase.IR.F.Math.RangeOfMarker(flag ? call15 : call14, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
		}
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeActivation fakeActivation = (FakeActivation)__result["fakeActivation"];
		Call call = (Call)__result["call"];
		Expr inputA = (Expr)__result["inputA"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Tensor<int> outChannels = ((TensorConst)__result["outChannels"]).Value.Cast<int>();
		Expr is16Segment = (Expr)__result["is16Segment"];
		Expr inputAMarker = (Expr)__result["inputAMarker"];
		Expr outputMarker = (Expr)__result["outputMarker"];
		Expr outputRange = (Expr)__result["outputRange"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeActivation, call, inputA, inputARange, outChannels, is16Segment, inputAMarker, outputMarker, __result, outputRange);
	}
}
