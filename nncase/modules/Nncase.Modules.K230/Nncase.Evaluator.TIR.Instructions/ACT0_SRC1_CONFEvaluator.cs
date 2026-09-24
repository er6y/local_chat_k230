using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class ACT0_SRC1_CONFEvaluator : ITypeInferencer<ACT0_SRC1_CONF>, ITypeInferencer, IOpPrinter<ACT0_SRC1_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, ACT0_SRC1_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, ACT0_SRC1_CONF target, bool ILmode)
	{
		return $"I.ACT0_SRC1_CONF(tcu_id: {context.GetArgument(target, ACT0_SRC1_CONF.tcu_id)}, pu_id: {context.GetArgument(target, ACT0_SRC1_CONF.pu_id)}, channel: {target.channel}, rshape: {target.rshape}, funct3: {target.funct3}, rshift_bits: {context.GetArgument(target, ACT0_SRC1_CONF.rshift_bits)})";
	}
}
