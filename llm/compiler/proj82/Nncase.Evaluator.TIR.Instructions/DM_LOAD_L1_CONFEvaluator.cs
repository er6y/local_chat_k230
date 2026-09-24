using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_L1_CONFEvaluator : ITypeInferencer<DM_LOAD_L1_CONF>, ITypeInferencer, IOpPrinter<DM_LOAD_L1_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_L1_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_L1_CONF target, bool ILmode)
	{
		return $"I.DM_LOAD_L1_CONF(tcu_id: {context.GetArgument(target, DM_LOAD_L1_CONF.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_L1_CONF.pu_id)}, rstride_s: {target.rstride_s}, datatype: {target.datatype}, l1_type: {target.l1_type}, funct4: {target.funct4})";
	}
}
