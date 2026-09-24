using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MFU_TRANSPOSE_CONFEvaluator : ITypeInferencer<MFU_TRANSPOSE_CONF>, ITypeInferencer, IOpPrinter<MFU_TRANSPOSE_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MFU_TRANSPOSE_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MFU_TRANSPOSE_CONF target, bool ILmode)
	{
		return $"I.MFU_TRANSPOSE_CONF(rstride_d: {target.rstride_d}, rstride_s: {target.rstride_s}, l2_datatype: {target.l2_datatype}, permute: {target.permute}, funct5: {target.funct5})";
	}
}
