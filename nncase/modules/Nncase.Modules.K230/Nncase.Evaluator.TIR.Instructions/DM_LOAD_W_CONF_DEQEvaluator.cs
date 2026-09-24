using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_W_CONF_DEQEvaluator : ITypeInferencer<DM_LOAD_W_CONF_DEQ>, ITypeInferencer, IOpPrinter<DM_LOAD_W_CONF_DEQ>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_W_CONF_DEQ target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_W_CONF_DEQ target, bool ILmode)
	{
		return $"I.DM_LOAD_W_CONF_DEQ(tcu_id: {context.GetArgument(target, DM_LOAD_W_CONF_DEQ.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_W_CONF_DEQ.pu_id)}, quant_type: {target.quant_type}, funct4: {target.funct4})";
	}
}
