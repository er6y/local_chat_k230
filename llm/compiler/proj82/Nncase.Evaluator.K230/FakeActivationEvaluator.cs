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

[TypeInferGenerator]
public class FakeActivationEvaluator : IEvaluator<FakeActivation>, IEvaluator, ITypeInferencer<FakeActivation>, ITypeInferencer, ICostEvaluator<FakeActivation>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeActivation target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeActivation.InputA);
		IRType argumentType2 = context.GetArgumentType<IRType>(target, FakeActivation.InputB);
		TensorType returnType = context.GetReturnType<TensorType>();
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + ((argumentType2 is TensorType type) ? CostUtility.GetMemoryAccess(type) : ((UInt128)(byte)0)),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, 0.125)
		};
	}

	public IValue Visit(IEvaluateContext context, FakeActivation a)
	{
		Tensor tensor = context.GetArgumentValueAsTensor(a, FakeActivation.InputA);
		IValue value = context.GetArgumentValue(a, FakeActivation.InputB);
		bool flag = value is NoneValue || ((TensorType)value.Type).Shape.Size == 0;
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(a, FakeActivation.Act);
		bool[] array = context.GetArgumentValueAsTensor(a, FakeActivation.Is16Segments).ToArray<bool>();
		int[] array2 = context.GetArgumentValueAsTensor(a, FakeActivation.OutChannels).ToArray<int>();
		Shape checkedShape = context.CurrentCall.CheckedShape;
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
					float[] array3 = tensor.ToArray<float>();
					for (int i = 0; i < array3.Length; i++)
					{
						double num = (double)array3[i] / (double)quantParameter[0].Scale + (double)quantParameter[0].ZeroPoint;
						if (!quantParameter[0].Scale.Equals(1f) || quantParameter[0].ZeroPoint != 0)
						{
							num = System.Math.Round(num);
						}
						double num2 = (num - (double)quantParameter[0].ZeroPoint) * (double)quantParameter[0].Scale;
						array3[i] = (float)num2;
					}
					tensor = Value.FromTensor(Tensor.From(array3, tensor.Shape)).AsTensor();
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> quantParameter2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo.QuantParameter;
					Trace.Assert(quantParameter2.Count == 1);
					float[] array4 = value.AsTensor().ToArray<float>();
					for (int j = 0; j < array4.Length; j++)
					{
						double num3 = (double)array4[j] / (double)quantParameter2[0].Scale + (double)quantParameter2[0].ZeroPoint;
						if (!quantParameter2[0].Scale.Equals(1f) || quantParameter2[0].ZeroPoint != 0)
						{
							num3 = System.Math.Round(num3);
						}
						double num4 = (num3 - (double)quantParameter2[0].ZeroPoint) * (double)quantParameter2[0].Scale;
						array4[j] = (float)num4;
					}
					value = Value.FromTensor(Tensor.From(array4, value.AsTensor().Shape));
				}
			}
		}
		return Value.FromConst(K230Kernels.FakeGnneActivation(array[0], tensor, flag ? new Tensor<float>(0) : value.AsTensor(), flag, checkedShape, argumentValueAsTensor.ToArray<float>(), array2[0], a.Type));
	}

	private IRType Visit(ITypeInferenceContext context, FakeActivation target, TensorType inputA)
	{
		if (!(context.GetArgument(target, FakeActivation.OutChannels) is Const))
		{
			return new InvalidType("FakeActivation out_channels need a constant value");
		}
		return new TensorType(inputA.DType, target.OutputShape.ToArray());
	}

	public IRType Visit(ITypeInferenceContext context, FakeActivation target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, FakeActivation.InputA);
		context.CheckArgumentType<IRType>(target, FakeActivation.InputA);
		context.CheckArgumentType<IRType>(target, FakeActivation.InputB);
		context.CheckArgumentType<IRType>(target, FakeActivation.Act);
		context.CheckArgumentType<IRType>(target, FakeActivation.OutChannels);
		context.CheckArgumentType<IRType>(target, FakeActivation.InAShiftBits);
		context.CheckArgumentType<IRType>(target, FakeActivation.InBShiftBits);
		context.CheckArgumentType<IRType>(target, FakeActivation.OutShiftBits);
		context.CheckArgumentType<IRType>(target, FakeActivation.Is16Segments);
		return Visit(context, target, inputA);
	}
}
