using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_PDP1_CONF1Evaluator : ITypeInferencer<MFU_PDP1_CONF1>, ITypeInferencer, IOpPrinter<MFU_PDP1_CONF1>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_PDP1_CONF1 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_PDP1_CONF1 target, bool ILmode)
	{
		return $"I.MFU_PDP1_CONF1(stride_w: {context.GetArgument(target, MFU_PDP1_CONF1.stride_w)}, stride_h: {context.GetArgument(target, MFU_PDP1_CONF1.stride_h)}, rstride_s: {target.rstride_s}, funct2: {target.funct2}, rstride_d: {target.rstride_d}, funct5: {target.funct5})";
	}
}
