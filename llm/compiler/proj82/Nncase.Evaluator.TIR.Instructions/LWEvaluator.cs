using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LWEvaluator : ITypeInferencer<LW>, ITypeInferencer, IOpPrinter<LW>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LW target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LW target, bool ILmode)
	{
		return $"I.LW(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, LW.offset)}, funct3: {target.funct3})";
	}
}
