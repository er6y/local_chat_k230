using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FETCHIF_CONF4Evaluator : ITypeInferencer<PU_FETCHIF_CONF4>, ITypeInferencer, IOpPrinter<PU_FETCHIF_CONF4>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FETCHIF_CONF4 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FETCHIF_CONF4 target, bool ILmode)
	{
		return $"I.PU_FETCHIF_CONF4(tcu_id: {context.GetArgument(target, PU_FETCHIF_CONF4.tcu_id)}, pu_id: {context.GetArgument(target, PU_FETCHIF_CONF4.pu_id)}, rpad_value: {target.rpad_value}, sspad: {target.sspad}, funct4: {target.funct4})";
	}
}
