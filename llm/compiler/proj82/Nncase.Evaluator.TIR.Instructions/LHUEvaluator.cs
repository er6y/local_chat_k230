using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LHUEvaluator : ITypeInferencer<LHU>, ITypeInferencer, IOpPrinter<LHU>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LHU target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LHU target, bool ILmode)
	{
		return $"I.LHU(rd: {target.rd}, rs: {target.rs}, offset: {context.GetArgument(target, LHU.offset)}, funct3: {target.funct3})";
	}
}
