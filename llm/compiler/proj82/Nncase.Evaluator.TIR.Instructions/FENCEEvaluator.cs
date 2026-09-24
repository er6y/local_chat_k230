using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class FENCEEvaluator : ITypeInferencer<FENCE>, ITypeInferencer, IOpPrinter<FENCE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, FENCE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, FENCE target, bool ILmode)
	{
		return "I.FENCE()";
	}
}
