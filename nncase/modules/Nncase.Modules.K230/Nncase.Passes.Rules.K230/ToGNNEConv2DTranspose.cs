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
public class ToGNNEConv2DTranspose : GNNEQuantRule
{
	private QuantizeManager _quantize;

	private ConvQuantConf? _conf;

	private byte[]? _qWeightsQint8;

	private byte[]? _qWeightsBiasQint8;

	private ActParam2? _actParam;

	private ActParam2? _actParamQint8;

	private Tensor? _originWeights;

	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.K230.IsFakeConv2DTranspose("fakeConvTranspose", "call", (FakeConv2DTranspose _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("outputshape"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("outputPadding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


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

	private void FuseScale(DeQuantizeParam qint8Qp)
	{
		_actParamQint8.FusedScale(qint8Qp.Scale);
		QuantizeParam[] array = _quantize.WeightsByChannelQPQint8();
		float[] array2 = new float[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = QuantHelper.GetDeqParamFromQuantParam(array[i]).Scale;
		}
		_actParamQint8.FusedChannelScale(array2);
		_actParamQint8.FusedXs();
	}

	private Tensor LowerInit(Expr inputMarker, Tensor<float> inputRange, Expr weightsMarker, Tensor<float> weightsRange, Tensor<float> outputRange, Tensor weights, TensorConst act, FakeConv2DTranspose fakeConv2DTranspose, int groupsConvTranspose = 1)
	{
		Tensor tensor = weights;
		Tensor<float> wRange = weightsRange;
		CompileOptions compileOptions = base.CompileSession.CompileOptions;
		_ = compileOptions.QuantizeOptions.BindQuantMethod;
		MixQuantInfo mixQuantInfo = ((Marker)inputMarker).MixQuantInfo;
		MixQuantInfo mixQuantInfo2 = ((Marker)weightsMarker).MixQuantInfo;
		DataType quantType = ((mixQuantInfo?.MarkerQuantType == null) ? compileOptions.QuantizeOptions.QuantType : mixQuantInfo.MarkerQuantType);
		DataType dataType = ((mixQuantInfo2?.MarkerQuantType == null) ? compileOptions.QuantizeOptions.WQuantType : mixQuantInfo2.MarkerQuantType);
		_conf = new ConvQuantConf
		{
			QuantType = quantType,
			UseMseQuantW = false,
			WQuantType = dataType
		};
		if (mixQuantInfo2 != null && mixQuantInfo2.DoSquant)
		{
			tensor = ((dataType == DataTypes.UInt8) ? ((K230Target.K230MixQuantInfo)mixQuantInfo2).U8FineTunedWeights?.Value : ((!(dataType == DataTypes.Int8)) ? ((K230Target.K230MixQuantInfo)mixQuantInfo2).I16FineTunedWeights?.Value : ((K230Target.K230MixQuantInfo)mixQuantInfo2).I8FineTunedWeights?.Value));
		}
		if (dataType == DataTypes.UInt8 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.U8FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)(((K230Target.K230MixQuantInfo)mixQuantInfo2).U8FineTunedWeightsRangesByChannel?.Value);
		}
		if (dataType == DataTypes.Int8 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.I8FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)(((K230Target.K230MixQuantInfo)mixQuantInfo2).I8FineTunedWeightsRangesByChannel?.Value);
		}
		if (dataType == DataTypes.Int16 && ((K230Target.K230MixQuantInfo)mixQuantInfo2)?.I16FineTunedWeightsRangesByChannel != null)
		{
			wRange = (Tensor<float>)(((K230Target.K230MixQuantInfo)mixQuantInfo2).I16FineTunedWeightsRangesByChannel?.Value);
		}
		_quantize = new QuantizeManager(inputRange, wRange, outputRange, tensor, _conf, is_matmul: false, groupsConvTranspose);
		_originWeights = tensor;
		_actParam = new ActParam2(fakeConv2DTranspose.ActParam);
		_actParamQint8 = new ActParam2(fakeConv2DTranspose.ActParam);
		for (int i = 0; i < ((TensorConst)(Expr)weights).CheckedShape[0].FixedValue; i++)
		{
			_actParam.Ks[0, i] = act.Value.ToArray<float>()[i * 7];
			_actParam.Ks[1, i] = act.Value.ToArray<float>()[i * 7 + 1];
			_actParam.Bs[0, i] = act.Value.ToArray<float>()[i * 7 + 2];
			_actParam.Bs[1, i] = act.Value.ToArray<float>()[i * 7 + 3];
			_actParam.FusedClamp[i].Min = act.Value.ToArray<float>()[i * 7 + 4];
			_actParam.FusedClamp[i].Max = act.Value.ToArray<float>()[i * 7 + 5];
			_actParam.Xs[0, i] = act.Value.ToArray<float>()[i * 7 + 6];
			_actParamQint8.Ks[0, i] = act.Value.ToArray<float>()[i * 7];
			_actParamQint8.Ks[1, i] = act.Value.ToArray<float>()[i * 7 + 1];
			_actParamQint8.Bs[0, i] = act.Value.ToArray<float>()[i * 7 + 2];
			_actParamQint8.Bs[1, i] = act.Value.ToArray<float>()[i * 7 + 3];
			_actParamQint8.FusedClamp[i].Min = act.Value.ToArray<float>()[i * 7 + 4];
			_actParamQint8.FusedClamp[i].Max = act.Value.ToArray<float>()[i * 7 + 5];
			_actParamQint8.Xs[0, i] = act.Value.ToArray<float>()[i * 7 + 6];
		}
		return tensor;
	}

	private Expr? GetReplace(FakeConv2DTranspose fakeConvTranspose, Call call, Expr inputMarker, Expr input, Tensor<float> inputRange, Expr weightsMarker, Tensor weights, Tensor<float> weightsRange, TensorConst act, Expr outputshape, Expr padding, Expr outputPadding, Expr stride, Expr dilation, int groups, Expr outputMarker, Tensor<float> outputRange, Tensor<float> padValue)
	{
		Tensor oldWeights = LowerInit(inputMarker, inputRange, weightsMarker, weightsRange, outputRange, weights, act, fakeConvTranspose, groups);
		DataType dataType = (false ? DataTypes.UInt8 : _conf.QuantType);
		Call weightsBias = LoadWeightsBias();
		Expr act2 = LoadAct();
		Expr input2 = LoadQuantIf(input, dataType, inputMarker);
		DeQuantizeParam ifDeqQuantParam = _quantize.GetIfDeqQuantParam(DataTypes.UInt8);
		FuseInt16Scale(ifDeqQuantParam);
		QuantizeParam ifQuantParam = _quantize.GetIfQuantParam(dataType);
		short padValue2 = GetPadValue(padValue, ifQuantParam, dataType);
		Expr weights2 = LoadWeights(oldWeights, isPdp0dw: false, groups);
		bool flag = _conf.QuantType == DataTypes.Int16 || (_conf.WQuantType == DataTypes.Int16 && _conf.QuantType == DataTypes.Int8);
		Call call2 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEConv2DTranspose(DataTypes.Float16, input2, weights2, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, _qWeightsQint8), weightsBias, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, _qWeightsBiasQint8), act2, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, _actParamQint8.ToAct0Data()), ifQuantParam.ZeroPoint, 0, 0, Tensor.FromScalar(ifDeqQuantParam), padding, stride, dilation, groups, flag, padValue2, _actParam, _actParamQint8, outputPadding, outputshape));
		call2.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
		return Nncase.IR.F.Math.RangeOfMarker(call2, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
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
		if (input is Call call && call.Target is Dequantize && call.Arguments[0].CheckedDataType == realType)
		{
			QuantParam quantParam = ((TensorConst)call.Arguments[Dequantize.DequantParam.Index]).Value.ToScalar<QuantParam>();
			ifDeqQuantParam.ZeroPoint = quantParam.ZeroPoint;
			ifDeqQuantParam.Scale = quantParam.Scale;
		}
		return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)realType, Nncase.IR.F.Math.Quantize(inputMarker, new QuantParam(ifDeqQuantParam.ZeroPoint, ifDeqQuantParam.Scale), realType));
	}

	private Expr LoadWeights(Tensor oldWeights, bool isPdp0dw = false, int groupsConvTranspose = 1)
	{
		if (_conf.WQuantType != DataTypes.Int16 || isPdp0dw)
		{
			QuantT[] array = _quantize.GetQuantWeights(isMatmul: false, groupsConvTranspose);
			_qWeightsQint8 = _quantize.GetQuantWeightsU8(isMatmul: false, groupsConvTranspose).ToArray();
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
		FakeConv2DTranspose fakeConvTranspose = (FakeConv2DTranspose)__result["fakeConvTranspose"];
		Call call = (Call)__result["call"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr input = (Expr)__result["input"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		Tensor value = ((TensorConst)__result["weights"]).Value;
		Tensor<float> weightsRange = ((TensorConst)__result["weightsRange"]).Value.Cast<float>();
		TensorConst act = (TensorConst)__result["act"];
		Expr outputshape = (Expr)__result["outputshape"];
		Expr padding = (Expr)__result["padding"];
		Expr outputPadding = (Expr)__result["outputPadding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Expr outputMarker = (Expr)__result["outputMarker"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		Tensor<float> padValue = ((TensorConst)__result["padValue"]).Value.Cast<float>();
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeConvTranspose, call, inputMarker, input, inputRange, weightsMarker, value, weightsRange, act, outputshape, padding, outputPadding, stride, dilation, groups, outputMarker, outputRange, padValue);
	}
}
