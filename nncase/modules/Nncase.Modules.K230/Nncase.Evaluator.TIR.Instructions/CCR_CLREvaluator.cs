using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class CCR_CLREvaluator : ITypeInferencer<CCR_CLR>, ITypeInferencer, IOpPrinter<CCR_CLR>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, CCR_CLR target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, CCR_CLR target, bool ILmode)
	{
		return $"I.CCR_CLR(ccr: {context.GetArgument(target, CCR_CLR.ccr)})";
	}
}
