using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_STORE_OFEvaluator : ITypeInferencer<DM_STORE_OF>, ITypeInferencer, IOpPrinter<DM_STORE_OF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_STORE_OF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_STORE_OF target, bool ILmode)
	{
		return $"I.DM_STORE_OF(tcu_id: {context.GetArgument(target, DM_STORE_OF.tcu_id)}, pu_id: {context.GetArgument(target, DM_STORE_OF.pu_id)}, raddr_d: {target.raddr_d}, rshape: {target.rshape}, src_channel: {target.src_channel})";
	}
}
