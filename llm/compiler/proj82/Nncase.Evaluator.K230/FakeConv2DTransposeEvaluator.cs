#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class FakeConv2DTransposeEvaluator : IEvaluator<FakeConv2DTranspose>, IEvaluator, ITypeInferencer<FakeConv2DTranspose>, ITypeInferencer, ICostEvaluator<FakeConv2DTranspose>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeConv2DTranspose target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeConv2DTranspose.Input);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, FakeConv2DTranspose.Weights);
		Shape shape = argumentType2.Shape;
		TensorType returnType = context.GetReturnType<TensorType>();
		Dimension dimension = shape[1] * shape[2] * shape[3];
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, (float)(dimension.FixedValue * 2) / 768f)
		};
	}

	public IValue Visit(IEvaluateContext context, FakeConv2DTranspose conv)
	{
		OrtKISharp.Tensor tensor = context.GetOrtArgumentValue(conv, FakeConv2DTranspose.Input);
		OrtKISharp.Tensor tensor2 = context.GetOrtArgumentValue(conv, FakeConv2DTranspose.Weights);
		long[] argumentValueAsArray = context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.Stride);
		context.GetOrtArgumentValue(conv, FakeConv2DTranspose.Padding);
		long[] argumentValueAsArray2 = context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.Dilation);
		long argumentValueAsScalar = context.GetArgumentValueAsScalar<long>(conv, FakeConv2DTranspose.Groups);
		long[] argumentValueAsArray3 = context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.OutputShape);
		context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.OutputPadding);
		long[] shape = tensor2.Shape;
		long[] array = context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.Padding).ToArray();
		long[] shape2 = tensor.Shape;
		long[] argumentValueAsArray4 = context.GetArgumentValueAsArray<long>(conv, FakeConv2DTranspose.OutputPadding);
		long[] shape3 = tensor2.Shape;
		if (K230Kernels.GetWindowedOutputSize((int)argumentValueAsArray3[2] + (int)array[0] + (int)array[1] - (int)argumentValueAsArray4[0], (int)shape3[2], (int)argumentValueAsArray[0], (int)argumentValueAsArray2[0], same: false) != shape2[2] || K230Kernels.GetWindowedOutputSize((int)argumentValueAsArray3[3] + (int)array[2] + (int)array[3] - (int)argumentValueAsArray4[1], (int)shape3[3], (int)argumentValueAsArray[1], (int)argumentValueAsArray2[1], same: false) != shape2[3])
		{
			throw new InvalidOleVariantTypeException("Invalid conv2d transpose shape");
		}
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(conv, FakeConv2DTranspose.Act);
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null)
		{
			MarkerPattern markerPattern = Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard());
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
			{
				MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
				if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
				{
					List<QuantParam> list = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo?.QuantParameter;
					Trace.Assert(list.Count == 1);
					float[] array2 = tensor.ToArray<float>();
					for (int j = 0; j < array2.Length; j++)
					{
						double num = (double)array2[j] / (double)list[0].Scale + (double)list[0].ZeroPoint;
						if (!list[0].Scale.Equals(1f) || list[0].ZeroPoint != 0)
						{
							num = System.Math.Round(num);
						}
						double num2 = (num - (double)list[0].ZeroPoint) * (double)list[0].Scale;
						array2[j] = (float)num2;
					}
					tensor = OrtKISharp.Tensor.MakeTensor(array2, tensor.Shape);
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> list2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo?.QuantParameter;
					float[] array3 = tensor2.ToArray<float>();
					int count = list2.Count;
					int num3 = array3.Length / count;
					for (int k = 0; k < array3.Length; k++)
					{
						double num4 = (double)array3[k] / (double)list2[k / num3].Scale + (double)list2[k / num3].ZeroPoint;
						if (!list2[k / num3].Scale.Equals(1f) || list2[k / num3].ZeroPoint != 0)
						{
							num4 = System.Math.Round(num4);
						}
						double num5 = (num4 - (double)list2[k / num3].ZeroPoint) * (double)list2[k / num3].Scale;
						array3[k] = (float)num5;
					}
					tensor2 = OrtKISharp.Tensor.MakeTensor(array3, tensor2.Shape);
				}
			}
		}
		tensor.ToArray<float>();
		tensor2.ToArray<float>();
		long num6 = argumentValueAsArray3[0] * argumentValueAsArray3[1] * argumentValueAsArray3[2] * argumentValueAsArray3[3];
		float[] array4 = new float[num6];
		Array.Clear(array4, 0, (int)num6);
		long num7 = shape2[1] / argumentValueAsScalar;
		long num8 = argumentValueAsArray3[1] / argumentValueAsScalar;
		float[] array5 = tensor2.ToArray<float>();
		float[] array6 = tensor.ToArray<float>();
		int num9 = 0;
		for (int l = 0; l < shape2[0]; l++)
		{
			Span<float> span = array4.AsSpan();
			Span<float> span2 = span.Slice(l * (int)argumentValueAsArray3[1] * (int)argumentValueAsArray3[2] * (int)argumentValueAsArray3[3]);
			for (int m = 0; m < argumentValueAsScalar; m++)
			{
				Span<float> span3 = span2.Slice(m * (int)num8 * (int)argumentValueAsArray3[2] * (int)argumentValueAsArray3[3]);
				span = array5.AsSpan();
				Span<float> span4 = span.Slice(m * (int)num8 * (int)num7 * (int)shape[2] * (int)shape[3]);
				for (int n = 0; n < num7; n++)
				{
					for (int num10 = 0; num10 < shape2[2]; num10++)
					{
						for (int num11 = 0; num11 < shape2[3]; num11++)
						{
							int num12 = (int)(num10 * argumentValueAsArray[0] - array[0]);
							int num13 = (int)(num11 * argumentValueAsArray[1] - array[2]);
							int num14 = System.Math.Max(0, (int)((-num12 + argumentValueAsArray2[0] - 1) / argumentValueAsArray2[0]));
							int num15 = (int)System.Math.Min(shape[2], ((int)argumentValueAsArray3[2] - num12 + argumentValueAsArray2[0] - 1) / argumentValueAsArray2[0]);
							int num16 = (int)System.Math.Max(0L, (-num13 + argumentValueAsArray2[1] - 1) / argumentValueAsArray2[1]);
							int num17 = (int)System.Math.Min(shape[3], ((int)argumentValueAsArray3[3] - num13 + argumentValueAsArray2[1] - 1) / argumentValueAsArray2[1]);
							float num18 = ((num11 >= 0 && num11 < shape2[3] && num10 >= 0 && num10 < shape2[2]) ? array6[num9] : 0f);
							num9++;
							for (int num19 = 0; num19 < num8; num19++)
							{
								Span<float> span5 = span3.Slice((int)(num19 * argumentValueAsArray3[2] * argumentValueAsArray3[3]));
								Span<float> span6 = span4.Slice((int)(num19 * num7 * shape[2] * shape[3])).Slice((int)(n * shape[2] * shape[3]));
								for (int num20 = num14; num20 < num15; num20++)
								{
									for (int num21 = num16; num21 < num17; num21++)
									{
										int num22 = (int)(num12 + argumentValueAsArray2[0] * num20);
										int num23 = (int)(num13 + argumentValueAsArray2[1] * num21);
										float num24 = span6[(int)(num20 * shape[3] + num21)];
										span5[(int)(num22 * argumentValueAsArray3[3] + num23)] += num18 * num24;
									}
								}
							}
						}
					}
				}
			}
		}
		Tensor<float> tensor3 = Tensor.From(array4, (from i in argumentValueAsArray3.ToArray()
			select (int)i).ToArray());
		float[] array7 = tensor3.ToArray<float>();
		float[] array8 = new float[K230Kernels.ComputeSize(tensor3.Shape)];
		int num25 = tensor3.Dimensions[2] * tensor3.Dimensions[3];
		for (int num26 = 0; num26 < array7.Length; num26++)
		{
			long num27 = num26 / num25;
			array8[num26] = K230Kernels.FakeApplyAct0(array7[num26], argumentValueAsTensor.ToArray<float>(), (int)num27, 0);
		}
		array8.Select((float x) => (float)System.Math.Round(x)).ToArray();
		return Value.FromTensor(Tensor.From(array8, (from i in argumentValueAsArray3.ToArray()
			select (int)i).ToArray()));
	}

	private IRType Visit(ITypeInferenceContext context, FakeConv2DTranspose target, TensorType input)
	{
		if (context.GetArgument(target, FakeConv2DTranspose.OutputShape) is TensorConst tensorConst)
		{
			return new TensorType(input.DType, new Shape(tensorConst.Value.ToArray<int>()));
		}
		return new InvalidType("Conv2dTranspose can't infer shape with dynamic outputShape");
	}

	public IRType Visit(ITypeInferenceContext context, FakeConv2DTranspose target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakeConv2DTranspose.Input);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Input);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Weights);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Act);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.OutputShape);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Padding);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.OutputPadding);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Stride);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Dilation);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Groups);
		context.CheckArgumentType<IRType>(target, FakeConv2DTranspose.Value);
		return Visit(context, target, input);
	}
}
