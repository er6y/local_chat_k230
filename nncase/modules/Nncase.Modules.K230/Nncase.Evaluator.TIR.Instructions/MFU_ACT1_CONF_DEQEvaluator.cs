using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_DEQEvaluator : ITypeInferencer<MFU_ACT1_CONF_DEQ>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_DEQ>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_DEQ target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_DEQ target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_DEQ(rscale: {target.rscale}, rbias: {target.rbias}, quant_type: {target.quant_type}, sid: {context.GetArgument(target, MFU_ACT1_CONF_DEQ.sid)}, funct5: {target.funct5}, rshift_bits: {context.GetArgument(target, MFU_ACT1_CONF_DEQ.rshift_bits)})";
	}
}
