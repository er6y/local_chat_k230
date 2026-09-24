using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_WEvaluator : ITypeInferencer<DM_LOAD_W>, ITypeInferencer, IOpPrinter<DM_LOAD_W>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_W target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_W target, bool ILmode)
	{
		return $"I.DM_LOAD_W(tcu_id: {context.GetArgument(target, DM_LOAD_W.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_W.pu_id)}, raddr_s: {target.raddr_s}, raddr_bw: {target.raddr_bw}, r_iochannels: {target.r_iochannels}, dest_type: {target.dest_type})";
	}
}
