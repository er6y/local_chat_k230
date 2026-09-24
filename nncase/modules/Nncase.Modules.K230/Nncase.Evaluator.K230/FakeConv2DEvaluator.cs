#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public class FakeConv2DEvaluator : IEvaluator<FakeConv2D>, IEvaluator, ITypeInferencer<FakeConv2D>, ITypeInferencer, ICostEvaluator<FakeConv2D>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeConv2D target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeConv2D.Input);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, FakeConv2D.Weights);
		TensorType returnType = context.GetReturnType<TensorType>();
		Shape shape = argumentType2.Shape;
		Dimension dimension = 2 * shape[1] * shape[2] * shape[3] - 1;
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, (float)dimension.FixedValue / 768f)
		};
	}

	private Const Visit(IEvaluateContext context, OrtKISharp.Tensor input, OrtKISharp.Tensor weights, Tensor act, Tensor<long> stride, OrtKISharp.Tensor padding, Tensor<long> dilation, long groups)
	{
		MarkerPattern markerPattern = Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard());
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null)
		{
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
			{
				MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
				if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
				{
					List<QuantParam> quantParameter = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo.QuantParameter;
					Trace.Assert(quantParameter.Count == 1);
					float[] array = input.ToArray<float>();
					for (int i = 0; i < array.Length; i++)
					{
						double num = (double)array[i] / (double)quantParameter[0].Scale + (double)quantParameter[0].ZeroPoint;
						if (quantParameter[0].Scale != 1f || quantParameter[0].ZeroPoint != 0)
						{
							num = System.Math.Round(num);
						}
						double num2 = (num - (double)quantParameter[0].ZeroPoint) * (double)quantParameter[0].Scale;
						array[i] = (float)num2;
					}
					input = OrtKISharp.Tensor.MakeTensor(array, input.Shape);
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> quantParameter2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo.QuantParameter;
					float[] array2 = weights.ToArray<float>();
					int count = quantParameter2.Count;
					int num3 = array2.Length / count;
					for (int j = 0; j < array2.Length; j++)
					{
						double num4 = (double)array2[j] / (double)quantParameter2[j / num3].Scale + (double)quantParameter2[j / num3].ZeroPoint;
						if (quantParameter2[j / num3].Scale != 1f || quantParameter2[j / num3].ZeroPoint != 0)
						{
							num4 = System.Math.Round(num4);
						}
						double num5 = (num4 - (double)quantParameter2[j / num3].ZeroPoint) * (double)quantParameter2[j / num3].Scale;
						array2[j] = (float)num5;
					}
					weights = OrtKISharp.Tensor.MakeTensor(array2, weights.Shape);
				}
			}
		}
		if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
		{
			AdaQuantInfo? adaQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).AdaQuantInfo;
			if (adaQuantInfo != null)
			{
				_ = adaQuantInfo.InputQuantParameter;
				if (true)
				{
					QuantParam inputQuantParameter = ((Marker)context.CurrentCall.Arguments[0]).AdaQuantInfo.InputQuantParameter;
					float[] array3 = input.ToArray<float>();
					for (int k = 0; k < array3.Length; k++)
					{
						double num6 = (double)array3[k] / (double)inputQuantParameter.Scale + (double)inputQuantParameter.ZeroPoint;
						if (inputQuantParameter.Scale != 1f || inputQuantParameter.ZeroPoint != 0)
						{
							num6 = System.Math.Round(num6);
						}
						double num7 = (num6 - (double)inputQuantParameter.ZeroPoint) * (double)inputQuantParameter.Scale;
						array3[k] = (float)num7;
					}
					input = OrtKISharp.Tensor.MakeTensor(array3, input.Shape);
				}
			}
		}
		Tensor tensor = OrtKI.Conv(input, weights, K230Kernels.Proc((int)weights.Shape[0]), "NOTSET", dilation.ToArray(), groups, new long[2]
		{
			weights.Shape[2],
			weights.Shape[3]
		}, EvaluatorUtil.ToOnnxPadFormat(padding), stride.ToArray()).ToTensor();
		if (context.CurrentCall.Arguments[0] is Marker)
		{
			if (((Marker)context.CurrentCall.Arguments[0]).AdaQuantInfo == null)
			{
				((Marker)context.CurrentCall.Arguments[0]).AdaQuantInfo = new AdaQuantInfo();
			}
			((Marker)context.CurrentCall.Arguments[0]).AdaQuantInfo.AdaRoundRefTensor = tensor;
		}
		float[] array4 = tensor.ToArray<float>();
		float[] array5 = new float[K230Kernels.ComputeSize(tensor.Shape)];
		int num8 = tensor.Shape[2].FixedValue * tensor.Shape[3].FixedValue;
		for (int l = 0; l < array4.Length; l++)
		{
			int channel = l / num8;
			array5[l] = K230Kernels.FakeApplyAct0(array4[l], act.ToArray<float>(), channel, 0);
		}
		return Const.FromTensor(Tensor.From(array5, tensor.Shape));
	}

	private IRType Visit(ITypeInferenceContext context, FakeConv2D target, TensorType input, TensorType weights)
	{
		Expr[] arguments = context.GetArguments(target, FakeConv2D.Stride, FakeConv2D.Padding, FakeConv2D.Dilation, FakeConv2D.Groups);
		return TypeInference.Conv2DType(input, weights, arguments[0], arguments[1], arguments[2], arguments[3]);
	}

	public IValue Visit(IEvaluateContext context, FakeConv2D target)
	{
		OrtKISharp.Tensor ortArgumentValue = context.GetOrtArgumentValue(target, FakeConv2D.Input);
		OrtKISharp.Tensor ortArgumentValue2 = context.GetOrtArgumentValue(target, FakeConv2D.Weights);
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(target, FakeConv2D.Act);
		Tensor<long> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<long>(target, FakeConv2D.Stride);
		OrtKISharp.Tensor ortArgumentValue3 = context.GetOrtArgumentValue(target, FakeConv2D.Padding);
		Tensor<long> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<long>(target, FakeConv2D.Dilation);
		long argumentValueAsScalar = context.GetArgumentValueAsScalar<long>(target, FakeConv2D.Groups);
		return Value.FromConst(Visit(context, ortArgumentValue, ortArgumentValue2, argumentValueAsTensor, argumentValueAsTensor2, ortArgumentValue3, argumentValueAsTensor3, argumentValueAsScalar));
	}

	public IRType Visit(ITypeInferenceContext context, FakeConv2D target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakeConv2D.Input);
		TensorType weights = context.CheckArgumentType<TensorType>(target, FakeConv2D.Weights);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Input);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Weights);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Act);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Padding);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Stride);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Dilation);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Groups);
		context.CheckArgumentType<IRType>(target, FakeConv2D.Value);
		return Visit(context, target, input, weights);
	}
}
