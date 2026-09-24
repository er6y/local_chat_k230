using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MMU_SETIDEvaluator : ITypeInferencer<MMU_SETID>, ITypeInferencer, IOpPrinter<MMU_SETID>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MMU_SETID target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MMU_SETID target, bool ILmode)
	{
		return $"I.MMU_SETID(rd: {target.rd}, mmu_id: {context.GetArgument(target, MMU_SETID.mmu_id)})";
	}
}
