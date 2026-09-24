using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nncase.Evaluator;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToGNNELSTM : IRewriteRule
{
	private QuantizeManager? _quantizeIf;

	private QuantizeManager? _quantizeH;

	private QuantizeManager? _quantizeWXc;

	private QuantizeManager? _quantizeWRc;

	private QuantizeManager? _quantizeOf;

	private ConvQuantConf? _conf;

	public IPattern Pattern { get; } = Nncase.PatternMatch.Utility.IsWrappedLSTM(Nncase.PatternMatch.F.K230.IsFakeLSTM("fakeLstm", "call", (FakeLSTM _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("wXcMarker", Nncase.PatternMatch.Utility.IsTensorConst("wXc"), Nncase.PatternMatch.Utility.IsConst("wXcRange")), Nncase.PatternMatch.Utility.IsTensorConst("actXc"), Nncase.PatternMatch.Utility.IsRangeOfMarker("wRcMarker", Nncase.PatternMatch.Utility.IsTensorConst("wRc"), Nncase.PatternMatch.Utility.IsConst("wRcRange")), Nncase.PatternMatch.Utility.IsTensorConst("actRc"), Nncase.PatternMatch.Utility.IsRangeOfMarker("initialHMarker", Nncase.PatternMatch.Utility.IsWildcard("initialH"), Nncase.PatternMatch.Utility.IsConst("initialHRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("initialCMarker", Nncase.PatternMatch.Utility.IsWildcard("initialC"), Nncase.PatternMatch.Utility.IsConst("initialCRange")), Nncase.PatternMatch.Utility.IsTensorConst("segFittingParamFt"), Nncase.PatternMatch.Utility.IsTensorConst("segFittingParamGt"), Nncase.PatternMatch.Utility.IsTensorConst("hasStatic"), Nncase.PatternMatch.Utility.IsTensorConst("outputSize")), (Pattern t, int i) => Nncase.PatternMatch.Utility.IsRangeOfMarker($"outputMarker_{i}", Nncase.PatternMatch.Utility.IsAlt(Nncase.PatternMatch.F.Tensors.IsReshape(t, Nncase.PatternMatch.Utility.IsTensorConst($"shapes_{i}")), t), Nncase.PatternMatch.Utility.IsTensorConst($"outputRange_{i}")));


	private void LowerInit(FakeLSTM fakeLstm, Expr inputMarker, Tensor<float> inputRange, Expr wXcMarker, Expr wRcMarker, Tensor<float> wXcRange, Tensor<float> wRcRange, Expr initialHMarker, Tensor<float> initialHRange, Expr initialCMarker, Tensor<float> initialCRange, Expr initialH, Expr initialC, Tensor wXc, Tensor wRc, Tensor<float> outputRange)
	{
		int numDirection = ((fakeLstm.Direction != LSTMDirection.Bidirectional) ? 1 : 2);
		Tensor<float> wRange = ReplaceWeightRangeToByChannel(wXc, numDirection);
		Tensor<float> tensor = ReplaceWeightRangeToByChannel(wRc, numDirection);
		CompileOptions compileOptions = new CompileOptions();
		_ = compileOptions.QuantizeOptions.BindQuantMethod;
		MixQuantInfo mixQuantInfo = ((Marker)inputMarker).MixQuantInfo;
		MixQuantInfo mixQuantInfo2 = ((Marker)wXcMarker).MixQuantInfo;
		DataType quantType = ((mixQuantInfo?.MarkerQuantType == null) ? compileOptions.QuantizeOptions.QuantType : mixQuantInfo.MarkerQuantType);
		DataType wQuantType = ((mixQuantInfo2?.MarkerQuantType == null) ? compileOptions.QuantizeOptions.WQuantType : mixQuantInfo2?.MarkerQuantType);
		_conf = new ConvQuantConf
		{
			QuantType = quantType,
			UseMseQuantW = false,
			WQuantType = wQuantType
		};
		_quantizeIf = new QuantizeManager(inputRange, wRange, outputRange, wXc, _conf, is_matmul: true);
		_quantizeH = new QuantizeManager(initialHRange, (initialH is TensorConst) ? new Tensor<float>(initialHRange.ToArray(), new int[]{1,2}) : tensor, outputRange, (initialH is TensorConst tensorConst) ? tensorConst.Value : wRc, _conf, is_matmul: false);
		_quantizeWXc = new QuantizeManager(inputRange, wRange, outputRange, wXc, _conf, is_matmul: true);
		_quantizeWRc = new QuantizeManager(initialHRange, tensor, outputRange, wRc, _conf, is_matmul: true);
		_quantizeOf = new QuantizeManager(inputRange, wRange, outputRange, wXc, _conf, is_matmul: true);
	}

	private Expr? GetReplace(FakeLSTM fakeLstm, Call call, Expr input, Expr inputMarker, Expr initialHMarker, Expr initialCMarker, Tensor<float> inputRange, Tensor wXc, Tensor<float> wXcRange, Tensor<float> wRcRange, Tensor wRc, Tensor<float> initialHRange, Tensor<float> initialCRange, Expr wRcMarker, Expr wXcMarker, Expr initialH, Expr initialC, Expr segFittingParamFt, Expr segFittingParamGt, Tensor<float> outputRange_0, Expr hasStatic, int outputSize, IMatchResult result)
	{
		LowerInit(fakeLstm, inputMarker, inputRange, wXcMarker, wRcMarker, wXcRange, wRcRange, initialHMarker, initialHRange, initialCMarker, initialCRange, initialH, initialC, wXc, wRc, outputRange_0);
		DeQuantizeParam ifDeqQuantParam = _quantizeIf.GetIfDeqQuantParam(_conf.QuantType);
		Call input2 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)_conf.QuantType, Nncase.IR.F.Math.Quantize(inputMarker, new QuantParam(ifDeqQuantParam.ZeroPoint, ifDeqQuantParam.Scale), (PrimType)_conf.QuantType));
		QuantizeParam quantizeParam;
		Call initialH2;
		if (initialH is TensorConst)
		{
			Tensor value = ((TensorConst)initialH).Value;
			quantizeParam = _quantizeH.GetWeightsQuantParam()[0];
			initialH2 = Nncase.IR.K230.F.Tensors.GNNELoad(input: (_conf.WQuantType == DataTypes.Int16) ? Tensor.From((from x in _quantizeH.GetQuantWeightsI16()
				select (x)).ToArray(), value.Shape) : ((!(_conf.WQuantType == DataTypes.Int8)) ? ((Tensor)Tensor.From((from x in _quantizeH.GetQuantWeights()
				select x.U8).ToArray(), value.Shape)) : ((Tensor)Tensor.From((from x in _quantizeH.GetQuantWeights()
				select x.I8).ToArray(), value.Shape))), destType: (PrimType)_conf.WQuantType);
		}
		else
		{
			DeQuantizeParam ifDeqQuantParam2 = _quantizeH.GetIfDeqQuantParam(_conf.QuantType);
			initialH2 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)_conf.QuantType, Nncase.IR.F.Math.Quantize(initialH, new QuantParam(ifDeqQuantParam2.ZeroPoint, ifDeqQuantParam2.Scale), (PrimType)_conf.QuantType));
			quantizeParam = new QuantizeParam(ifDeqQuantParam2.ZeroPoint, 1f / ifDeqQuantParam2.Scale);
		}
		Call initialC2;
		if (initialC is TensorConst)
		{
			Tensor value2 = ((TensorConst)initialC).Value;
			Half[] array = new Half[K230Kernels.ComputeSize(value2.Shape)];
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = value2.ToArray<Half>()[j];
			}
			Tensor<Half> tensor = Tensor.From(array, value2.Shape);
			initialC2 = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, tensor);
		}
		else
		{
			initialC2 = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, initialC);
		}
		Call wXc2 = Nncase.IR.K230.F.Tensors.GNNELoadW(input: (_conf.WQuantType == DataTypes.UInt8) ? Tensor.From((from x in _quantizeIf.GetQuantWeights(isMatmul: true)
			select x.U8).ToArray(), wXc.Shape) : ((!(_conf.WQuantType == DataTypes.Int8)) ? ((Tensor)Tensor.From((from x in _quantizeIf.GetQuantWeightsI16(isMatmul: true)
			select (x)).ToArray(), wXc.Shape)) : ((Tensor)Tensor.From((from x in _quantizeIf.GetQuantWeights(isMatmul: true)
			select x.I8).ToArray(), wXc.Shape))), destType: (PrimType)_conf.WQuantType);
		byte[] array2 = _quantizeIf.GetWeightsQuantBiasQint8();
		if (_conf.WQuantType == DataTypes.Int16)
		{
			array2 = Enumerable.Repeat((byte)0, array2.Length).ToArray();
		}
		byte[] array3 = array2.ToArray();
		int[] obj = new int[4] { 1, 1, 1, 0 };
		obj[3] = array2.Length;
		Tensor<byte> tensor2 = Tensor.From(array3, obj);
		Call wXcQarg = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor2);
		Call wRc2 = Nncase.IR.K230.F.Tensors.GNNELoadW(input: (_conf.WQuantType == DataTypes.UInt8) ? Tensor.From((from x in _quantizeWRc.GetQuantWeights(isMatmul: true)
			select x.U8).ToArray(), wRc.Shape) : ((!(_conf.WQuantType == DataTypes.Int8)) ? ((Tensor)Tensor.From((from x in _quantizeWRc.GetQuantWeightsI16(isMatmul: true)
			select (x)).ToArray(), wRc.Shape)) : ((Tensor)Tensor.From((from x in _quantizeWRc.GetQuantWeights(isMatmul: true)
			select x.I8).ToArray(), wRc.Shape))), destType: (PrimType)_conf.WQuantType);
		byte[] array4 = _quantizeWRc.GetWeightsQuantBiasQint8();
		if (_conf.WQuantType == DataTypes.Int16)
		{
			array4 = Enumerable.Repeat((byte)0, array4.Length).ToArray();
		}
		byte[] array5 = array4.ToArray();
		int[] obj2 = new int[4] { 1, 1, 1, 0 };
		obj2[3] = array4.Length;
		Tensor<byte> tensor3 = Tensor.From(array5, obj2);
		Call wRcQarg = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor3);
		ActParam2 actParam = new ActParam2(fakeLstm.ActParamXc);
		DeQuantizeParam ifDeqQuantParam3 = _quantizeWXc.GetIfDeqQuantParam(_conf.QuantType);
		float[] weightsDeqScale = _quantizeWXc.GetWeightsDeqScale();
		actParam.FusedScale(ifDeqQuantParam3.Scale);
		actParam.FusedChannelScale(weightsDeqScale);
		Tensor<Half> tensor4 = actParam.ToAct0Data();
		Call actXc = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, tensor4);
		ActParam2 actParam2 = new ActParam2(fakeLstm.ActParamRc);
		QuantizeParam quantizeParam2 = ((initialH is TensorConst) ? _quantizeH.GetWeightsQuantParam()[0] : _quantizeH.GetIfQuantParam(_conf.QuantType));
		float[] weightsDeqScale2 = _quantizeWRc.GetWeightsDeqScale();
		actParam2.FusedScale(1f / quantizeParam2.Scale);
		actParam2.FusedChannelScale(weightsDeqScale2);
		Call actRc = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam2.ToAct0Data());
		ActParam2 actParam3 = new ActParam2(fakeLstm.ActParamRc);
		DeQuantizeParam ofDeqQuantParam = _quantizeWRc.GetOfDeqQuantParam(_conf.QuantType);
		QuantizeParam ofQuantParam = _quantizeWRc.GetOfQuantParam(_conf.QuantType);
		float[] weightsDeqScale3 = _quantizeWRc.GetWeightsDeqScale();
		actParam3.FusedScale(ofDeqQuantParam.Scale);
		actParam3.FusedChannelScale(weightsDeqScale3);
		Call actRc2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam3.ToAct0Data());
		Call segFittingParamFt2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, ((TensorConst)segFittingParamFt).Value.Cast<Half>());
		Call segFittingParamGt2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, ((TensorConst)segFittingParamGt).Value.Cast<Half>());
		Shape shape = ((TensorType)((TupleType)call.CheckedType)[0]).Shape;
		ActParam2 actParam4 = new ActParam2(shape[shape.Count - 1].FixedValue);
		Call actBin = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam4.ToAct1Data());
		Shape shape2 = ((TensorType)((TupleType)call.CheckedType)[0]).Shape;
		ActParam2 actParam5 = new ActParam2(shape2[shape2.Count - 1].FixedValue);
		QuantizeParam ofQParams = _quantizeOf.GetOfQuantParam(_conf.QuantType);
		actParam5.SetFusedClamp(ValueRange<Half>.Full);
		actParam5.FusedQuantParam(new QuantParam(ofQParams.ZeroPoint, 1f / ofQParams.Scale));
		Call actBinQ = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam5.ToAct1Data());
		Call c2 = Nncase.IR.K230.F.Tensors.GNNELSTM((PrimType)_conf.QuantType, (PrimType)_conf.QuantType, input2, wXc2, actXc, wRc2, actRc, actRc2, initialH2, initialC2, segFittingParamFt2, segFittingParamGt2, wXcQarg, wRcQarg, actBin, actBinQ, actParam, actParam2, actParam3, ifDeqQuantParam.ZeroPoint, 0, quantizeParam.ZeroPoint, ofQuantParam.ZeroPoint, 0, 0, 0, 0, 0, actParam4, actParam5, fakeLstm.Direction, hasStatic, outputSize);
		int[][] array6 = new int[outputSize][];
		for (int k = 0; k < outputSize; k++)
		{
			try
			{
				TensorConst tensorConst = (TensorConst)result[$"shapes_{k}"];
				array6[k] = tensorConst.Value.ToArray<int>();
			}
			catch (KeyNotFoundException)
			{
				array6[k] = ((Marker)result[$"outputMarker_{k}"]).CheckedShape.ToValueArray();
			}
		}
		return WrapOutput(c2, outputSize, array6);
		Nncase.IR.Tuple WrapOutput(Call c, int outputsize, int[][] shapes)
		{
			Call c3 = c;
			int[][] shapes2 = shapes;
			Call[] source = (from i in Enumerable.Range(0, outputsize)
				select Nncase.IR.F.Tensors.GetItem(c3, i)).ToArray();
			Expr[] fields = source.Select((Call gi, int i) => (i == 2) ? Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, gi) : Nncase.IR.K230.F.Tensors.GNNEStore(_conf.QuantType, gi)).ToArray();
			Nncase.IR.Tuple stores = new Nncase.IR.Tuple(fields);
			return new Nncase.IR.Tuple(source.Select((Call _, int i) => Nncase.IR.F.Tensors.GetItem(stores, i)).Select((Func<Call, int, Expr>)((Call gi, int i) => (i == 2) ? Nncase.IR.F.Tensors.Reshape(gi, shapes2[2]) : Nncase.IR.F.Math.Dequantize(Nncase.IR.F.Tensors.Reshape(gi, shapes2[i]), new QuantParam(ofQParams.ZeroPoint, 1f / ofQParams.Scale), DataTypes.Float32))).ToArray());
		}
	}

	private Tensor<float> ReplaceWeightRangeToByChannel(Tensor weights, int numDirection)
	{
		float[] array = ((TensorConst)(Expr)weights).Value.ToArray<float>();
		Dimension dimension = weights.Shape[2];
		return new Tensor<float>(QuantUtility.GetWeightsRangesByChannel(array, numDirection * dimension.FixedValue).ToArray(), new int[2]
		{
			dimension.FixedValue * numDirection,
			2
		});
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeLSTM fakeLstm = (FakeLSTM)__result["fakeLstm"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr initialHMarker = (Expr)__result["initialHMarker"];
		Expr initialCMarker = (Expr)__result["initialCMarker"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Tensor value = ((TensorConst)__result["wXc"]).Value;
		Tensor<float> wXcRange = ((TensorConst)__result["wXcRange"]).Value.Cast<float>();
		Tensor<float> wRcRange = ((TensorConst)__result["wRcRange"]).Value.Cast<float>();
		Tensor value2 = ((TensorConst)__result["wRc"]).Value;
		Tensor<float> initialHRange = ((TensorConst)__result["initialHRange"]).Value.Cast<float>();
		Tensor<float> initialCRange = ((TensorConst)__result["initialCRange"]).Value.Cast<float>();
		Expr wRcMarker = (Expr)__result["wRcMarker"];
		Expr wXcMarker = (Expr)__result["wXcMarker"];
		Expr initialH = (Expr)__result["initialH"];
		Expr initialC = (Expr)__result["initialC"];
		Expr segFittingParamFt = (Expr)__result["segFittingParamFt"];
		Expr segFittingParamGt = (Expr)__result["segFittingParamGt"];
		Tensor<float> outputRange_ = ((TensorConst)__result["outputRange_0"]).Value.Cast<float>();
		Expr hasStatic = (Expr)__result["hasStatic"];
		int outputSize = ((TensorConst)__result["outputSize"]).Value.ToScalar<int>();
		return GetReplace(fakeLstm, call, input, inputMarker, initialHMarker, initialCMarker, inputRange, value, wXcRange, wRcRange, value2, initialHRange, initialCRange, wRcMarker, wXcMarker, initialH, initialC, segFittingParamFt, segFittingParamGt, outputRange_, hasStatic, outputSize, __result);
	}
}
