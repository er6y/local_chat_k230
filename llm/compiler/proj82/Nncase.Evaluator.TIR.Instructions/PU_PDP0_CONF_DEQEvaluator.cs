using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_PDP0_CONF_DEQEvaluator : ITypeInferencer<PU_PDP0_CONF_DEQ>, ITypeInferencer, IOpPrinter<PU_PDP0_CONF_DEQ>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_PDP0_CONF_DEQ target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_PDP0_CONF_DEQ target, bool ILmode)
	{
		return $"I.PU_PDP0_CONF_DEQ(tcu_id: {context.GetArgument(target, PU_PDP0_CONF_DEQ.tcu_id)}, pu_id: {context.GetArgument(target, PU_PDP0_CONF_DEQ.pu_id)}, rbx: {target.rbx}, quant_type: {target.quant_type}, funct4: {target.funct4})";
	}
}
