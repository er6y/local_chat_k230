#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public sealed class FakeDynamicGNNEMatMulEvaluator : IEvaluator<FakeDynamicGNNEMatMul>, IEvaluator, ITypeInferencer<FakeDynamicGNNEMatMul>, ITypeInferencer, ICostEvaluator<FakeDynamicGNNEMatMul>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeDynamicGNNEMatMul target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.InputA);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.InputB);
		TensorType argumentType3 = context.GetArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.Act);
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
		int num2 = num;
		float num3 = 768f;
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2) + CostUtility.GetMemoryAccess(argumentType3),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, (float)num2 / num3)
		};
	}

	private IValue Visit(IEvaluateContext context, FakeDynamicGNNEMatMul op, Tensor<float> inputA, Tensor<float> inputB, Tensor<float> act)
	{
		int[] array = inputA.Dimensions.ToArray().SkipLast(2).TakeOrDefault(2, 1)
			.ToArray();
		ReadOnlySpan<int> dimensions = inputA.Dimensions;
		int num = dimensions[dimensions.Length - 2];
		dimensions = inputA.Dimensions;
		int aCols = dimensions[dimensions.Length - 1];
		int[] array2 = inputB.Dimensions.ToArray().SkipLast(2).TakeOrDefault(2, 1)
			.ToArray();
		dimensions = inputB.Dimensions;
		int num2 = dimensions[dimensions.Length - 1];
		List<int> list = inputA.Dimensions.ToArray().SkipLast(2).ToList();
		List<int> list2 = inputB.Dimensions.ToArray().SkipLast(2).ToList();
		while (list.Count < list2.Count)
		{
			list.Insert(0, 1);
		}
		while (list2.Count < list.Count)
		{
			list2.Insert(0, 1);
		}
		Tensor<float> tensor = new Tensor<float>((from p in list.Zip(list2)
			select System.Math.Max(p.First, p.Second)).Concat(new int[2] { num, num2 }).ToArray());
		Memory<float> buffer;
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
					float[] array3 = inputA.ToArray<float>();
					for (int i = 0; i < inputA.Length; i++)
					{
						double num3 = (double)array3[i] / (double)quantParameter[0].Scale + (double)quantParameter[0].ZeroPoint;
						if (quantParameter[0].Scale != 1f || quantParameter[0].ZeroPoint != 0)
						{
							num3 = System.Math.Round(num3);
						}
						double num4 = (num3 - (double)quantParameter[0].ZeroPoint) * (double)quantParameter[0].Scale;
						buffer = inputA.Buffer;
						buffer.Span[i] = (float)num4;
					}
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> quantParameter2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo.QuantParameter;
					int count = quantParameter2.Count;
					int num5 = inputB.Length / count;
					float[] array4 = inputB.ToArray<float>();
					for (int j = 0; j < inputB.Length; j++)
					{
						double num6 = (double)array4[j] / (double)quantParameter2[j / num5].Scale + (double)quantParameter2[j / num5].ZeroPoint;
						if (quantParameter2[j / num5].Scale != 1f || quantParameter2[j / num5].ZeroPoint != 0)
						{
							num6 = System.Math.Round(num6);
						}
						double num7 = (num6 - (double)quantParameter2[j / num5].ZeroPoint) * (double)quantParameter2[j / num5].Scale;
						buffer = inputB.Buffer;
						buffer.Span[j] = (float)num7;
					}
				}
			}
		}
		buffer = inputA.Buffer;
		Span<float> span = buffer.Span;
		buffer = inputB.Buffer;
		Span<float> span2 = buffer.Span;
		buffer = tensor.Buffer;
		Span<float> span3 = buffer.Span;
		buffer = act.Buffer;
		K230Kernels.FakeDynamicGnneMatmul(context, span, span2, span3, buffer.Span, array[0], array[1], num, aCols, array2[0], array2[1], num2, op.DynamicChannel);
		return Value.FromTensor(tensor);
	}

	private IRType Visit(TensorType inputA, TensorType inputB, TensorType act)
	{
		if (inputA.Shape.IsUnranked || inputB.Shape.IsUnranked)
		{
			return new InvalidType("Shape InputA or InputB Can't Be Unranked");
		}
		if (inputA.Shape.Rank < 2 || inputB.Shape.Rank < 2)
		{
			return new InvalidType("Rank InputA and InputB Must >= 2!");
		}
		List<Dimension> list = inputA.Shape.SkipLast(2).ToList();
		List<Dimension> list2 = inputB.Shape.SkipLast(2).ToList();
		while (list2.Count < list.Count)
		{
			list2.Insert(0, 1);
		}
		while (list.Count < list2.Count)
		{
			list.Insert(0, 1);
		}
		IEnumerable<Dimension> first = list.Zip(list2).Select(delegate((Dimension First, Dimension Second) p)
		{
			var (dimension, dimension2) = p;
			return (dimension.Kind == DimensionKind.Fixed && dimension2.Kind == DimensionKind.Fixed) ? ((Dimension)System.Math.Max(dimension.FixedValue, dimension2.FixedValue)) : Dimension.Unknown;
		});
		Dimension[] array = new Dimension[2];
		Shape shape = inputA.Shape;
		array[0] = shape[shape.Count - 2];
		Shape shape2 = inputB.Shape;
		array[1] = shape2[shape2.Count - 1];
		Dimension[] second = array;
		return new TensorType(inputA.DType, first.Concat(second).ToArray());
	}

	public IValue Visit(IEvaluateContext context, FakeDynamicGNNEMatMul target)
	{
		Tensor<float> argumentValueAsTensor = context.GetArgumentValueAsTensor<float>(target, FakeDynamicGNNEMatMul.InputA);
		Tensor<float> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<float>(target, FakeDynamicGNNEMatMul.InputB);
		Tensor<float> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<float>(target, FakeDynamicGNNEMatMul.Act);
		return Visit(context, target, argumentValueAsTensor, argumentValueAsTensor2, argumentValueAsTensor3);
	}

	public IRType Visit(ITypeInferenceContext context, FakeDynamicGNNEMatMul target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.InputA);
		TensorType inputB = context.CheckArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.InputB);
		TensorType act = context.CheckArgumentType<TensorType>(target, FakeDynamicGNNEMatMul.Act);
		context.CheckArgumentType<IRType>(target, FakeDynamicGNNEMatMul.InputA);
		context.CheckArgumentType<IRType>(target, FakeDynamicGNNEMatMul.InputB);
		context.CheckArgumentType<IRType>(target, FakeDynamicGNNEMatMul.Act);
		return Visit(inputA, inputB, act);
	}
}
