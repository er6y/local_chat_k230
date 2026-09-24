using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class SUBEvaluator : ITypeInferencer<SUB>, ITypeInferencer, IOpPrinter<SUB>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, SUB target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, SUB target, bool ILmode)
	{
		return $"I.SUB(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
