using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_PDP0_COMPUTEEvaluator : ITypeInferencer<PU_PDP0_COMPUTE>, ITypeInferencer, IOpPrinter<PU_PDP0_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_PDP0_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_PDP0_COMPUTE target, bool ILmode)
	{
		return $"I.PU_PDP0_COMPUTE(tcu_id: {context.GetArgument(target, PU_PDP0_COMPUTE.tcu_id)}, raddr_s: {target.raddr_s})";
	}
}
