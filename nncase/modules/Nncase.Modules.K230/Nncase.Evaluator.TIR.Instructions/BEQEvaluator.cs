using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BEQEvaluator : ITypeInferencer<BEQ>, ITypeInferencer, IOpPrinter<BEQ>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BEQ target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BEQ target, bool ILmode)
	{
		return $"I.BEQ(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BEQ.offset)}, funct3: {target.funct3})";
	}
}
