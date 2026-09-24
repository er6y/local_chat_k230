using System;
using Nncase.IR;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToFakePdpReduce : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsReduceWindow2D("r", "call", (ReduceWindow2D _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst("filter"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("ceilMode"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad"));


	private Expr? GetReplace(Call call, ReduceWindow2D r, Expr input, int[] filter, int[] stride, int[] padding, bool ceilMode, Expr countIncludePad)
	{
		if (r.ReduceOp == ReduceOp.Prod)
		{
			return null;
		}
		if ((filter[0] != input.CheckedShape.ToValueArray()[2] || filter[1] != input.CheckedShape.ToValueArray()[3]) && (filter[0] * filter[1] > 256 || filter[0] > 64 || filter[1] > 64))
		{
			return null;
		}
		int[,] array = new int[2, 2]
		{
			{
				padding[0],
				padding[1]
			},
			{
				padding[2],
				padding[3]
			}
		};
		if (ceilMode)
		{
			Func<int, int, int, int, int> obj = (int outSize, int k, int s, int p) => (outSize - 1) * s + k - p;
			int num = obj(call.CheckedShape[2].FixedValue, filter[0], stride[0], array[0, 0] + array[0, 1]) - input.CheckedShape[2].FixedValue;
			int num2 = obj(call.CheckedShape[3].FixedValue, filter[1], stride[1], array[1, 0] + array[1, 1]) - input.CheckedShape[3].FixedValue;
			array[0, 1] = ((num > 0) ? (array[0, 1] + num) : array[0, 1]);
			array[1, 1] = ((num2 > 0) ? (array[1, 1] + num2) : array[1, 1]);
		}
		return Nncase.IR.K230.F.Tensors.FakePdp(r.ReduceOp, input, 0, filter, stride, array, countIncludePad);
	}

	private float GetPadValue(ReduceOp reduceOp)
	{
		return reduceOp switch
		{
			ReduceOp.Mean => float.NaN, 
			ReduceOp.Min => float.MaxValue, 
			ReduceOp.Max => float.MinValue, 
			ReduceOp.Sum => float.NaN, 
			_ => throw new ArgumentOutOfRangeException("reduceOp", reduceOp, null), 
		};
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		ReduceWindow2D r = (ReduceWindow2D)__result["r"];
		Expr input = (Expr)__result["input"];
		int[] filter = ((TensorConst)__result["filter"]).Value.ToArray<int>();
		int[] stride = ((TensorConst)__result["stride"]).Value.ToArray<int>();
		int[] padding = ((TensorConst)__result["padding"]).Value.ToArray<int>();
		bool ceilMode = ((TensorConst)__result["ceilMode"]).Value.ToScalar<bool>();
		Expr countIncludePad = (Expr)__result["countIncludePad"];
		return GetReplace(call, r, input, filter, stride, padding, ceilMode, countIncludePad);
	}
}
