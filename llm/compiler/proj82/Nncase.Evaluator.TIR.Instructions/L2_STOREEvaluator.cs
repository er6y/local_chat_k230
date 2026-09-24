using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class L2_STOREEvaluator : ITypeInferencer<L2_STORE>, ITypeInferencer, IOpPrinter<L2_STORE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, L2_STORE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, L2_STORE target, bool ILmode)
	{
		return $"I.L2_STORE(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rshape: {target.rshape})";
	}
}
