#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class FakeAi2dResizeEvaluator : IEvaluator<FakeAi2dResize>, IEvaluator, ITypeInferencer<FakeAi2dResize>, ITypeInferencer, ICostEvaluator<FakeAi2dResize>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeAi2dResize target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeAi2dResize.Input);
		TensorType returnType = context.GetReturnType<TensorType>();
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType)
		};
	}

	public IValue Visit(IEvaluateContext context, FakeAi2dResize r)
	{
		Tensor tensor = context.GetArgumentValueAsTensor(r, FakeAi2dResize.Input);
		int[] argumentValueAsArray = context.GetArgumentValueAsArray<int>(r, FakeAi2dResize.NewSize);
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null && Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard()).MatchLeaf(context.CurrentCall.Arguments[0]))
		{
			MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
			if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
			{
				List<QuantParam> quantParameter = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo.QuantParameter;
				Trace.Assert(quantParameter.Count == 1);
				float[] array = tensor.ToArray<float>();
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
				tensor = Value.FromTensor(Tensor.From(array, tensor.Shape)).AsTensor();
			}
		}
		if (r.ResizeMethod == MFU_CROP_RESIZE.BILINER)
		{
			return Value.FromTensor(K230Kernels.FakeAi2dResizeBilinear(tensor, argumentValueAsArray, r.AlignCorners, r.HalfPixelCenters));
		}
		return Value.FromTensor(K230Kernels.FakeAi2dResizeNearestNeighbor(tensor, argumentValueAsArray, r.AlignCorners, r.HalfPixelCenters));
	}

	private IRType Visit(ITypeInferenceContext context, FakeAi2dResize target, TensorType input)
	{
		if (input.Shape[2] == 1 && input.Shape[3] == 1)
		{
			return new InvalidType("FakeAi2dResize doesn't support 1x1 input");
		}
		Expr argument = context.GetArgument(target, FakeAi2dResize.NewSize);
		return TypeInference.ResizeType(input, argument, null);
	}

	public IRType Visit(ITypeInferenceContext context, FakeAi2dResize target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakeAi2dResize.Input);
		context.CheckArgumentType<IRType>(target, FakeAi2dResize.Input);
		context.CheckArgumentType<IRType>(target, FakeAi2dResize.NewSize);
		return Visit(context, target, input);
	}
}
