using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class AUIPCEvaluator : ITypeInferencer<AUIPC>, ITypeInferencer, IOpPrinter<AUIPC>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, AUIPC target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, AUIPC target, bool ILmode)
	{
		return $"I.AUIPC(rd: {target.rd}, imm: {context.GetArgument(target, AUIPC.imm)})";
	}
}
