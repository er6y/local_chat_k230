using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class Ai2dPadEvaluator : IEvaluator<Ai2dPad>, IEvaluator, ITypeInferencer<Ai2dPad>, ITypeInferencer, ICostEvaluator<Ai2dPad>, ICostEvaluator
{
	public IValue Visit(IEvaluateContext context, Ai2dPad r)
	{
		Tensor tensor = context.GetArgumentValue(r, Ai2dPad.Input).AsTensor();
		OrtKISharp.Tensor int64OrtTensorArgumentValue = context.GetInt64OrtTensorArgumentValue(r, Ai2dPad.Padding);
		Tensor tensor2 = context.GetArgumentValue(r, Ai2dPad.Value).AsTensor();
		float[] array = tensor.ToArray<float>();
		float[] array2 = tensor2.ToArray<float>();
		if (r.OutputType == DataTypes.UInt8)
		{
			return OrtKI.Cast(OrtKI.Pad(OrtKISharp.Tensor.MakeTensor(array, ((IEnumerable<int>)tensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), OrtKISharp.Tensor.MakeTensor(array2, ((IEnumerable<int>)tensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), r.Mode.ToString().ToLower(null)), 2L).ToValue();
		}
		if (r.OutputType == DataTypes.Int8)
		{
			return OrtKI.Cast(OrtKI.Pad(OrtKISharp.Tensor.MakeTensor(array, ((IEnumerable<int>)tensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), OrtKISharp.Tensor.MakeTensor(array2, ((IEnumerable<int>)tensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), r.Mode.ToString().ToLower(null)), 3L).ToValue();
		}
		if (r.OutputType == DataTypes.Int16)
		{
			return OrtKI.Cast(OrtKI.Pad(OrtKISharp.Tensor.MakeTensor(array, ((IEnumerable<int>)tensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), OrtKISharp.Tensor.MakeTensor(array2, ((IEnumerable<int>)tensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), r.Mode.ToString().ToLower(null)), 5L).ToValue();
		}
		return OrtKI.Cast(OrtKI.Pad(OrtKISharp.Tensor.MakeTensor(array, ((IEnumerable<int>)tensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), OrtKISharp.Tensor.MakeTensor(array2, ((IEnumerable<int>)tensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), r.Mode.ToString().ToLower(null)), 10L).ToValue();
	}

	public Cost Visit(ICostEvaluateContext context, Ai2dPad target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	private IRType Visit(ITypeInferenceContext context, Ai2dPad target, TensorType input)
	{
		Expr argument = context.GetArgument(target, Ai2dPad.Padding);
		Expr argument2 = context.GetArgument(target, Ai2dPad.Value);
		return TypeInference.PadType(input, argument, argument2);
	}

	public IRType Visit(ITypeInferenceContext context, Ai2dPad target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, Ai2dPad.Input);
		context.CheckArgumentType<IRType>(target, Ai2dPad.Input);
		context.CheckArgumentType<IRType>(target, Ai2dPad.Padding);
		context.CheckArgumentType<IRType>(target, Ai2dPad.Value);
		context.CheckArgumentType<IRType>(target, Ai2dPad.InDeqBias);
		context.CheckArgumentType<IRType>(target, Ai2dPad.OutQuantParam);
		return Visit(context, target, input);
	}
}
