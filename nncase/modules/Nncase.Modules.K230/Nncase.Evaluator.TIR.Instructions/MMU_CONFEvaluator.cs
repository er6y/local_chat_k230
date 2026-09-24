using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MMU_CONFEvaluator : ITypeInferencer<MMU_CONF>, ITypeInferencer, IOpPrinter<MMU_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MMU_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MMU_CONF target, bool ILmode)
	{
		return $"I.MMU_CONF(rstart: {target.rstart}, rdepth: {target.rdepth}, mmu_id: {context.GetArgument(target, MMU_CONF.mmu_id)})";
	}
}
