using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class Ai2dResizeEvaluator : IEvaluator<Ai2dResize>, IEvaluator, ITypeInferencer<Ai2dResize>, ITypeInferencer, ICostEvaluator<Ai2dResize>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, Ai2dResize target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, Ai2dResize r)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(r, Ai2dResize.Input);
		int[] argumentValueAsArray = context.GetArgumentValueAsArray<int>(r, Ai2dResize.NewSize);
		int inDeqBias = context.GetArgumentValueAsArray<int>(r, Ai2dResize.InDeqBias)[0];
		QuantParam quantParam = context.GetArgumentValueAsArray<QuantParam>(r, Ai2dResize.OutQuantParam)[0];
		if (r.ResizeMethod == MFU_CROP_RESIZE.BILINER)
		{
			return Value.FromTensor(K230Kernels.Ai2dResizeBilinear(r.OutputDatatype, argumentValueAsTensor, argumentValueAsArray, r.AlignCorners, r.HalfPixelCenters, inDeqBias, quantParam));
		}
		return Value.FromTensor(K230Kernels.Ai2dResizeNearestNeighbor(r.OutputDatatype, argumentValueAsTensor, argumentValueAsArray, r.AlignCorners, r.HalfPixelCenters, inDeqBias, quantParam));
	}

	private IRType Visit(ITypeInferenceContext context, Ai2dResize target, TensorType input)
	{
		Expr argument = context.GetArgument(target, Ai2dResize.NewSize);
		if (input.Shape[2] == 1 && input.Shape[3] == 1)
		{
			return new InvalidType("Ai2dResize doesn't support 1x1 Input");
		}
		return TypeInference.ResizeType(input, argument, null);
	}

	public IRType Visit(ITypeInferenceContext context, Ai2dResize target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, Ai2dResize.Input);
		context.CheckArgumentType<IRType>(target, Ai2dResize.Input);
		context.CheckArgumentType<IRType>(target, Ai2dResize.NewSize);
		context.CheckArgumentType<IRType>(target, Ai2dResize.InDeqBias);
		context.CheckArgumentType<IRType>(target, Ai2dResize.OutQuantParam);
		return Visit(context, target, input);
	}
}
