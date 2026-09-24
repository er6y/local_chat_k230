using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class REMEvaluator : ITypeInferencer<REM>, ITypeInferencer, IOpPrinter<REM>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, REM target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, REM target, bool ILmode)
	{
		return $"I.REM(rd: {target.rd}, rs1: {target.rs1}, rs2: {target.rs2}, funct5: {target.funct5})";
	}
}
