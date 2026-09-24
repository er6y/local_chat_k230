using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class SS_PACK_SHAPEEvaluator : ITypeInferencer<SS_PACK_SHAPE>, ITypeInferencer, IOpPrinter<SS_PACK_SHAPE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, SS_PACK_SHAPE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, SS_PACK_SHAPE target, bool ILmode)
	{
		return $"I.SS_PACK_SHAPE(rn: {target.rn}, rc: {target.rc}, rh: {target.rh}, rw: {target.rw}, rss: {target.rss})";
	}
}
