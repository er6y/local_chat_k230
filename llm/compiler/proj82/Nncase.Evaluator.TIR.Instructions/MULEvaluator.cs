using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class MULEvaluator : ITypeInferencer<MUL>, ITypeInferencer, IOpPrinter<MUL>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, MUL target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, MUL target, bool ILmode)
	{
		return $"I.MUL(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
