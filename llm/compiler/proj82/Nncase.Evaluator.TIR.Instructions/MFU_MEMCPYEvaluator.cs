using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_MEMCPYEvaluator : ITypeInferencer<MFU_MEMCPY>, ITypeInferencer, IOpPrinter<MFU_MEMCPY>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_MEMCPY target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_MEMCPY target, bool ILmode)
	{
		return $"I.MFU_MEMCPY(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rstride_d: {target.rstride_d}, rstride_s: {target.rstride_s}, rshape: {target.rshape})";
	}
}
