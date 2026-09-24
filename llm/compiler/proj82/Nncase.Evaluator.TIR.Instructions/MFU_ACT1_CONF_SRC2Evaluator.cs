using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_SRC2Evaluator : ITypeInferencer<MFU_ACT1_CONF_SRC2>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_SRC2>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_SRC2 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_SRC2 target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_SRC2(rleft_repeats: {target.rleft_repeats}, rshape: {target.rshape}, sid: {context.GetArgument(target, MFU_ACT1_CONF_SRC2.sid)}, source_type: {target.source_type}, funct5: {target.funct5})";
	}
}
