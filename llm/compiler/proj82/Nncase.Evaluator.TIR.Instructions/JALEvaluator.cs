using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class JALEvaluator : ITypeInferencer<JAL>, ITypeInferencer, IOpPrinter<JAL>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, JAL target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, JAL target, bool ILmode)
	{
		return $"I.JAL(rd: {target.rd}, offset: {context.GetArgument(target, JAL.offset)})";
	}
}
