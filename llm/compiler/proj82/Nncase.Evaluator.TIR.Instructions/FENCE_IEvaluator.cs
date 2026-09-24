using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class FENCE_IEvaluator : ITypeInferencer<FENCE_I>, ITypeInferencer, IOpPrinter<FENCE_I>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, FENCE_I target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, FENCE_I target, bool ILmode)
	{
		return "I.FENCE_I()";
	}
}
