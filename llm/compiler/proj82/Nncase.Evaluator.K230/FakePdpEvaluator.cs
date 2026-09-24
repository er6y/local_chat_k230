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
public class FakePdpEvaluator : IEvaluator<FakePdp>, IEvaluator, ITypeInferencer<FakePdp>, ITypeInferencer, ICostEvaluator<FakePdp>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakePdp target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakePdp.Input);
		TensorType returnType = context.GetReturnType<TensorType>();
		float num = 1f;
		float num2 = 5f;
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, num / num2)
		};
	}

	public IValue Visit(IEvaluateContext context, FakePdp r)
	{
		OrtKISharp.Tensor tensor = context.GetOrtArgumentValue(r, FakePdp.Input);
		long[] argumentValueAsArray = context.GetArgumentValueAsArray<long>(r, FakePdp.Filter);
		long[] argumentValueAsArray2 = context.GetArgumentValueAsArray<long>(r, FakePdp.Stride);
		long[] argumentValueAsArray3 = context.GetArgumentValueAsArray<long>(r, FakePdp.Padding);
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
		return (r.ReduceOp switch
		{
			ReduceOp.Min => throw new NotSupportedException("Unsupported MFU_PDP_OP MIN"), 
			ReduceOp.Max => OrtKI.MaxPool(tensor, "NOTSET", 0L, new long[2] { 1L, 1L }, argumentValueAsArray, argumentValueAsArray3, 0L, argumentValueAsArray2)[0], 
			ReduceOp.Mean => OrtKI.AveragePool(tensor, "NOTSET", 0L, 0L, argumentValueAsArray, argumentValueAsArray3, argumentValueAsArray2), 
			ReduceOp.Sum => throw new NotSupportedException("Unsupported MFU_PDP_OP SUM"), 
			_ => throw new ArgumentOutOfRangeException("r"), 
		}).ToValue();
	}

	private IRType Visit(ITypeInferenceContext context, FakePdp target, TensorType input)
	{
		Expr[] arguments = context.GetArguments(target, FakePdp.Filter, FakePdp.Stride, FakePdp.Padding);
		IRType iRType = TypeInference.ReduceWindow2DType(input, arguments[0], arguments[1], arguments[2], false);
		return new TensorType(DataTypes.Float32, ((TensorType)iRType).Shape);
	}

	public IRType Visit(ITypeInferenceContext context, FakePdp target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakePdp.Input);
		context.CheckArgumentType<IRType>(target, FakePdp.Input);
		context.CheckArgumentType<IRType>(target, FakePdp.PadValue);
		context.CheckArgumentType<IRType>(target, FakePdp.Filter);
		context.CheckArgumentType<IRType>(target, FakePdp.Stride);
		context.CheckArgumentType<IRType>(target, FakePdp.Padding);
		context.CheckArgumentType<IRType>(target, FakePdp.CountIncludePad);
		return Visit(context, target, input);
	}
}
