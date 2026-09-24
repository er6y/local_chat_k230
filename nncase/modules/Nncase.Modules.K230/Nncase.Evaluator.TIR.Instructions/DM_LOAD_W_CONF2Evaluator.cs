using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class DM_LOAD_W_CONF2Evaluator : ITypeInferencer<DM_LOAD_W_CONF2>, ITypeInferencer, IOpPrinter<DM_LOAD_W_CONF2>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, DM_LOAD_W_CONF2 target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, DM_LOAD_W_CONF2 target, bool ILmode)
	{
		return $"I.DM_LOAD_W_CONF2(tcu_id: {context.GetArgument(target, DM_LOAD_W_CONF2.tcu_id)}, pu_id: {context.GetArgument(target, DM_LOAD_W_CONF2.pu_id)}, rgroups: {target.rgroups}, rgoc: {target.rgoc}, funct4: {target.funct4})";
	}
}
