using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_PDP0_FETCHIF_CONF3Evaluator : ITypeInferencer<PU_PDP0_FETCHIF_CONF3>, ITypeInferencer, IOpPrinter<PU_PDP0_FETCHIF_CONF3>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_PDP0_FETCHIF_CONF3 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_PDP0_FETCHIF_CONF3 target, bool ILmode)
	{
		return $"I.PU_PDP0_FETCHIF_CONF3(tcu_id: {context.GetArgument(target, PU_PDP0_FETCHIF_CONF3.tcu_id)}, pu_id: {context.GetArgument(target, PU_PDP0_FETCHIF_CONF3.pu_id)}, rshape: {target.rshape}, funct4: {target.funct4})";
	}
}
