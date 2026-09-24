using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class ACT0_COMPUTEEvaluator : ITypeInferencer<ACT0_COMPUTE>, ITypeInferencer, IOpPrinter<ACT0_COMPUTE>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, ACT0_COMPUTE target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, ACT0_COMPUTE target, bool ILmode)
	{
		return $"I.ACT0_COMPUTE(raddr_d: {target.raddr_d}, tcu_id: {context.GetArgument(target, ACT0_COMPUTE.tcu_id)}, channel: {target.channel}, target: {target.target}, dest_datatype: {target.dest_datatype}, is_by_channel: {context.GetArgument(target, ACT0_COMPUTE.is_by_channel)})";
	}
}
