using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class SS_PACK_STRIDEEvaluator : ITypeInferencer<SS_PACK_STRIDE>, ITypeInferencer, IOpPrinter<SS_PACK_STRIDE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, SS_PACK_STRIDE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, SS_PACK_STRIDE target, bool ILmode)
	{
		return $"I.SS_PACK_STRIDE(rn: {target.rn}, rc: {target.rc}, rh: {target.rh}, rss: {target.rss})";
	}
}
