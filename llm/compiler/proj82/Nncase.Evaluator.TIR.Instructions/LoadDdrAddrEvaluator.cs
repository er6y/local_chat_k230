using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.TIR.Instructions;

internal sealed class LoadDdrAddrEvaluator : ITypeInferencer<LoadDDrAddr>, ITypeInferencer, IOpPrinter<LoadDDrAddr>, IOpPrinter
{
	public IRType Visit(ITypeInferenceContext context, LoadDDrAddr target)
	{
		return TupleType.Void;
	}

	public string Visit(IIRPrinterContext context, LoadDDrAddr target, bool ILmode)
	{
		return $"I.LoadDdrAddr(rd_basement: {target.rd_basement}, value: {context.GetArgument(target, LoadDDrAddr.Basement)}; rd_offset: {target.rd_target}, value: {context.GetArgument(target, LoadDDrAddr.Offset)})";
	}
}
