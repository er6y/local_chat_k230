using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nncase.Evaluator;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.RNN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToFakeLSTM : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsWrappedLSTM(Nncase.PatternMatch.F.RNN.IsLSTM("lstm", "call", (LSTM _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("xMarker", Nncase.PatternMatch.Utility.IsWildcard("x"), Nncase.PatternMatch.Utility.IsConst("xRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsRangeOfMarker("wMarker", Nncase.PatternMatch.Utility.IsTensorConst("w"), Nncase.PatternMatch.Utility.IsConst("wRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsRangeOfMarker("rMarker", Nncase.PatternMatch.Utility.IsTensorConst("r"), Nncase.PatternMatch.Utility.IsConst("rRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("b"), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsRangeOfMarker("initHMarker", Nncase.PatternMatch.Utility.IsWildcard("initH"), Nncase.PatternMatch.Utility.IsConst("initHRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsRangeOfMarker("initCMarker", Nncase.PatternMatch.Utility.IsWildcard("initC"), Nncase.PatternMatch.Utility.IsConst("initCRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst("outputSize")), (Pattern t, int i) => Nncase.PatternMatch.Utility.IsRangeOfMarker($"outputMarker_{i}", t, Nncase.PatternMatch.Utility.IsWildcard()));


	private Expr? GetReplace(LSTM lstm, Call call, Expr x, TensorConst w, TensorConst r, Tensor<float> b, Expr initH, Expr initC, int outputSize, Marker xMarker, Marker wMarker, Marker rMarker, Marker initHMarker, Marker initCMarker, Tensor<float> xRange, Tensor<float> wRange, Tensor<float> rRange, Tensor<float> initHRange, Tensor<float> initCRange, IMatchResult result)
	{
		IMatchResult result2 = result;
		int channel = b.Shape[0].FixedValue * b.Shape[1].FixedValue / 2;
		ActParam2 actParam = new ActParam2(channel, new QuantParam(0, 1f));
		ActParam2 actParam2 = new ActParam2(channel, new QuantParam(0, 1f));
		int fixedValue = ((TensorType)((TupleType)call.CheckedType)[0]).Shape[1].FixedValue;
		int num = K230Kernels.ComputeSize(b.Shape) / 2 / fixedValue;
		List<float> list = new List<float>();
		for (int j = 0; j < num; j++)
		{
			list.Add(b.ToArray()[j]);
		}
		if (lstm.Direction == LSTMDirection.Bidirectional)
		{
			for (int k = num * 2; k < num * 3; k++)
			{
				list.Add(b.ToArray()[k]);
			}
		}
		float[] array = new float[list.Count];
		Array.Copy(list.ToArray(), 0, array, 0, array.Length);
		List<float> list2 = new List<float>();
		for (int l = num; l < num * 2; l++)
		{
			list2.Add(b.ToArray()[l]);
		}
		if (lstm.Direction == LSTMDirection.Bidirectional)
		{
			for (int m = num * 3; m < num * 4; m++)
			{
				list2.Add(b.ToArray()[m]);
			}
		}
		float[] array2 = new float[list2.Count];
		Array.Copy(list2.ToArray(), 0, array2, 0, array2.Length);
		for (int n = 0; n < array.Length; n++)
		{
			actParam.Bs[0, n] = array[n];
			actParam.Bs[1, n] = array[n];
		}
		for (int num2 = 0; num2 < array2.Length; num2++)
		{
			actParam2.Bs[0, num2] = array2[num2];
			actParam2.Bs[1, num2] = array2[num2];
		}
		ActParam16 actParam3 = new ActParam16(1);
		ActParam16 actParam4 = new ActParam16(1);
		ActFun actFun = new ActFun();
		actFun.SplitPoint0 = -8f;
		actFun.SplitPoint14 = 8f;
		actFun.SplitPointCenter = 0f;
		actFun.CenterPoint = 7;
		actFun.MinParam = new List<float> { 0f, 0f };
		actFun.MaxParam = new List<float> { 0f, 1f };
		actFun.Func = (float i) => 1f / (MathF.Exp(0f - i) + 1f);
		ActFun actFun2 = new ActFun();
		actFun2.SplitPoint0 = -4f;
		actFun2.SplitPoint14 = 4f;
		actFun2.SplitPointCenter = 0f;
		actFun2.CenterPoint = 7;
		actFun2.MinParam = new List<float> { 0f, -1f };
		actFun2.MaxParam = new List<float> { 0f, 1f };
		actFun2.Func = (float i) => MathF.Tanh(i);
		SetSegFittingParamSigmoid(actParam3, actFun);
		SetSegFittingParamTanh(actParam4, actFun2);
		int[] sourceArray = x.CheckedShape.ToValueArray();
		int[] array3 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(sourceArray, 0, array3, 1, 3);
		Marker input = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(x, array3), xRange).With(null, null, null, adaQuantInfo: xMarker.AdaQuantInfo, mixQuantInfo: xMarker.MixQuantInfo);
		int[] array4 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(w.CheckedShape.ToValueArray(), 0, array4, 1, 3);
		Marker wXc = Nncase.IR.F.Math.RangeOfMarker(Tensor.FromBytes(w.CheckedDataType, w.Value.BytesBuffer.ToArray(), array4), wRange).With(null, null, null, adaQuantInfo: wMarker.AdaQuantInfo, mixQuantInfo: wMarker.MixQuantInfo);
		int[] array5 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(r.CheckedShape.ToValueArray(), 0, array5, 1, 3);
		Marker wRc = Nncase.IR.F.Math.RangeOfMarker(Tensor.FromBytes(r.CheckedDataType, r.Value.BytesBuffer.ToArray(), array5), rRange).With(null, null, null, adaQuantInfo: rMarker.AdaQuantInfo, mixQuantInfo: rMarker.MixQuantInfo);
		int[] array6 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(initH.CheckedShape.ToValueArray(), 0, array6, 1, 3);
		Marker marker;
		if (initH is TensorConst tensorConst)
		{
			Tensor value = tensorConst.Value;
			if (value != null)
			{
				marker = Nncase.IR.F.Math.RangeOfMarker(Tensor.FromBytes(value.ElementType, value.BytesBuffer.ToArray(), array6), initHRange).With(null, null, null, adaQuantInfo: initHMarker.AdaQuantInfo, mixQuantInfo: initHMarker.MixQuantInfo);
				goto IL_05b5;
			}
		}
		marker = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(initH, array6), initHRange).With(null, null, null, adaQuantInfo: initHMarker.AdaQuantInfo, mixQuantInfo: initHMarker.MixQuantInfo);
		goto IL_05b5;
		IL_05b5:
		Expr initialH = marker;
		int[] array7 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(initC.CheckedShape.ToValueArray(), 0, array7, 1, 3);
		if (initC is TensorConst tensorConst2)
		{
			Tensor value2 = tensorConst2.Value;
			if (value2 != null)
			{
				marker = Nncase.IR.F.Math.RangeOfMarker(Tensor.FromBytes(value2.ElementType, value2.BytesBuffer.ToArray(), array7), initCRange).With(null, null, null, adaQuantInfo: initCMarker.AdaQuantInfo, mixQuantInfo: initCMarker.MixQuantInfo);
				goto IL_0689;
			}
		}
		marker = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(initC, array7), initCRange).With(null, null, null, adaQuantInfo: initCMarker.AdaQuantInfo, mixQuantInfo: initCMarker.MixQuantInfo);
		goto IL_0689;
		IL_0689:
		Expr initialC = marker;
		Memory<float> buffer = actParam.GetAct0Data;
		int[] obj = new int[4] { 1, 1, 0, 7 };
		obj[2] = fixedValue * w.CheckedShape[1].FixedValue;
		Expr actXc = new Tensor<float>(buffer, obj);
		Memory<float> buffer2 = actParam2.GetAct0Data;
		int[] obj2 = new int[4] { 1, 1, 0, 7 };
		obj2[2] = fixedValue * r.CheckedShape[1].FixedValue;
		Call call2 = Nncase.IR.K230.F.Tensors.FakeLSTM(input, wXc, actXc, wRc, new Tensor<float>(buffer2, obj2), initialH, initialC, new Tensor<float>(actParam3.GetAct1Data, new int[]{1,1,1,1}), new Tensor<float>(actParam4.GetAct1Data, new int[] { 1, 1, 1, 1 }), false, lstm.Direction, outputSize, actParam, actParam2);
		Shape[] oldShapes2 = ((TupleType)call.CheckedType).Select((IRType s) => ((TensorType)s).Shape).ToArray();
		return WrapOutput(call2, outputSize, oldShapes2);
		Nncase.IR.Tuple WrapOutput(Call call1, int outputSize1, Shape[] oldShapes)
		{
			Call call3 = call1;
			Shape[] oldShapes3 = oldShapes;
			Expr[] fields = (from i in Enumerable.Range(0, outputSize1)
				select Nncase.IR.F.Tensors.GetItem(call3, i)).ToArray().Select((Call item, int i) => ((Marker)result2[$"outputMarker_{i}"]).With(null, Nncase.IR.F.Tensors.Reshape(item, oldShapes3[i]), null, null, null)).ToArray();
			return new Nncase.IR.Tuple(fields);
		}
	}

	private void SetSegFittingParamSigmoid(ActParam16 actParam, ActFun f)
	{
		float[,] xs = actParam.Xs;
		float[] array = new float[15]
		{
			-7f, -4.5f, -3.5f, -2.7f, -2.1f, -1.6f, -1f, 0f, 1f, 1.6f,
			2.1f, 2.7f, 3.5f, 4.5f, 7f
		};
		for (int i = 0; i < 15; i++)
		{
			xs[i, 0] = array[i];
		}
		double[] array2 = new double[32]
		{
			0.0005523135475095087, 0.004717946782565874, 0.003582545941984816, 0.024643784893781717, 0.017628076952972527, 0.08920121475552378, 0.04080084671519202, 0.17057512199171598, 0.07504241042393778, 0.2641958959068108,
			0.11550039574401527, 0.35039917518496155, 0.16589656476120918, 0.4312276071962273, 0.23123362875892262, 0.4955178069668943, 0.23398496370676491, 0.5031840614393268, 0.17066333504274322, 0.5625762717242175,
			0.1197647477013204, 0.6417049229131112, 0.07822321089963047, 0.7281582013897308, 0.04270052410967129, 0.8235195920244173, 0.01849619693381177, 0.9073131477622514, 0.0037644096557241102, 0.9742926548134362,
			0.000567715948649905, 0.9951637114353361
		};
		for (int j = 0; j < 16; j++)
		{
			actParam.Ks[j, 0] = (float)array2[j * 2];
			actParam.Bs[j, 0] = (float)array2[j * 2 + 1];
		}
		actParam.SetFusedClamp(new ValueRange<float>(0f, 1f));
	}

	private void SetSegFittingParamTanh(ActParam16 actParam, ActFun f)
	{
		float[,] xs = actParam.Xs;
		float[] array = new float[15]
		{
			-3.1f, -2.28f, -1.76f, -1.438f, -1.122f, -0.82f, -0.51f, 0f, 0.47f, 0.81f,
			1.125f, 1.432f, 1.77f, 2.28f, 3.1f
		};
		for (int i = 0; i < 15; i++)
		{
			xs[i, 0] = array[i];
		}
		double[] array2 = new double[32]
		{
			0.0009063486449397695, -0.995190713545316, 0.019181480050426303, -0.9381162870143267, 0.06880845134681457, -0.8250607603958839, 0.1518869708683811, -0.6772576184059398, 0.27007052179269864, -0.5091301547712312,
			0.4374196151711459, -0.3220241963133208, 0.6540450071930701, -0.14398516081895907, 0.9192436825013623, -0.010453854050918476, 0.9412592709759997, 0.005250815043192469, 0.6730960939548211, 0.13033188197303736,
			0.43741961517114625, 0.32202419631332024, 0.2700705217926983, 0.5091301547712318, 0.151886970868381, 0.6772576184059396, 0.0688084513468159, 0.8250607603958814, 0.01918148005042708, 0.9381162870143249,
			0.0009063486449409908, 0.9951907135453116
		};
		for (int j = 0; j < 16; j++)
		{
			actParam.Ks[j, 0] = (float)array2[j * 2];
			actParam.Bs[j, 0] = (float)array2[j * 2 + 1];
		}
		actParam.SetFusedClamp(new ValueRange<float>(-1f, 1f));
	}

	private void SetSegFittingParam(ActParam16 actParam, ActFun f)
	{
		float[,] xs = actParam.Xs;
		int num = 0;
		xs[0, num] = f.SplitPoint0;
		xs[14, num] = f.SplitPoint14;
		int centerPoint = f.CenterPoint;
		xs[centerPoint, num] = f.SplitPointCenter;
		for (int i = 1; i < centerPoint; i++)
		{
			xs[i, num] = (xs[centerPoint, num] - xs[0, num]) / (float)centerPoint * (float)i + xs[0, num];
		}
		for (int j = centerPoint + 1; j < 15; j++)
		{
			xs[j, num] = (xs[14, num] - xs[centerPoint, num]) / (float)(14 - centerPoint) * (float)(j - centerPoint) + xs[centerPoint, num];
		}
		actParam.Ks[0, num] = f.MinParam[0];
		actParam.Bs[0, num] = f.MinParam[1];
		actParam.Ks[15, num] = f.MaxParam[0];
		actParam.Bs[15, num] = f.MaxParam[1];
		for (int k = 1; k < 15; k++)
		{
			float num2 = (f.Func(xs[k, num]) - f.Func(xs[k - 1, num])) / (xs[k, num] - xs[k - 1, num]);
			float num3 = f.Func(xs[k, num]) - num2 * xs[k, num];
			actParam.Ks[k, num] = num2;
			actParam.Bs[k, num] = num3;
		}
		actParam.SetFusedClamp(ValueRange<float>.Full);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		LSTM lstm = (LSTM)__result["lstm"];
		Call call = (Call)__result["call"];
		Expr x = (Expr)__result["x"];
		TensorConst w = (TensorConst)__result["w"];
		TensorConst r = (TensorConst)__result["r"];
		Tensor<float> b = ((TensorConst)__result["b"]).Value.Cast<float>();
		Expr initH = (Expr)__result["initH"];
		Expr initC = (Expr)__result["initC"];
		int outputSize = ((TensorConst)__result["outputSize"]).Value.ToScalar<int>();
		Marker xMarker = (Marker)__result["xMarker"];
		Marker wMarker = (Marker)__result["wMarker"];
		Marker rMarker = (Marker)__result["rMarker"];
		Marker initHMarker = (Marker)__result["initHMarker"];
		Marker initCMarker = (Marker)__result["initCMarker"];
		Tensor<float> xRange = ((TensorConst)__result["xRange"]).Value.Cast<float>();
		Tensor<float> wRange = ((TensorConst)__result["wRange"]).Value.Cast<float>();
		Tensor<float> rRange = ((TensorConst)__result["rRange"]).Value.Cast<float>();
		Tensor<float> initHRange = ((TensorConst)__result["initHRange"]).Value.Cast<float>();
		Tensor<float> initCRange = ((TensorConst)__result["initCRange"]).Value.Cast<float>();
		return GetReplace(lstm, call, x, w, r, b, initH, initC, outputSize, xMarker, wMarker, rMarker, initHMarker, initCMarker, xRange, wRange, rRange, initHRange, initCRange, __result);
	}
}
