using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LBEvaluator : ITypeInferencer<LB>, ITypeInferencer, IOpPrinter<LB>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LB target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LB target, bool ILmode)
	{
		return $"I.LB(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, LB.offset)}, funct3: {target.funct3})";
	}
}
