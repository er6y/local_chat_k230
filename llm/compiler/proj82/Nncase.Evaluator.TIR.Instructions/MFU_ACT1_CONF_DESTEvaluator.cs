using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_DESTEvaluator : ITypeInferencer<MFU_ACT1_CONF_DEST>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_DEST>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_DEST target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_DEST target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_DEST(rlen: {target.rlen}, rshape: {target.rshape}, funct5: {target.funct5})";
	}
}
