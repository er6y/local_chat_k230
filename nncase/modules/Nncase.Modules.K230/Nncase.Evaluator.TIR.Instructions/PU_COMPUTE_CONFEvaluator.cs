using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class PU_COMPUTE_CONFEvaluator : ITypeInferencer<PU_COMPUTE_CONF>, ITypeInferencer, IOpPrinter<PU_COMPUTE_CONF>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, PU_COMPUTE_CONF target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, PU_COMPUTE_CONF target, bool ILmode)
	{
		return $"I.PU_COMPUTE_CONF(tcu_id: {context.GetArgument(target, PU_COMPUTE_CONF.tcu_id)}, pu_id: {context.GetArgument(target, PU_COMPUTE_CONF.pu_id)}, load_psum: {context.GetArgument(target, PU_COMPUTE_CONF.load_psum)}, clr_psum: {context.GetArgument(target, PU_COMPUTE_CONF.clr_psum)}, dest_target: {target.dest_target}, release_if: {context.GetArgument(target, PU_COMPUTE_CONF.release_if)}, mode: {target.mode}, funct4: {target.funct4})";
	}
}
