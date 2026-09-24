using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_ACT1_CONF_SRC1Evaluator : ITypeInferencer<MFU_ACT1_CONF_SRC1>, ITypeInferencer, IOpPrinter<MFU_ACT1_CONF_SRC1>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_ACT1_CONF_SRC1 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_ACT1_CONF_SRC1 target, bool ILmode)
	{
		return $"I.MFU_ACT1_CONF_SRC1(rslice: {target.rslice}, rright_repeats: {target.rright_repeats}, rslice_repeats: {target.rslice_repeats}, sid: {context.GetArgument(target, MFU_ACT1_CONF_SRC1.sid)}, slice_loc: {target.slice_loc}, funct5: {target.funct5})";
	}
}
