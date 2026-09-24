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
public sealed class FuseQuantIntoPdp0Reduce : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = FuseQuantHelper.MakePattern<GNNEPdp0Reduce>("pdpCall", "pdp");


	private Expr? GetReplace(Quantize quant, QuantParam qp, Call st, Expr stStrides, GNNEPdp0Reduce pdp, IReadOnlyList<Expr> pdpCallParams, RunPassContext context)
	{
		if (context.GetAnalysis<IExprUserAnalysisResult>()[st].Count() > 1)
		{
			return null;
		}
		ActParamBase actParamBase = FuseQuantHelper.FuseActAndQuant(quant, qp, pdp.ActParam);
		DataType targetType = quant.TargetType;
		ActParam2 actParam = (ActParam2)actParamBase;
		PrimType destType = (PrimType)quant.TargetType;
		return Tensors.GNNEStore(targetType, ReplaceUtility.ReplaceCallParams(pdp.With(null, destType, actParam), pdpCallParams, (GNNEPdp0Reduce.Act, GetReplaceHelper.LoadAct0((ActParam2)actParamBase))), stStrides);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Quantize quant = (Quantize)__result["quant"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		Call st = (Call)__result["st"];
		Expr stStrides = (Expr)__result["stStrides"];
		GNNEPdp0Reduce pdp = (GNNEPdp0Reduce)__result["pdp"];
		IReadOnlyList<Expr> pdpCallParams = (IReadOnlyList<Expr>)__result["pdpCallParams"];
		return GetReplace(quant, qp, st, stStrides, pdp, pdpCallParams, __context);
	}
}
