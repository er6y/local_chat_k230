using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class EliminateFloat32 : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNELoad(DataTypes.Float16, Nncase.PatternMatch.F.K230.IsGNNEStore("st", "stCall", (GNNEStore op) => op.DestType == DataTypes.Float32, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst()));


	public Expr? GetReplace(Expr input, Call stCall, RunPassContext context)
	{
		if (context.GetAnalysis<IExprUserAnalysisResult>()[stCall].Count() > 1)
		{
			return null;
		}
		return Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float16, input));
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr input = (Expr)__result["input"];
		Call stCall = (Call)__result["stCall"];
		return GetReplace(input, stCall, __context);
	}
}
