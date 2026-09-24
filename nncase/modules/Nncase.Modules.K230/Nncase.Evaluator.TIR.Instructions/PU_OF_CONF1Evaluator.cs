using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_OF_CONF1Evaluator : ITypeInferencer<PU_OF_CONF1>, ITypeInferencer, IOpPrinter<PU_OF_CONF1>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_OF_CONF1 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_OF_CONF1 target, bool ILmode)
	{
		return $"I.PU_OF_CONF1(tcu_id: {context.GetArgument(target, PU_OF_CONF1.tcu_id)}, pu_id: {context.GetArgument(target, PU_OF_CONF1.pu_id)}, rgoc: {target.rgoc}, rgoc_last: {target.rgoc_last}, rstride_d: {target.rstride_d}, funct4: {target.funct4})";
	}
}
