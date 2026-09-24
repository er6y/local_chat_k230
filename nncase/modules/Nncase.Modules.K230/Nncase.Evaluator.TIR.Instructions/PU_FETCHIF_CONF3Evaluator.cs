using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FETCHIF_CONF3Evaluator : ITypeInferencer<PU_FETCHIF_CONF3>, ITypeInferencer, IOpPrinter<PU_FETCHIF_CONF3>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FETCHIF_CONF3 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FETCHIF_CONF3 target, bool ILmode)
	{
		return $"I.PU_FETCHIF_CONF3(tcu_id: {context.GetArgument(target, PU_FETCHIF_CONF3.tcu_id)}, pu_id: {context.GetArgument(target, PU_FETCHIF_CONF3.pu_id)}, raddr_s: {target.raddr_s}, rgroups: {target.rgroups}, rshape: {target.rshape}, funct4: {target.funct4})";
	}
}
