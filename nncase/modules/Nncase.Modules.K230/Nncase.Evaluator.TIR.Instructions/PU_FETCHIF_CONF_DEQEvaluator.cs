using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FETCHIF_CONF_DEQEvaluator : ITypeInferencer<PU_FETCHIF_CONF_DEQ>, ITypeInferencer, IOpPrinter<PU_FETCHIF_CONF_DEQ>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FETCHIF_CONF_DEQ target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FETCHIF_CONF_DEQ target, bool ILmode)
	{
		return $"I.PU_FETCHIF_CONF_DEQ(tcu_id: {context.GetArgument(target, PU_FETCHIF_CONF_DEQ.tcu_id)}, pu_id: {context.GetArgument(target, PU_FETCHIF_CONF_DEQ.pu_id)}, ric: {target.ric}, rbx: {target.rbx}, quant_type: {target.quant_type}, funct4: {target.funct4})";
	}
}
