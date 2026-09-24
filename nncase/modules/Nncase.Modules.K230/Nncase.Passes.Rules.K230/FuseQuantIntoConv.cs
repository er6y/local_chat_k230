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
public sealed class FuseQuantIntoConv : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = FuseQuantHelper.MakePattern<GNNEConv2D>("convCall", "conv");


	private Expr? GetReplace(Quantize quant, QuantParam qp, Call st, Expr stStrides, GNNEConv2D conv, IReadOnlyList<Expr> convCallParams, RunPassContext context)
	{
		if (context.GetAnalysis<IExprUserAnalysisResult>()[st].Count() > 1)
		{
			return null;
		}
		ActParamBase actParamBase = FuseQuantHelper.FuseActAndQuant(quant, qp, conv.ActParam);
		return Tensors.GNNEStore(quant.TargetType, ReplaceUtility.ReplaceCallParams(conv.With((ActParam2)actParamBase, null, (PrimType)quant.TargetType), convCallParams, (GNNEConv2D.Act, GetReplaceHelper.LoadAct0((ActParam2)actParamBase))), stStrides);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Quantize quant = (Quantize)__result["quant"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		Call st = (Call)__result["st"];
		Expr stStrides = (Expr)__result["stStrides"];
		GNNEConv2D conv = (GNNEConv2D)__result["conv"];
		IReadOnlyList<Expr> convCallParams = (IReadOnlyList<Expr>)__result["convCallParams"];
		return GetReplace(quant, qp, st, stStrides, conv, convCallParams, __context);
	}
}
