using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReduceWithDim1ToGlobalReduceWindow : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.Math.IsReduce("reduce", "call", (Reduce _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("inputRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("axis"), Nncase.PatternMatch.Utility.IsTensorConst("initValue"), Nncase.PatternMatch.Utility.IsTensorConst()), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(Reduce reduce, Call call, Expr input, TensorConst axis, TensorConst initValue, Marker inputMarker, Marker outputMarker, Expr inputRange, Expr outputRange)
	{
		int[] array = input.CheckedShape.ToValueArray();
		int[] axisValue = axis.Value.ToArray<int>();
		if (!TryMatch(call.CheckedShape.ToValueArray(), array, axisValue) || reduce.ReduceOp == ReduceOp.Prod)
		{
			return null;
		}
		Marker input2 = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Transpose(inputMarker, new int[4] { 2, 3, 0, 1 }), inputRange).With(null, null, null, adaQuantInfo: inputMarker.AdaQuantInfo, mixQuantInfo: inputMarker.MixQuantInfo);
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.NN.ReduceWindow2D(reduce.ReduceOp, input2, initValue, new int[2]
		{
			array[0],
			array[1]
		}, new int[2] { 1, 1 }, new int[2, 2], new int[2] { 1, 1 }, false, false), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo), call.CheckedShape), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	private bool TryMatch(int[] outShape, int[] inShape, int[] axisValue)
	{
		if (outShape.Length == 4 && inShape.Length == 4 && axisValue.Length == 1 && axisValue[0] == 1 && inShape[0] == 1)
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
		Marker inputMarker = (Marker)__result["inputMarker"];
		Marker outputMarker = (Marker)__result["outputMarker"];
		Expr inputRange = (Expr)__result["inputRange"];
		Expr outputRange = (Expr)__result["outputRange"];
		return GetReplace(reduce, call, input, axis, initValue, inputMarker, outputMarker, inputRange, outputRange);
	}
}
