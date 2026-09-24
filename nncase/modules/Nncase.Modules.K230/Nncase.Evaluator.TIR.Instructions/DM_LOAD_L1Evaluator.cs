using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_L1Evaluator : ITypeInferencer<DM_LOAD_L1>, ITypeInferencer, IOpPrinter<DM_LOAD_L1>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_L1 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_L1 target, bool ILmode)
	{
		return $"I.DM_LOAD_L1(tcu_id: {context.GetArgument(target, DM_LOAD_L1.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_L1.pu_id)}, raddr_s: {target.raddr_s}, rhtoc_window: {target.rhtoc_window}, rshape: {target.rshape}, l1_type: {target.l1_type})";
	}
}
