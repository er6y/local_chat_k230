using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LoadImmEvaluator : ITypeInferencer<LoadImm>, ITypeInferencer, IOpPrinter<LoadImm>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LoadImm target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LoadImm target, bool ILmode)
	{
		return $"I.LoadImm(rd: {target.Rd}, value: {context.GetArgument(target, LoadImm.Value)})";
	}
}
