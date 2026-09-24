using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class REMUEvaluator : ITypeInferencer<REMU>, ITypeInferencer, IOpPrinter<REMU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, REMU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, REMU target, bool ILmode)
	{
		return $"I.REMU(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
