using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Nncase.CodeGen.K230;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Quantization;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToDynamicGNNEMatMul : RewriteRule<Pattern>
{
	private Const? _instructions;

	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeDynamicGNNEMatMul("fakeMatmul", "call", (FakeDynamicGNNEMatMul _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputAMarker", Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsConst("inputARange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("inputBMarker", Nncase.PatternMatch.Utility.IsWildcard("inputB"), Nncase.PatternMatch.Utility.IsConst("inputBRange")), Nncase.PatternMatch.Utility.IsConst("act0"));


	private static Call LoadQuantInA(Expr prevInput, ValueRange<float> inARange, DataType quanType)
	{
		DeQuantizeParam inADeqQuantParam = NonConstQuantManager.GetInADeqQuantParam(quanType, inARange);
		int num = 4 - prevInput.CheckedShape.Rank;
		if (num > 0)
		{
			prevInput = Nncase.IR.F.Tensors.Unsqueeze(prevInput, Enumerable.Range(0, num).ToArray());
		}
		return Nncase.IR.F.Math.Quantize(prevInput, Tensor.FromScalar(new QuantParam(inADeqQuantParam.ZeroPoint, inADeqQuantParam.Scale)), quanType);
	}

	private static Call LoadQuantInB(Expr prevInput, ValueRange<float> inBRange, DataType quanType)
	{
		DeQuantizeParam inBDeqQuantParam = NonConstQuantManager.GetInBDeqQuantParam(quanType, inBRange);
		int num = 4 - prevInput.CheckedShape.Rank;
		if (num > 0)
		{
			prevInput = Nncase.IR.F.Tensors.Unsqueeze(prevInput, Enumerable.Range(0, num).ToArray());
		}
		return Nncase.IR.F.Math.Quantize(prevInput, Tensor.FromScalar(new QuantParam(inBDeqQuantParam.ZeroPoint, inBDeqQuantParam.Scale)), quanType);
	}

	private Expr? GetReplace(FakeDynamicGNNEMatMul fakeMatmul, Call call, Expr inputAMarker, Expr inputA, Tensor<float> inputARange, Expr inputBMarker, Expr inputB, Tensor<float> inputBRange, Tensor<float> act0, RunPassContext options)
	{
		QuantizeOptions quantizeOptions = base.CompileSession.CompileOptions.QuantizeOptions;
		ActivationParameter<float> actParam = new ActivationParameter<float>(act0);
		_ = quantizeOptions.BindQuantMethod;
		MixQuantInfo mixQuantInfo = ((Marker)inputAMarker).MixQuantInfo;
		MixQuantInfo mixQuantInfo2 = ((Marker)inputBMarker).MixQuantInfo;
		DataType dataType = ((mixQuantInfo?.MarkerQuantType == null) ? quantizeOptions.QuantType : mixQuantInfo.MarkerQuantType);
		DataType dataType2 = ((mixQuantInfo2?.MarkerQuantType == null) ? quantizeOptions.QuantType : mixQuantInfo2.MarkerQuantType);
		Call call2 = LoadQuantInA(inputA, inputARange.AsValueRange(), dataType);
		Call call3 = LoadQuantInB(inputB, inputBRange.AsValueRange(), dataType2);
		Expr expr = LoadAct(actParam, inputARange.AsValueRange(), inputBRange.AsValueRange(), dataType, dataType2);
		Shape checkedShape = inputA.CheckedShape;
		Expr expr2;
		if (checkedShape[checkedShape.Count - 2].IsFixed)
		{
			Shape checkedShape2 = inputA.CheckedShape;
			byte[] array = LoadInABias(checkedShape2[checkedShape2.Count - 2].FixedValue, inputARange.AsValueRange(), dataType);
			int[] obj = new int[4] { 1, 1, 1, 0 };
			Shape checkedShape3 = inputA.CheckedShape;
			obj[3] = checkedShape3[checkedShape3.Count - 2].FixedValue;
			expr2 = new TensorConst(Tensor.From(array, obj));
		}
		else
		{
			byte[] array2 = LoadInABias(1, inputARange.AsValueRange(), dataType);
			Call call4 = Nncase.IR.F.Tensors.Slice(Nncase.IR.F.Tensors.ShapeOf(inputA), new int[1] { -2 }, new int[1] { -1 }, 1);
			expr2 = Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Tensors.Tile(new TensorConst(Tensor.From(array2, new int[]{1,1,1,0})), call4), Nncase.IR.F.Tensors.Concat(new Nncase.IR.Tuple(new long[3] { 1L, 1L, 1L }, call4), 0));
		}
		int num = checked((byte)NonConstQuantManager.GetInBQuantParam(dataType2, inputBRange.AsValueRange()).ZeroPoint);
		Const instructions = GetInstructions(options);
		Call call5 = Nncase.IR.F.NN.CustomCall(new DynamicGNNEMatMul((PrimType)call.CheckedDataType), instructions, call2, call3, expr2, num, expr, 0, Convert.ToInt32(fakeMatmul.DynamicChannel));
		int num2 = 4 - call.CheckedShape.Rank;
		if (num2 > 0)
		{
			call5 = Nncase.IR.F.Tensors.Squeeze(call5, Enumerable.Range(0, num2).ToArray());
		}
		return call5;
	}

	private Const GetInstructions(RunPassContext passOptions)
	{
		if ((object)_instructions != null)
		{
			return _instructions;
		}
		CSourceGModelBuilder cSourceGModelBuilder = new CSourceGModelBuilder(Path.Join(Path.GetDirectoryName(GetType().Assembly.Location), "Functional", "dynamic_gnne_matmul.c"));
		cSourceGModelBuilder.Serialize();
		byte[] array = File.ReadAllBytes(cSourceGModelBuilder.GModelBuilder.BinFilePath);
		if (DumpScope.Current.IsEnabled(DumpFlags.Rewrite))
		{
			cSourceGModelBuilder.Dump("ToDynamicGNNEMatMul", DumpScope.Current.Directory);
		}
		_instructions = Const.FromTensor(Tensor.From(array));
		return _instructions;
	}

	private byte[] LoadInABias(int channels, ValueRange<float> inARange, DataType quanType)
	{
		DeQuantizeParam inADeqQuantParam = NonConstQuantManager.GetInADeqQuantParam(quanType, inARange);
		byte[] array = new byte[channels];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = checked((byte)inADeqQuantParam.ZeroPoint);
		}
		return array;
	}

	private Tensor<Half> LoadAct(ActivationParameter<float> actParam, ValueRange<float> inARange, ValueRange<float> inBRange, DataType quanTypeA, DataType quanTypeB)
	{
		DeQuantizeParam inADeqQuantParam = NonConstQuantManager.GetInADeqQuantParam(quanTypeA, inARange);
		DeQuantizeParam inBDeqQuantParam = NonConstQuantManager.GetInBDeqQuantParam(quanTypeB, inBRange);
		actParam.FusedScale(inADeqQuantParam.Scale);
		actParam.FusedScale(inBDeqQuantParam.Scale);
		return actParam.ToAct0Data<Half>();
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeDynamicGNNEMatMul fakeMatmul = (FakeDynamicGNNEMatMul)__result["fakeMatmul"];
		Call call = (Call)__result["call"];
		Expr inputAMarker = (Expr)__result["inputAMarker"];
		Expr inputA = (Expr)__result["inputA"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Expr inputBMarker = (Expr)__result["inputBMarker"];
		Expr inputB = (Expr)__result["inputB"];
		Tensor<float> inputBRange = ((TensorConst)__result["inputBRange"]).Value.Cast<float>();
		Tensor<float> act = ((TensorConst)__result["act0"]).Value.Cast<float>();
		return GetReplace(fakeMatmul, call, inputAMarker, inputA, inputARange, inputBMarker, inputB, inputBRange, act, __context);
	}
}
