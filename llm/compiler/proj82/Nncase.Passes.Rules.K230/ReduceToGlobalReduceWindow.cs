using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReduceToGlobalReduceWindow : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Math.IsReduce("reduce", "call", (Reduce _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("axis"), Nncase.PatternMatch.Utility.IsTensorConst("initValue"), Nncase.PatternMatch.Utility.IsTensorConst());


	private Expr? GetReplace(Reduce reduce, Call call, Expr input, TensorConst axis, TensorConst initValue)
	{
		int[] array = input.CheckedShape.ToValueArray();
		int[] axisValue = axis.Value.ToArray<int>();
		if (!TryMatch(call.CheckedShape.ToValueArray(), array, axisValue) || reduce.ReduceOp == ReduceOp.Prod)
		{
			return null;
		}
		int[] array2 = new int[2]
		{
			array[2],
			array[3]
		};
		int[] array3 = new int[2] { 1, 1 };
		int[] array4 = new int[2] { 1, 1 };
		int[,] array5 = new int[2, 2];
		return Nncase.IR.F.NN.ReduceWindow2D(reduce.ReduceOp, input, initValue, array2, array3, array5, array4, false, false);
	}

	private bool TryMatch(int[] outShape, int[] inShape, int[] axisValue)
	{
		if (outShape.Length == 4 && inShape.Length == 4 && axisValue.Length == 2 && (axisValue[0] == 2 || axisValue[0] == -2) && (axisValue[1] == 3 || axisValue[1] == -1))
		{
			return true;
		}
		return false;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Reduce reduce = (Reduce)__result["reduce"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		TensorConst axis = (TensorConst)__result["axis"];
		TensorConst initValue = (TensorConst)__result["initValue"];
		return GetReplace(reduce, call, input, axis, initValue);
	}
}
