using Nncase.IR;
using Nncase.IR.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FiveDimWithOneBatchTranspose : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsTranspose(null, "transpose", Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsWildcard("perm")with
	{
		TypePattern = TypePatternUtility.HasRank(5)
	});


	private Expr? GetReplace(Call transpose, Expr input, TensorConst perm)
	{
		if (!OnTryMatch(input, perm))
		{
			return null;
		}
		Call input2 = Nncase.IR.F.Tensors.Reshape(transpose.CheckedShape, new Dimension[4]
		{
			transpose.CheckedShape[0],
			transpose.CheckedShape[1],
			transpose.CheckedShape[2],
			transpose.CheckedShape[3]
		});
		int[] array = new int[4];
		for (int i = 0; i < 4; i++)
		{
			array[i] = perm.Value.Cast<int>()[new int[1] { i + 1 }] - 1;
		}
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Tensors.Transpose(input2, array), transpose.CheckedShape);
	}

	private bool OnTryMatch(Expr input, TensorConst perm)
	{
		if (input.CheckedShape.Count == 5 && input.CheckedShape[0] == 1 && perm[0] == 1)
		{
			return true;
		}
		return false;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call transpose = (Call)__result["transpose"];
		Expr input = (Expr)__result["input"];
		TensorConst perm = (TensorConst)__result["perm"];
		return GetReplace(transpose, input, perm);
	}
}
