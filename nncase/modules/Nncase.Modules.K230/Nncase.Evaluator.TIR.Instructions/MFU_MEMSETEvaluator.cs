using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_MEMSETEvaluator : ITypeInferencer<MFU_MEMSET>, ITypeInferencer, IOpPrinter<MFU_MEMSET>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_MEMSET target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_MEMSET target, bool ILmode)
	{
		return $"I.MFU_MEMSET(raddr_d: {target.raddr_d}, rv: {target.rv}, rstride: {target.rstride}, rshape: {target.rshape}, l2_datatype: {target.l2_datatype})";
	}
}
