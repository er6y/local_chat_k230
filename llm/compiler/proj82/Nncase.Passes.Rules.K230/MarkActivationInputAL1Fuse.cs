using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class MarkActivationInputAL1Fuse : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsCallWildcard("actCall", Nncase.PatternMatch.Utility.IsOp<GNNEActivation>("act"), Nncase.PatternMatch.F.K230.IsGNNELoad("ld", "ldCall", (GNNELoad _) => true, Nncase.PatternMatch.Utility.IsCallWildcard("quantCall", Nncase.PatternMatch.Utility.IsOp<Quantize>(), Nncase.PatternMatch.Utility.IsCallWildcard("stCall", Nncase.PatternMatch.Utility.IsOp<GNNEStore>(), Nncase.PatternMatch.Utility.IsCallWildcard("convCall", Nncase.PatternMatch.Utility.IsOp<GNNEConv2D>(), Nncase.PatternMatch.Utility.IsWildcard("input"))))), Nncase.PatternMatch.Utility.IsWildcard("inputB"));


	private Expr? GetReplace(GNNEActivation act, Call stCall, Call ldCall, Call actCall, Call convCall, IReadOnlyList<Expr> actCallParams, RunPassContext context, Call quantCall)
	{
		int[] array = convCall[GNNEConv2D.Input].CheckedShape.ToValueArray();
		int[] array2 = convCall.CheckedShape.ToValueArray();
		int[] array3 = convCall[GNNEConv2D.Weights].CheckedShape.ToValueArray();
		int[] source = ((TensorConst)convCall[GNNEConv2D.Padding]).Value.ToArray<int>();
		int num = ((TensorConst)convCall[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)convCall[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
		int num3 = ((TensorConst)convCall[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
		int num4 = ((TensorConst)convCall[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
		int num5 = ((TensorConst)convCall[GNNEConv2D.Groups]).Value.ToScalar<int>();
		if (array[0] > 1 && array2[2] == 1 && array2[3] == 1 && array[2] == 1 && array[3] == 1 && array3[2] == 1 && array3[3] == 1 && num == 1 && num2 == 1 && num3 == 1 && num4 == 1 && source.Sum() == 0 && num5 == 1)
		{
			return null;
		}
		if (actCall[GNNEActivation.InputB] != None.Default && actCall[GNNEActivation.InputA] == actCall[GNNEActivation.InputB])
		{
			return null;
		}
		IExprUserAnalysisResult analysis = context.GetAnalysis<IExprUserAnalysisResult>();
		if (analysis[stCall].Count() > 1 || analysis[quantCall].Count() > 1 || analysis[ldCall].Count() > 1 || act.InputFromL1[1] || actCall.CheckedShape != convCall.CheckedShape)
		{
			return null;
		}
		Call item = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, stCall);
		IRArray<bool>? inputFromL = new bool[2] { true, false };
		return ReplaceUtility.ReplaceCallParams(act.With(null, null, null, null, inputFromL), actCallParams, (ldCall, item));
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEActivation act = (GNNEActivation)__result["act"];
		Call stCall = (Call)__result["stCall"];
		Call ldCall = (Call)__result["ldCall"];
		Call actCall = (Call)__result["actCall"];
		Call convCall = (Call)__result["convCall"];
		IReadOnlyList<Expr> actCallParams = (IReadOnlyList<Expr>)__result["actCallParams"];
		Call quantCall = (Call)__result["quantCall"];
		return GetReplace(act, stCall, ldCall, actCall, convCall, actCallParams, __context, quantCall);
	}
}
