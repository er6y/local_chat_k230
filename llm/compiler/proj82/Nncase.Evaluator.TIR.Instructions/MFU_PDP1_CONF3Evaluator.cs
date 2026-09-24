using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_PDP1_CONF3Evaluator : ITypeInferencer<MFU_PDP1_CONF3>, ITypeInferencer, IOpPrinter<MFU_PDP1_CONF3>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_PDP1_CONF3 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_PDP1_CONF3 target, bool ILmode)
	{
		return $"I.MFU_PDP1_CONF3(rpe_channels: {target.rpe_channels}, rpe_last_channels: {target.rpe_last_channels}, rpad_value: {target.rpad_value}, sspad: {target.sspad}, funct5: {target.funct5})";
	}
}
