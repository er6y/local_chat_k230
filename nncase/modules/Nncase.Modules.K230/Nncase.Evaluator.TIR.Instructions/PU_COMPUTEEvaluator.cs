using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_COMPUTEEvaluator : ITypeInferencer<PU_COMPUTE>, ITypeInferencer, IOpPrinter<PU_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_COMPUTE target, bool ILmode)
	{
		return $"I.PU_COMPUTE(tcu_id: {context.GetArgument(target, PU_COMPUTE.tcu_id)}, of_shift_mode: {target.of_shift_mode})";
	}
}
