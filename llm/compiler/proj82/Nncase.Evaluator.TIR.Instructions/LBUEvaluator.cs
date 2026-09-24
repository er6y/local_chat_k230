using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LBUEvaluator : ITypeInferencer<LBU>, ITypeInferencer, IOpPrinter<LBU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LBU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LBU target, bool ILmode)
	{
		return $"I.LBU(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, LBU.offset)}, funct3: {target.funct3})";
	}
}
