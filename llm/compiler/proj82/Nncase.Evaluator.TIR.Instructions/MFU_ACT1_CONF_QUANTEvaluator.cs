using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_QUANTEvaluator : ITypeInferencer<MFU_ACT1_CONF_QUANT>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_QUANT>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_QUANT target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_QUANT target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_QUANT(quant_type: {target.quant_type}, funct5: {target.funct5}, rshift_bits: {context.GetArgument(target, MFU_ACT1_CONF_QUANT.rshift_bits)})";
	}
}
