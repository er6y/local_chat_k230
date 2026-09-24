using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class EXTRAWEvaluator : ITypeInferencer<EXTRAW>, ITypeInferencer, IOpPrinter<EXTRAW>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, EXTRAW target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, EXTRAW target, bool ILmode)
	{
		return $"I.EXTRAW(extrd: {context.GetArgument(target, EXTRAW.extrd)}, rs: {target.rs}, imm: {context.GetArgument(target, EXTRAW.imm)})";
	}
}
