using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BNEEvaluator : ITypeInferencer<BNE>, ITypeInferencer, IOpPrinter<BNE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BNE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BNE target, bool ILmode)
	{
		return $"I.BNE(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BNE.offset)}, funct3: {target.funct3})";
	}
}
