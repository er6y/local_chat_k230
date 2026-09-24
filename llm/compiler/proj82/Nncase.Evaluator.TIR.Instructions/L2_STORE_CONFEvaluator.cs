using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class L2_STORE_CONFEvaluator : ITypeInferencer<L2_STORE_CONF>, ITypeInferencer, IOpPrinter<L2_STORE_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, L2_STORE_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, L2_STORE_CONF target, bool ILmode)
	{
		return $"I.L2_STORE_CONF(rstride_d: {target.rstride_d}, rstride_s: {target.rstride_s}, l2_datatype: {target.l2_datatype}, ddr_datatype: {target.ddr_datatype})";
	}
}
