using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_STRIDEEvaluator : ITypeInferencer<MFU_ACT1_CONF_STRIDE>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_STRIDE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_STRIDE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_STRIDE target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_STRIDE(rstride_s1: {target.rstride_s1}, rstride_s2: {target.rstride_s2}, rstride_d1: {target.rstride_d1}, funct5: {target.funct5})";
	}
}
