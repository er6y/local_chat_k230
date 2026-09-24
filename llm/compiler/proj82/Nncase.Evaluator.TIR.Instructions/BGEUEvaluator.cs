using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BGEUEvaluator : ITypeInferencer<BGEU>, ITypeInferencer, IOpPrinter<BGEU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BGEU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BGEU target, bool ILmode)
	{
		return $"I.BGEU(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BGEU.offset)}, funct3: {target.funct3})";
	}
}
