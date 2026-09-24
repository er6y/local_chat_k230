using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class L2_LOAD_W_CONFEvaluator : ITypeInferencer<L2_LOAD_W_CONF>, ITypeInferencer, IOpPrinter<L2_LOAD_W_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, L2_LOAD_W_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, L2_LOAD_W_CONF target, bool ILmode)
	{
		return $"I.L2_LOAD_W_CONF(rlen_compressed: {target.rlen_compressed}, rlen_decompressed: {target.rlen_decompressed}, l2_datatype: {target.l2_datatype}, ddr_datatype: {target.ddr_datatype}, enable_decompress: {context.GetArgument(target, L2_LOAD_W_CONF.enable_decompress)})";
	}
}
