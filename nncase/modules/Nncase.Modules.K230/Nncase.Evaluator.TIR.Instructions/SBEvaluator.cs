using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class SBEvaluator : ITypeInferencer<SB>, ITypeInferencer, IOpPrinter<SB>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, SB target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, SB target, bool ILmode)
	{
		return $"I.SB(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, SB.offset)}, funct3: {target.funct3})";
	}
}
