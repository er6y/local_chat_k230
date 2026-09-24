using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FuseQuantIntoPdp0Dw : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = FuseQuantHelper.MakePattern<GNNEPdp0DW>("dwCall", "dw");


	private Expr? GetReplace(Quantize quant, QuantParam qp, Call st, Expr stStrides, GNNEPdp0DW dw, IReadOnlyList<Expr> dwCallParams, RunPassContext context)
	{
		if (context.GetAnalysis<IExprUserAnalysisResult>()[st].Count() > 1)
		{
			return null;
		}
		ActParamBase actParamBase = FuseQuantHelper.FuseActAndQuant(quant, qp, dw.ActParam);
		return Tensors.GNNEStore(quant.TargetType, ReplaceUtility.ReplaceCallParams(dw.With((ActParam2)actParamBase, null, (PrimType)quant.TargetType), dwCallParams, (GNNEPdp0DW.Act, GetReplaceHelper.LoadAct0((ActParam2)actParamBase))), stStrides);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Quantize quant = (Quantize)__result["quant"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		Call st = (Call)__result["st"];
		Expr stStrides = (Expr)__result["stStrides"];
		GNNEPdp0DW dw = (GNNEPdp0DW)__result["dw"];
		IReadOnlyList<Expr> dwCallParams = (IReadOnlyList<Expr>)__result["dwCallParams"];
		return GetReplace(quant, qp, st, stStrides, dw, dwCallParams, __context);
	}
}
