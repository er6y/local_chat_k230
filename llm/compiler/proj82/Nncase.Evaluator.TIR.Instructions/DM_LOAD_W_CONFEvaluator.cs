using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_W_CONFEvaluator : ITypeInferencer<DM_LOAD_W_CONF>, ITypeInferencer, IOpPrinter<DM_LOAD_W_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_W_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_W_CONF target, bool ILmode)
	{
		return $"I.DM_LOAD_W_CONF(tcu_id: {context.GetArgument(target, DM_LOAD_W_CONF.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_W_CONF.pu_id)}, kernel_h: {context.GetArgument(target, DM_LOAD_W_CONF.kernel_h)}, kernel_w: {context.GetArgument(target, DM_LOAD_W_CONF.kernel_w)}, rstride_oc: {target.rstride_oc}, funct4: {target.funct4})";
	}
}
