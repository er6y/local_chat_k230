using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BLTUEvaluator : ITypeInferencer<BLTU>, ITypeInferencer, IOpPrinter<BLTU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BLTU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BLTU target, bool ILmode)
	{
		return $"I.BLTU(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BLTU.offset)}, funct3: {target.funct3})";
	}
}
