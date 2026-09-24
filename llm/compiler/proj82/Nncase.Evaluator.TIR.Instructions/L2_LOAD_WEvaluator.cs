using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class L2_LOAD_WEvaluator : ITypeInferencer<L2_LOAD_W>, ITypeInferencer, IOpPrinter<L2_LOAD_W>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, L2_LOAD_W target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, L2_LOAD_W target, bool ILmode)
	{
		return $"I.L2_LOAD_W(raddr_d: {target.raddr_d}, raddr_s: {target.raddr_s}, rvalid_c_num: {target.rvalid_c_num})";
	}
}
