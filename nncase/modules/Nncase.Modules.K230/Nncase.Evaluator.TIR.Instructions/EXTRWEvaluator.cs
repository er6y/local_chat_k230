using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class EXTRWEvaluator : ITypeInferencer<EXTRW>, ITypeInferencer, IOpPrinter<EXTRW>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, EXTRW target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, EXTRW target, bool ILmode)
	{
		return $"I.EXTRW(extrd: {context.GetArgument(target, EXTRW.extrd)}, rs: {target.rs}, imm: {context.GetArgument(target, EXTRW.imm)})";
	}
}
