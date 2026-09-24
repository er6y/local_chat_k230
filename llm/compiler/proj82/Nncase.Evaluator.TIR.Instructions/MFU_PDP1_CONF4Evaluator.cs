using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_PDP1_CONF4Evaluator : ITypeInferencer<MFU_PDP1_CONF4>, ITypeInferencer, IOpPrinter<MFU_PDP1_CONF4>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_PDP1_CONF4 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_PDP1_CONF4 target, bool ILmode)
	{
		return $"I.MFU_PDP1_CONF4(rwindow_w: {target.rwindow_w}, rwindow_h: {target.rwindow_h}, rscale: {target.rscale}, enable_h2c: {context.GetArgument(target, MFU_PDP1_CONF4.enable_h2c)}, enable_bw: {context.GetArgument(target, MFU_PDP1_CONF4.enable_bw)}, funct5: {target.funct5})";
	}
}
