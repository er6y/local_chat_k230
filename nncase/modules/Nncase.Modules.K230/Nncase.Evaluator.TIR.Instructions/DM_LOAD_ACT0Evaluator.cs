using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_ACT0Evaluator : ITypeInferencer<DM_LOAD_ACT0>, ITypeInferencer, IOpPrinter<DM_LOAD_ACT0>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_ACT0 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_ACT0 target, bool ILmode)
	{
		return $"I.DM_LOAD_ACT0(tcu_id: {context.GetArgument(target, DM_LOAD_ACT0.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_ACT0.pu_id)}, raddr_s: {target.raddr_s}, rlen: {target.rlen}, dest_channel: {target.dest_channel}, is_by_channel: {context.GetArgument(target, DM_LOAD_ACT0.is_by_channel)})";
	}
}
