using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ExpandShapeOfGlobalReduceWindow : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.Math.IsReduce("reduce", "call", (Reduce _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("inputRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("axis"), Nncase.PatternMatch.Utility.IsTensorConst("initValue"), Nncase.PatternMatch.Utility.IsTensorConst()), Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));


	private Expr? GetReplace(Reduce reduce, Call call, Expr input, TensorConst axis, TensorConst initValue, Marker inputMarker, Marker outputMarker, Expr outputRange)
	{
		int[] inShape = input.CheckedShape.ToValueArray();
		int[] axisValue = axis.Value.ToArray<int>();
		if (!TryMatch(call.CheckedShape.ToValueArray(), inShape, axisValue) || reduce.ReduceOp == ReduceOp.Prod)
		{
			return null;
		}
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Tensors.Reduce(reduce.ReduceOp, inputMarker, axis, initValue, true), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo), call.CheckedShape), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	private bool TryMatch(int[] outShape, int[] inShape, int[] axisValue)
	{
		if (outShape.Length == 2 && inShape.Length == 4 && axisValue.Length == 2 && (axisValue[0] == 2 || axisValue[0] == -2) && (axisValue[1] == 3 || axisValue[1] == -1))
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
		Expr outputRange = (Expr)__result["outputRange"];
		return GetReplace(reduce, call, input, axis, initValue, inputMarker, outputMarker, outputRange);
	}
}
