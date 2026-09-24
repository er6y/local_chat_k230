using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class ADDEvaluator : ITypeInferencer<ADD>, ITypeInferencer, IOpPrinter<ADD>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, ADD target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, ADD target, bool ILmode)
	{
		return $"I.ADD(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
