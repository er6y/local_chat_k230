using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class CCR_SETEvaluator : ITypeInferencer<CCR_SET>, ITypeInferencer, IOpPrinter<CCR_SET>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, CCR_SET target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, CCR_SET target, bool ILmode)
	{
		return $"I.CCR_SET(ccr: {context.GetArgument(target, CCR_SET.ccr)}, value: {context.GetArgument(target, CCR_SET.value)})";
	}
}
