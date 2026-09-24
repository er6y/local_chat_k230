using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.TIR.Instructions;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToGNNEPdpReduce : GNNEQuantRule, IRewriteRule
{
	private static int _mAXPDP0KERNELSIZE = 9;

	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.K230.IsFakePdp("r", "call", (FakePdp _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("input_marker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("inputRange")), Nncase.PatternMatch.Utility.IsTensorConst("padValue"), Nncase.PatternMatch.Utility.IsTensorConst("filter"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad")), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(FakePdp r, Call call, Marker input_marker, Tensor<float> padValue, Expr filter, Expr stride, Expr padding, Expr countIncludePad, Expr outputMarker, Expr outputRange, RunPassContext context)
	{
		IExprUserAnalysisResult analysis = context.GetAnalysis<IExprUserAnalysisResult>();
		bool flag = false;
		Call call2 = ((!(call[FakePdp.Input] is Marker { Target: Call target }) || !(target.Target is FakeConv2D)) ? null : target);
		Call call3 = call2;
		if ((object)call3 != null)
		{
			int[] array = call3[FakeConv2D.Input].CheckedShape.ToValueArray();
			int[] array2 = call3.CheckedShape.ToValueArray();
			int num = ((TensorConst)call3[FakeConv2D.Groups]).Value.ToScalar<int>();
			bool flag2 = array[1] == array2[1] && array2[1] == num && num != 1;
			int[] array3 = ((TensorConst)call[FakePdp.Filter]).Value.ToArray<int>();
			if (analysis[input_marker].Count() == 1 && !flag2 && HasValidPdp0Kernel(array3[0], array3[1]) && (r.ReduceOp == ReduceOp.Max || r.ReduceOp == ReduceOp.Min))
			{
				flag = true;
			}
		}
		CompileOptions compileOptions = base.CompileSession.CompileOptions;
		MixQuantInfo mixQuantInfo = input_marker.MixQuantInfo;
		DataType dataType = ((compileOptions.QuantizeOptions.QuantScheme == string.Empty) ? compileOptions.QuantizeOptions.QuantType : mixQuantInfo.MarkerQuantType);
		int bits = base.QuantBits;
		if (dataType == DataTypes.Int16)
		{
			dataType = DataTypes.UInt8;
			bits = 8;
		}
		QuantMode quantMode = ((!(dataType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		QuantParam quantParam = QuantUtility.GetQuantParam(base.InputRange, bits, quantMode);
		DeQuantizeParam[] array4 = GNNETypePatternUtility.GNNEGetDeqParams(1, quantParam.Scale, quantParam.ZeroPoint);
		QuantParam quantParam2 = QuantUtility.GetQuantParam(base.OutputRange, bits, quantMode);
		QuantizeParam[] array5 = GNNETypePatternUtility.GNNEGetQuantParams(1, quantParam2.Scale, quantParam2.ZeroPoint);
		if (flag)
		{
			QuantParam quantParam3 = QuantUtility.GetQuantParam(base.InputRange, 8, QuantMode.UnsignedMode);
			PrimType targetType = ((dataType == DataTypes.Int8) ? DataTypes.Int8 : DataTypes.UInt8);
			if (dataType == DataTypes.Int16)
			{
				quantParam = quantParam3;
			}
			QuantParam quantizeParam = quantParam;
			float num2 = ((dataType == DataTypes.UInt8) ? GetPadValueByte(r.ReduceOp) : ((dataType == DataTypes.Int8) ? GetPadValueSbyte(r.ReduceOp) : GetPadValueInt16(r.ReduceOp)));
			if (GetReplaceHelper.ToScalar<bool>(countIncludePad) || r.ReduceOp != 0)
			{
				int value = (int)System.Math.Round(padValue.GetValue(0) / quantizeParam.Scale + (float)quantizeParam.ZeroPoint);
				num2 = ((base.QuantType == DataTypes.UInt8) ? ((byte)System.Math.Clamp(value, 0, 255)) : ((base.QuantType == DataTypes.Int8) ? ((sbyte)System.Math.Clamp(value, -127, 127)) : ((short)System.Math.Clamp(value, -32767, 32767))));
			}
			int fixedValue = call.CheckedShape[1].FixedValue;
			quantizeParam.Scale = 1f / quantizeParam.Scale;
			quantizeParam.ZeroPoint = 0;
			ActParam2 actParam = new ActParam2(fixedValue, quantizeParam, isDeq: true);
			Expr act = LoadAct0(actParam);
			Call call4 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEPdp0Reduce(ToPUPdpOp(r.ReduceOp), DataTypes.Float16, actParam, Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, Nncase.IR.F.Math.Quantize(input_marker, new QuantParam(quantParam.ZeroPoint, quantParam.Scale), targetType)), filter, stride, padding, array4, (int)num2, 0, countIncludePad, act));
			call4.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
			return Nncase.IR.F.Math.RangeOfMarker(call4, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
		}
		float num3 = ((dataType == DataTypes.UInt8) ? GetPadValueByte(r.ReduceOp) : ((dataType == DataTypes.Int8) ? GetPadValueSbyte(r.ReduceOp) : GetPadValueInt16(r.ReduceOp)));
		if (GetReplaceHelper.ToScalar<bool>(countIncludePad))
		{
			int value2 = (int)System.Math.Round(padValue.GetValue(0) / quantParam.Scale + (float)quantParam.ZeroPoint);
			num3 = ((base.QuantType == DataTypes.UInt8) ? ((byte)System.Math.Clamp(value2, 0, 255)) : ((base.QuantType == DataTypes.Int8) ? ((sbyte)System.Math.Clamp(value2, -127, 127)) : ((short)System.Math.Clamp(value2, -32767, 32767))));
		}
		Call call5 = Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEPdp1(DataTypes.Float16, ToMFUPdpOp(r.ReduceOp), Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, Nncase.IR.F.Math.Quantize(input_marker, quantParam, dataType)), filter, stride, padding, array5, array4, num3, 0, countIncludePad));
		call5.CheckedType = new TensorType(DataTypes.Float32, call.CheckedShape);
		return Nncase.IR.F.Math.RangeOfMarker(call5, outputRange).With(null, null, null, adaQuantInfo: ((Marker)outputMarker).AdaQuantInfo, mixQuantInfo: ((Marker)outputMarker).MixQuantInfo);
	}

	private bool HasValidPdp0Kernel(int filterH, int filterW)
	{
		return filterH * filterW <= _mAXPDP0KERNELSIZE;
	}

	private MFU_PDP_OP ToMFUPdpOp(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Mean => MFU_PDP_OP.AVERAGE, 
			ReduceOp.Min => MFU_PDP_OP.MIN, 
			ReduceOp.Max => MFU_PDP_OP.MAX, 
			ReduceOp.Sum => MFU_PDP_OP.SUM, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	private PU_PDP0_MODE ToPUPdpOp(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Mean => PU_PDP0_MODE.average, 
			ReduceOp.Min => PU_PDP0_MODE.min, 
			ReduceOp.Max => PU_PDP0_MODE.max, 
			ReduceOp.Sum => PU_PDP0_MODE.sum, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	private float GetPadValueSbyte(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Min => 127f, 
			ReduceOp.Max => -128f, 
			ReduceOp.Mean => float.NaN, 
			ReduceOp.Sum => float.NaN, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	private float GetPadValueByte(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Min => 255f, 
			ReduceOp.Max => 0f, 
			ReduceOp.Mean => float.NaN, 
			ReduceOp.Sum => float.NaN, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	private float GetPadValueInt16(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Min => float.MaxValue, 
			ReduceOp.Max => float.MinValue, 
			ReduceOp.Mean => float.NaN, 
			ReduceOp.Sum => float.NaN, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	private Expr LoadAct0(ActParam2 actParam2)
	{
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam2.ToAct0Data());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakePdp r = (FakePdp)__result["r"];
		Call call = (Call)__result["call"];
		Marker input_marker = (Marker)__result["input_marker"];
		Tensor<float> padValue = ((TensorConst)__result["padValue"]).Value.Cast<float>();
		Expr filter = (Expr)__result["filter"];
		Expr stride = (Expr)__result["stride"];
		Expr padding = (Expr)__result["padding"];
		Expr countIncludePad = (Expr)__result["countIncludePad"];
		Expr outputMarker = (Expr)__result["outputMarker"];
		Expr outputRange = (Expr)__result["outputRange"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(r, call, input_marker, padValue, filter, stride, padding, countIncludePad, outputMarker, outputRange, __context);
	}
}
