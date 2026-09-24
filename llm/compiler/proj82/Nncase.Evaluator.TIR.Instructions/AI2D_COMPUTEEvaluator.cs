using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class AI2D_COMPUTEEvaluator : ITypeInferencer<AI2D_COMPUTE>, ITypeInferencer, IOpPrinter<AI2D_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, AI2D_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, AI2D_COMPUTE target, bool ILmode)
	{
		return "I.AI2D_COMPUTE()";
	}
}
