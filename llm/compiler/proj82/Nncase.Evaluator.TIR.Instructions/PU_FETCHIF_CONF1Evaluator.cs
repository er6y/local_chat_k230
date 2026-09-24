using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FETCHIF_CONF1Evaluator : ITypeInferencer<PU_FETCHIF_CONF1>, ITypeInferencer, IOpPrinter<PU_FETCHIF_CONF1>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FETCHIF_CONF1 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FETCHIF_CONF1 target, bool ILmode)
	{
		return $"I.PU_FETCHIF_CONF1(tcu_id: {context.GetArgument(target, PU_FETCHIF_CONF1.tcu_id)}, pu_id: {context.GetArgument(target, PU_FETCHIF_CONF1.pu_id)}, stride_w: {context.GetArgument(target, PU_FETCHIF_CONF1.stride_w)}, stride_h: {context.GetArgument(target, PU_FETCHIF_CONF1.stride_h)}, rstride_s: {target.rstride_s}, funct4: {target.funct4})";
	}
}
