using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class BGEEvaluator : ITypeInferencer<BGE>, ITypeInferencer, IOpPrinter<BGE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, BGE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, BGE target, bool ILmode)
	{
		return $"I.BGE(rs1: {target.rs1}, rs2: {target.rs2}, offset: {context.GetArgument(target, BGE.offset)}, funct3: {target.funct3})";
	}
}
