using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToGNNEConv2D : GNNEQuantRule
{
	private QuantizeManager? _quantize;

	private ConvQuantConf? _conf;

	private byte[]? _qWeightsQint8;

	private byte[]? _qWeightsBiasQint8;

	private ActParam2? _actParam;

	private ActParam2? _actParamQint8;

	private Tensor? _originWeights;

	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv", "call", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	public void FuseScale(DeQuantizeParam qint8_qp)
	{
		_actParamQint8.FusedScale(qint8_qp.Scale);
		QuantizeParam[] array = _quantize.WeightsByChannelQPQint8();
		float[] array2 = new float[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = QuantHelper.GetDeqParamFromQuantParam(array[i]).Scale;
		}
		_actParamQint8.FusedChannelScale(array2);
		_actParamQint8.FusedXs();
	}

	private static short GetPadValue(Tensor<float> padValue, QuantizeParam qP, DataType realType)
	{
		int num = (int)System.Math.Round(padValue.ToScalar() / qP.Scale + (float)qP.ZeroPoint);
		if (!(realType == DataTypes.UInt8))
		{
			if (!(realType == DataTypes.Int8))
			{
				return (short)num;
			}
			return (sbyte)num;
		}
		return (byte)num;
	}

	private Tensor LowerInit(Expr inputMarker, Tensor<float> inputRange, Expr weightsMarker, Tensor<float> weightsRange, Tensor<float> outputRange, Tensor weights, FakeConv2D fakeConv2D, bool isPdp0DW)
	{
		Tensor tensor = weights;
		Tensor<float> wRange = weightsRange;
		CompileOptions compileOptions = base.CompileSession.CompileOptions;
		_ = compileOptions.QuantizeOptions.BindQuantMethod;
		MixQuantInfo mixQuantInfo = ((Marker)inputMarker).MixQuantInfo;
		MixQuantInfo mixQuantInfo2 = ((Marker)weightsMarker).MixQuantInfo;
		DataType quantType = ((compileOptions.QuantizeOptions.QuantScheme == string.Empty) ? compileOptions.QuantizeOptions.QuantType : mixQuantInfo.MarkerQuantType);
		DataType dataType = ((compileOptions.QuantizeOptions.QuantScheme == string.Empty) ? compileOptions.QuantizeOptions.WQuantType : mixQuantInfo2.MarkerQuantType);
		if (isPdp0DW && compileOptions.QuantizeOptions.QuantType == DataTypes.Int16 && compileOptions.QuantizeOptions.QuantScheme == string.Empty)
		{
			quantType = DataTypes.UInt8;
		}
		if (isPdp0DW && compileOptions.QuantizeOptions.WQuantType == DataTypes.Int16 && compileOptions.QuantizeOptions.QuantScheme == string.Empty)
		{
			dataType = DataTypes.UInt8;
		}
		_conf = new ConvQuantConf
		{
			QuantType = quantType,
			UseMseQuantW = false,
			WQuantType = dataType
		};
		if (mixQuantInfo2 != null && mixQuantInfo2.DoSquant)
		{
			tensor = ((dataType == DataTypes.UInt8) ? ((K230Target.K230MixQuantInfo)mixQuantInfo2).U8FineTunedWeights.Value : ((!(dataType == DataTypes.Int8)) ? ((K230Target.K230MixQuantInfo)mixQuantInfo2).I16FineTunedWeights.Value : ((K230Target.K230MixQuantInfo)mixQuantInfo2).I8FineTunedWeights.Value));
		}
		if (dataType == DataTypes.UInt8 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.U8FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)((K230Target.K230MixQuantInfo)mixQuantInfo2).U8FineTunedWeightsRangesByChannel.Value;
		}
		if (dataType == DataTypes.Int8 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.I8FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)((K230Target.K230MixQuantInfo)mixQuantInfo2).I8FineTunedWeightsRangesByChannel.Value;
		}
		if (dataType == DataTypes.Int16 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.I16FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)((K230Target.K230MixQuantInfo)mixQuantInfo2).I16FineTunedWeightsRangesByChannel.Value;
		}
		_quantize = new QuantizeManager(inputRange, wRange, outputRange, tensor, _conf, is_matmul: false);
		_originWeights = tensor;
		_actParam = new ActParam2(fakeConv2D.ActParam);
		_actParamQint8 = new ActParam2(fakeConv2D.ActParam);
		return tensor;
	}

	private Expr? GetReplace(FakeConv2D fakeConv, Call call, Marker inputMarker, Expr input, Tensor<float> inputRange, Expr weightsMarker, Tensor weights, Tensor<float> weightsRange, Expr padding, Expr stride, Expr dilation, int groups, Expr outputMarker, Tensor<float> outputRange, Tensor<float> padValue, TensorConst act)
	{
		bool isPdp0DW = GNNETypePatternUtility.IsDepthWise(input, weights, groups) && weights.Shape[2].FixedValue <= 3 && weights.Shape[3].FixedValue <= 3 && inputMarker.Target is Call call2 && call2.Target is GNNEStore && call2[GNNEStore.Input] is Call call3 && call3.Target is GNNEConv2D;
		if (inputMarker != null && inputMarker.MixQuantInfo != null && inputMarker.MixQuantInfo.MarkerQuantType == DataTypes.Int16 && !((K230Target.K230MixQuantInfo)inputMarker.MixQuantInfo).PermitInt16Quant)
		{
			return null;
		}
		if (weightsMarker != null && ((Marker)weightsMarker).MixQuantInfo != null && ((Marker)weightsMarker).MixQuantInfo.MarkerQuantType == DataTypes.Int16 && !((K230Target.K230MixQuantInfo)((Marker)weightsMarker).MixQuantInfo).PermitInt16Quant)
		{
			return null;
		}
		if (inputMarker != null && inputMarker.MixQuantInfo != null && (inputMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float16 || inputMarker.MixQuantInfo.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		if (weightsMarker != null && ((Marker)weightsMarker).MixQuantInfo != null && (((Marker)weightsMarker).MixQuantInfo.MarkerQuantType == DataTypes.Float16 || ((Marker)weightsMarker).MixQuantInfo.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		if (inputMarker != null && inputMarker.MixQuantInfo != null && weightsMarker != null && ((Marker)weightsMarker).MixQuantInfo != null && inputMarker.MixQuantInfo.MarkerQuantType == DataTypes.Int16 && ((Marker)weightsMarker).MixQuantInfo.MarkerQuantType == DataTypes.Int16)
		{
			return null;
		}
		Tensor oldWeights = LowerInit(inputMarker, inputRange, weightsMarker, weightsRange, outputRange, weights, fakeConv, isPdp0DW);
		DataType quantType = _conf.QuantType;
		Call weightsBias = LoadWeightsBias();
		Expr weights2 = LoadWeights(oldWeights);
		Expr act2 = LoadAct();
		Expr input2 = LoadQuantIf(input, quantType, inputMarker);
		DeQuantizeParam ifDeqQuantParam = _quantize.GetIfDeqQuantParam(DataTypes.UInt8);
		FuseInt16Scale(ifDeqQuantParam);
		QuantizeParam ifQuantParam = _quantize.GetIfQuantParam(quantType);
		short padValue2 = GetPadValue(padValue, ifQuantParam, quantType);
		bool flag = _conf.QuantType == DataTypes.Int16 || _conf.WQuantType == DataTypes.Int16;
		Call call4 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEConv2D(DataTypes.Float16, input2, weights2, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, _qWeightsQint8), weightsBias, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, _qWeightsBiasQint8), act2, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, _actParamQint8.ToAct0Data()), ifQuantParam.ZeroPoint, 0, 0, Tensor.FromScalar(ifDeqQuantParam), padding, stride, dilation, groups, flag, padValue2, _actParam, _actParamQint8));
		call4.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
		return Nncase.IR.F.Math.RangeOfMarker(call4, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
	}

	private void FuseInt16Scale(DeQuantizeParam qint8Qp)
	{
		if (_conf.QuantType == DataTypes.Int16)
		{
			FuseScale(qint8Qp);
		}
		if (_conf.WQuantType == DataTypes.Int16)
		{
			FuseScale(qint8Qp);
		}
	}

	private Expr LoadQuantIf(Expr input, DataType realType, Expr inputMarker)
	{
		DeQuantizeParam ifDeqQuantParam = _quantize.GetIfDeqQuantParam(realType);
		if (input is Call)
		{
			Call call = (Call)input;
			if (call.Target is Dequantize && call.Arguments[0].CheckedDataType == realType)
			{
				QuantParam quantParam = ((TensorConst)call.Arguments[Dequantize.DequantParam.Index]).Value.ToScalar<QuantParam>();
				ifDeqQuantParam.ZeroPoint = quantParam.ZeroPoint;
				ifDeqQuantParam.Scale = quantParam.Scale;
			}
		}
		return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)realType, Nncase.IR.F.Math.Quantize(inputMarker, new QuantParam(ifDeqQuantParam.ZeroPoint, ifDeqQuantParam.Scale), realType));
	}

	private Expr LoadWeights(Tensor oldWeights, bool isPdp0dw = false)
	{
		if (_conf.WQuantType != DataTypes.Int16 || isPdp0dw)
		{
			QuantT[] array = _quantize.GetQuantWeights();
			_qWeightsQint8 = _quantize.GetQuantWeightsU8().ToArray();
			if (oldWeights.Shape != _originWeights.Shape)
			{
				QuantT[] second = Enumerable.Repeat(new QuantT(_conf.WQuantType == DataTypes.UInt8), oldWeights.Shape.Prod().FixedValue - array.Length).ToArray();
				array = array.Concat(second).ToArray();
			}
			DataType? obj = (isPdp0dw ? DataTypes.UInt8 : _conf.WQuantType);
			return Nncase.IR.K230.F.Tensors.GNNELoadW(input: (_conf.WQuantType == DataTypes.UInt8) ? ((Tensor)Tensor.From(array.Select((QuantT w) => w.U8).ToArray(), oldWeights.Shape)) : ((Tensor)Tensor.From(array.Select((QuantT w) => w.I8).ToArray(), oldWeights.Shape)), destType: (PrimType)obj);
		}
		short[] array2 = _quantize.GetQuantWeightsI16();
		_qWeightsQint8 = _quantize.GetQuantWeightsU8().ToArray();
		if (oldWeights.Shape != _originWeights.Shape)
		{
			short[] second2 = Enumerable.Repeat((short)0, oldWeights.Shape.Prod().FixedValue - array2.Length).ToArray();
			array2 = array2.Concat(second2).ToArray();
		}
		return Nncase.IR.K230.F.Tensors.GNNELoadW((PrimType)_conf.WQuantType, Tensor.From(array2, oldWeights.Shape));
	}

	private Call LoadWeightsBias()
	{
		byte[] array = _quantize.GetWeightsQuantBias();
		_qWeightsBiasQint8 = _quantize.GetWeightsQuantBiasQint8();
		if (_conf.WQuantType == DataTypes.Int16)
		{
			array = Enumerable.Repeat((byte)0, array.Length).ToArray();
		}
		PrimType uInt = DataTypes.UInt8;
		byte[] array2 = array;
		int[] obj = new int[4] { 1, 1, 1, 0 };
		obj[3] = array.Length;
		return Nncase.IR.K230.F.Tensors.GNNELoadW(uInt, Tensor.From(array2, obj));
	}

	private Expr LoadAct()
	{
		DeQuantizeParam ifDeqQuantParam = _quantize.GetIfDeqQuantParam(_conf.QuantType);
		_actParam.FusedScale(ifDeqQuantParam.Scale);
		_actParam.FusedChannelScale(_quantize.GetWeightsDeqScale());
		_actParam.FusedXs();
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, _actParam.ToAct0Data());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeConv2D fakeConv = (FakeConv2D)__result["fakeConv"];
		Call call = (Call)__result["call"];
		Marker inputMarker = (Marker)__result["inputMarker"];
		Expr input = (Expr)__result["input"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		Tensor value = ((TensorConst)__result["weights"]).Value;
		Tensor<float> weightsRange = ((TensorConst)__result["weightsRange"]).Value.Cast<float>();
		Expr padding = (Expr)__result["padding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Expr outputMarker = (Expr)__result["outputMarker"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		Tensor<float> padValue = ((TensorConst)__result["padValue"]).Value.Cast<float>();
		TensorConst act = (TensorConst)__result["act"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeConv, call, inputMarker, input, inputRange, weightsMarker, value, weightsRange, padding, stride, dilation, groups, outputMarker, outputRange, padValue, act);
	}
}
