using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LUIEvaluator : ITypeInferencer<LUI>, ITypeInferencer, IOpPrinter<LUI>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LUI target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LUI target, bool ILmode)
	{
		return $"I.LUI(rd: {target.rd}, imm: {context.GetArgument(target, LUI.imm)})";
	}
}
