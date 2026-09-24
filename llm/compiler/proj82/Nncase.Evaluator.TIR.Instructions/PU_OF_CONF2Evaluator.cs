using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_OF_CONF2Evaluator : ITypeInferencer<PU_OF_CONF2>, ITypeInferencer, IOpPrinter<PU_OF_CONF2>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_OF_CONF2 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_OF_CONF2 target, bool ILmode)
	{
		return $"I.PU_OF_CONF2(tcu_id: {context.GetArgument(target, PU_OF_CONF2.tcu_id)}, pu_id: {context.GetArgument(target, PU_OF_CONF2.pu_id)}, raddr_d: {target.raddr_d}, rshape_d: {target.rshape_d}, funct4: {target.funct4})";
	}
}
