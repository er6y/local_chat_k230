using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNETransposeEvaluator : IEvaluator<GNNETranspose>, IEvaluator, ITypeInferencer<GNNETranspose>, ITypeInferencer, ICostEvaluator<GNNETranspose>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNETranspose target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNETranspose tr)
	{
		OrtKISharp.Tensor ortArgumentValue = context.GetOrtArgumentValue(tr, GNNETranspose.Input);
		long[] perm = GNNETypePatternUtility.ApplyPerm(tr.Perm);
		return OrtKI.Transpose(ortArgumentValue, perm).ToValue();
	}

	private IRType Visit(ITypeInferenceContext context, GNNETranspose target, TensorType input)
	{
		GNNETypePatternUtility.CheckIsValidTransposeType(input.DType);
		long[] array = GNNETypePatternUtility.ApplyPerm(target.Perm);
		return TypeInference.TransposeType(input, array);
	}

	public IRType Visit(ITypeInferenceContext context, GNNETranspose target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNETranspose.Input);
		context.CheckArgumentType<IRType>(target, GNNETranspose.Input);
		return Visit(context, target, input);
	}
}
