using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_PDP1_COMPUTEEvaluator : ITypeInferencer<MFU_PDP1_COMPUTE>, ITypeInferencer, IOpPrinter<MFU_PDP1_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_PDP1_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_PDP1_COMPUTE target, bool ILmode)
	{
		return $"I.MFU_PDP1_COMPUTE(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rshape: {target.rshape})";
	}
}
