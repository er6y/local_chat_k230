using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_STORE_OF_CONFEvaluator : ITypeInferencer<DM_STORE_OF_CONF>, ITypeInferencer, IOpPrinter<DM_STORE_OF_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_STORE_OF_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_STORE_OF_CONF target, bool ILmode)
	{
		return $"I.DM_STORE_OF_CONF(tcu_id: {context.GetArgument(target, DM_STORE_OF_CONF.tcu_id)}, pu_id: {context.GetArgument(target, DM_STORE_OF_CONF.pu_id)}, rstride_d: {target.rstride_d}, datatype: {target.datatype}, funct4: {target.funct4})";
	}
}
