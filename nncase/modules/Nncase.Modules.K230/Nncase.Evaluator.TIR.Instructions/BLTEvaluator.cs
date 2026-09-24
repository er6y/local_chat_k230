using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BLTEvaluator : ITypeInferencer<BLT>, ITypeInferencer, IOpPrinter<BLT>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BLT target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BLT target, bool ILmode)
	{
		return $"I.BLT(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BLT.offset)}, funct3: {target.funct3})";
	}
}
