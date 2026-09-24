using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class CCR_DECLEvaluator : ITypeInferencer<CCR_DECL>, ITypeInferencer, IOpPrinter<CCR_DECL>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, CCR_DECL target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, CCR_DECL target, bool ILmode)
	{
		return $"I.CCR_DECL(rnum: {target.rnum})";
	}
}
