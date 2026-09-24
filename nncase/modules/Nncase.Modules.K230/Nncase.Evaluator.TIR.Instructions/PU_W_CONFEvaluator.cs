using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_W_CONFEvaluator : ITypeInferencer<PU_W_CONF>, ITypeInferencer, IOpPrinter<PU_W_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_W_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_W_CONF target, bool ILmode)
	{
		return $"I.PU_W_CONF(tcu_id: {context.GetArgument(target, PU_W_CONF.tcu_id)}, pu_id: {context.GetArgument(target, PU_W_CONF.pu_id)}, kernel_h: {context.GetArgument(target, PU_W_CONF.kernel_h)}, kernel_w: {context.GetArgument(target, PU_W_CONF.kernel_w)}, funct4: {target.funct4})";
	}
}
