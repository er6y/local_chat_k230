using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_FORWARD_PSUMEvaluator : ITypeInferencer<PU_FORWARD_PSUM>, ITypeInferencer, IOpPrinter<PU_FORWARD_PSUM>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_FORWARD_PSUM target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_FORWARD_PSUM target, bool ILmode)
	{
		return $"I.PU_FORWARD_PSUM(tcu_id: {context.GetArgument(target, PU_FORWARD_PSUM.tcu_id)}, pu_id: {context.GetArgument(target, PU_FORWARD_PSUM.pu_id)}, raddr: {target.raddr}, rlen: {target.rlen})";
	}
}
