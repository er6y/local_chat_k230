using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_PDP0_MODE_CONFEvaluator : ITypeInferencer<PU_PDP0_MODE_CONF>, ITypeInferencer, IOpPrinter<PU_PDP0_MODE_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_PDP0_MODE_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_PDP0_MODE_CONF target, bool ILmode)
	{
		return $"I.PU_PDP0_MODE_CONF(tcu_id: {context.GetArgument(target, PU_PDP0_MODE_CONF.tcu_id)}, pu_id: {context.GetArgument(target, PU_PDP0_MODE_CONF.pu_id)}, mode: {target.mode}, funct4: {target.funct4})";
	}
}
