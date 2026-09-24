#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public sealed class FakeMatMulEvaluator : IEvaluator<FakeMatMul>, IEvaluator, ITypeInferencer<FakeMatMul>, ITypeInferencer, ICostEvaluator<FakeMatMul>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeMatMul target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeMatMul.InputA);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, FakeMatMul.InputB);
		TensorType argumentType3 = context.GetArgumentType<TensorType>(target, FakeMatMul.Act);
		TensorType returnType = context.GetReturnType<TensorType>();
		Shape shape = argumentType.Shape;
		int num;
		if (!shape[shape.Count - 1].IsFixed)
		{
			num = 1;
		}
		else
		{
			Shape shape2 = argumentType.Shape;
			num = shape2[shape2.Count - 1].FixedValue;
		}
		uint num2 = (uint)num;
		float num3 = 768f;
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2) + CostUtility.GetMemoryAccess(argumentType3),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, (float)num2 / num3)
		};
	}

	private IValue Visit(IEvaluateContext context, OrtKISharp.Tensor inputA, OrtKISharp.Tensor inputB, Tensor<float> act)
	{
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null)
		{
			MarkerPattern markerPattern = Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard());
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
			{
				MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
				if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
				{
					List<QuantParam> quantParameter = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo.QuantParameter;
					Trace.Assert(quantParameter.Count == 1);
					float[] array = inputA.ToArray<float>();
					for (int i = 0; i < array.Length; i++)
					{
						double num = (double)array[i] / (double)quantParameter[0].Scale + (double)quantParameter[0].ZeroPoint;
						if (!quantParameter[0].Scale.Equals(1f) || quantParameter[0].ZeroPoint != 0)
						{
							num = System.Math.Round(num);
						}
						double num2 = (num - (double)quantParameter[0].ZeroPoint) * (double)quantParameter[0].Scale;
						array[i] = (float)num2;
					}
					inputA = OrtKISharp.Tensor.MakeTensor(array, inputA.Shape);
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> list = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo?.QuantParameter;
					float[] array2 = inputB.ToArray<float>();
					int count = list.Count;
					int num3 = array2.Length / count;
					for (int j = 0; j < array2.Length; j++)
					{
						double num4 = (double)array2[j] / (double)list[j / num3].Scale + (double)list[j / num3].ZeroPoint;
						if (!list[j / num3].Scale.Equals(1f) || list[j / num3].ZeroPoint != 0)
						{
							num4 = System.Math.Round(num4);
						}
						double num5 = (num4 - (double)list[j / num3].ZeroPoint) * (double)list[j / num3].Scale;
						array2[j] = (float)num5;
					}
					inputB = OrtKISharp.Tensor.MakeTensor(array2, inputB.Shape);
				}
			}
		}
		long num6 = inputA.Shape[1];
		long num7 = inputB.Shape[1];
		Tensor tensor = OrtKI.MatMul(inputA, inputB).ToTensor();
		float[] array3 = tensor.ToArray<float>();
		float[] array4 = new float[K230Kernels.ComputeSize(tensor.Shape)];
		if (num6 == num7 || (num6 > num7 && num7 == 1) || (num6 < num7 && num6 == 1))
		{
			for (int k = 0; k < array4.Length; k++)
			{
				array4[k] = K230Kernels.FakeApplyAct0(array3[k], act.ToArray<float>(), k / (tensor.Shape[2].FixedValue * tensor.Shape[3].FixedValue), 0);
			}
			return Value.FromTensor(Tensor.From(array4, tensor.Shape));
		}
		throw new InvalidOleVariantTypeException("Invalid matmul");
	}

	private IRType Visit(TensorType inputA, TensorType inputB)
	{
		if (inputA.Shape.IsUnranked || inputB.Shape.IsUnranked)
		{
			return new InvalidType("Shape InputA or InputB Can't Be Unranked");
		}
		if (inputA.Shape[3] != inputB.Shape[2])
		{
			return new InvalidType("FakeMatMul input a's cols must be equal to input b's rows");
		}
		Shape shape = new Shape(System.Math.Max(inputA.Shape[0].FixedValue, inputB.Shape[0].FixedValue), System.Math.Max(inputA.Shape[1].FixedValue, inputB.Shape[1].FixedValue), inputA.Shape[2], inputB.Shape[3]);
		return new TensorType(inputA.DType, shape);
	}

	public IValue Visit(IEvaluateContext context, FakeMatMul target)
	{
		OrtKISharp.Tensor ortArgumentValue = context.GetOrtArgumentValue(target, FakeMatMul.InputA);
		OrtKISharp.Tensor ortArgumentValue2 = context.GetOrtArgumentValue(target, FakeMatMul.InputB);
		Tensor<float> argumentValueAsTensor = context.GetArgumentValueAsTensor<float>(target, FakeMatMul.Act);
		return Visit(context, ortArgumentValue, ortArgumentValue2, argumentValueAsTensor);
	}

	public IRType Visit(ITypeInferenceContext context, FakeMatMul target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, FakeMatMul.InputA);
		TensorType inputB = context.CheckArgumentType<TensorType>(target, FakeMatMul.InputB);
		context.CheckArgumentType<IRType>(target, FakeMatMul.InputA);
		context.CheckArgumentType<IRType>(target, FakeMatMul.InputB);
		context.CheckArgumentType<IRType>(target, FakeMatMul.Act);
		return Visit(inputA, inputB);
	}
}
