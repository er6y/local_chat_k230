using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LHEvaluator : ITypeInferencer<LH>, ITypeInferencer, IOpPrinter<LH>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LH target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LH target, bool ILmode)
	{
		return $"I.LH(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, LH.offset)}, funct3: {target.funct3})";
	}
}
