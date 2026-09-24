using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONFEvaluator : ITypeInferencer<MFU_ACT1_CONF>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF(funct4: {target.funct4}, is_by_channel: {context.GetArgument(target, MFU_ACT1_CONF.is_by_channel)}, is_16_segments: {context.GetArgument(target, MFU_ACT1_CONF.is_16_segments)}, funct5: {target.funct5})";
	}
}
