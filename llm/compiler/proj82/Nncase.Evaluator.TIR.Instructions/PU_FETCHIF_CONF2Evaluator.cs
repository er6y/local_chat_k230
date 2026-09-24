using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FETCHIF_CONF2Evaluator : ITypeInferencer<PU_FETCHIF_CONF2>, ITypeInferencer, IOpPrinter<PU_FETCHIF_CONF2>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FETCHIF_CONF2 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FETCHIF_CONF2 target, bool ILmode)
	{
		return $"I.PU_FETCHIF_CONF2(tcu_id: {context.GetArgument(target, PU_FETCHIF_CONF2.tcu_id)}, pu_id: {context.GetArgument(target, PU_FETCHIF_CONF2.pu_id)}, rgic: {target.rgic}, rgic_last: {target.rgic_last}, funct4: {target.funct4})";
	}
}
