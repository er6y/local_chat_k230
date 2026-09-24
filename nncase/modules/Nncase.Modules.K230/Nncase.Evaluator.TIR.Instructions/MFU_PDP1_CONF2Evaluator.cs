using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_PDP1_CONF2Evaluator : ITypeInferencer<MFU_PDP1_CONF2>, ITypeInferencer, IOpPrinter<MFU_PDP1_CONF2>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_PDP1_CONF2 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_PDP1_CONF2 target, bool ILmode)
	{
		return $"I.MFU_PDP1_CONF2(rcount_w: {target.rcount_w}, rcount_h: {target.rcount_h}, rpe_h: {target.rpe_h}, rpe_last_h: {target.rpe_last_h}, funct5: {target.funct5})";
	}
}
