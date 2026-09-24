using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class L2_LOADEvaluator : ITypeInferencer<L2_LOAD>, ITypeInferencer, IOpPrinter<L2_LOAD>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, L2_LOAD target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, L2_LOAD target, bool ILmode)
	{
		return $"I.L2_LOAD(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rshape: {target.rshape})";
	}
}
