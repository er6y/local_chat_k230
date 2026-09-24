using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEPadEvaluator : IEvaluator<GNNEPad>, IEvaluator, ITypeInferencer<GNNEPad>, ITypeInferencer, ICostEvaluator<GNNEPad>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEPad target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEPad p)
	{
		OrtKISharp.Tensor data = context.GetArgumentValue(p, GNNEPad.Input).AsTensor().Cast<float>()
			.ToOrtTensor();
		OrtKISharp.Tensor int64OrtTensorArgumentValue = context.GetInt64OrtTensorArgumentValue(p, GNNEPad.Pads);
		return OrtKI.Cast(OrtKI.Pad(constant_value: context.GetArgumentValue(p, GNNEPad.Value).AsTensor().Cast<float>()
			.ToOrtTensor(), data: data, pads: EvaluatorUtil.ToOnnxPadFormat(int64OrtTensorArgumentValue), mode: "constant"), 10L).ToValue();
	}

	private IRType Visit(ITypeInferenceContext context, GNNEPad target, TensorType input)
	{
		Expr argument = context.GetArgument(target, GNNEPad.Pads);
		Expr argument2 = context.GetArgument(target, GNNEPad.Value);
		return TypeInference.PadType(input, argument, argument2);
	}

	public IRType Visit(ITypeInferenceContext context, GNNEPad target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEPad.Input);
		context.CheckArgumentType<IRType>(target, GNNEPad.Input);
		context.CheckArgumentType<IRType>(target, GNNEPad.Pads);
		context.CheckArgumentType<IRType>(target, GNNEPad.Value);
		return Visit(context, target, input);
	}
}
