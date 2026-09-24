using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_COMPUTEEvaluator : ITypeInferencer<MFU_ACT1_COMPUTE>, ITypeInferencer, IOpPrinter<MFU_ACT1_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_COMPUTE target, bool ILmode)
	{
		return $"I.MFU_ACT1_COMPUTE(raddr_d1: {target.raddr_d1}, raddr_s1: {target.raddr_s1}, raddr_s2: {target.raddr_s2}, raddr_arg: {target.raddr_arg})";
	}
}
