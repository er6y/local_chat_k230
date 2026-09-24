using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_TRANSPOSEEvaluator : ITypeInferencer<MFU_TRANSPOSE>, ITypeInferencer, IOpPrinter<MFU_TRANSPOSE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_TRANSPOSE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_TRANSPOSE target, bool ILmode)
	{
		return $"I.MFU_TRANSPOSE(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rshape: {target.rshape})";
	}
}
