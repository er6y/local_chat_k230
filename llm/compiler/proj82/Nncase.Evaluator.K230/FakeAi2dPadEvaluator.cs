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

[TypeInferGenerator]
public class FakeAi2dPadEvaluator : IEvaluator<FakeAi2dPad>, IEvaluator, ITypeInferencer<FakeAi2dPad>, ITypeInferencer, ICostEvaluator<FakeAi2dPad>, ICostEvaluator
{
	public IValue Visit(IEvaluateContext context, FakeAi2dPad r)
	{
		OrtKISharp.Tensor tensor = context.GetArgumentValue(r, FakeAi2dPad.Input).AsTensor().Cast<float>()
			.ToOrtTensor();
		OrtKISharp.Tensor int64OrtTensorArgumentValue = context.GetInt64OrtTensorArgumentValue(r, FakeAi2dPad.Padding);
		OrtKISharp.Tensor constant_value = context.GetArgumentValue(r, FakeAi2dPad.Value).AsTensor().Cast<float>()
			.ToOrtTensor();
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
				tensor = OrtKISharp.Tensor.MakeTensor(array, tensor.Shape);
			}
		}
		if (r.Mode == PadMode.Symmetric)
		{
			throw new NotImplementedException();
		}
		return OrtKI.Cast(OrtKI.Pad(tensor, EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), constant_value, r.Mode.ToString().ToLower(null)), 1L).ToValue();
	}

	public Cost Visit(ICostEvaluateContext context, FakeAi2dPad target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeAi2dPad.Input);
		TensorType returnType = context.GetReturnType<TensorType>();
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) / (byte)2,
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType) / (byte)2
		};
	}

	private IRType Visit(ITypeInferenceContext context, FakeAi2dPad target, TensorType input)
	{
		Expr argument = context.GetArgument(target, FakeAi2dPad.Padding);
		Expr argument2 = context.GetArgument(target, FakeAi2dPad.Value);
		return TypeInference.PadType(input, argument, argument2);
	}

	public IRType Visit(ITypeInferenceContext context, FakeAi2dPad target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakeAi2dPad.Input);
		context.CheckArgumentType<IRType>(target, FakeAi2dPad.Input);
		context.CheckArgumentType<IRType>(target, FakeAi2dPad.Padding);
		context.CheckArgumentType<IRType>(target, FakeAi2dPad.Value);
		return Visit(context, target, input);
	}
}
