using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DIVUEvaluator : ITypeInferencer<DIVU>, ITypeInferencer, IOpPrinter<DIVU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DIVU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DIVU target, bool ILmode)
	{
		return $"I.DIVU(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
