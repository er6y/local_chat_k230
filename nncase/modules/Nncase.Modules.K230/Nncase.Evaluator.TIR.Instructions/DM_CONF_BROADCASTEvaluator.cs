using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_CONF_BROADCASTEvaluator : ITypeInferencer<DM_CONF_BROADCAST>, ITypeInferencer, IOpPrinter<DM_CONF_BROADCAST>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_CONF_BROADCAST target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_CONF_BROADCAST target, bool ILmode)
	{
		return $"I.DM_CONF_BROADCAST(tcu_id: {context.GetArgument(target, DM_CONF_BROADCAST.tcu_id)}, broadcast_if: {context.GetArgument(target, DM_CONF_BROADCAST.broadcast_if)}, broadcast_w: {context.GetArgument(target, DM_CONF_BROADCAST.broadcast_w)}, psum_cascade: {context.GetArgument(target, DM_CONF_BROADCAST.psum_cascade)})";
	}
}
