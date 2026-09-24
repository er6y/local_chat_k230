using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230.F;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class OptSlice : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsSlice("slice", "call", (Slice _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("begin"), Nncase.PatternMatch.Utility.IsTensorConst("ends"), Nncase.PatternMatch.Utility.IsTensorConst("axes"), Nncase.PatternMatch.Utility.IsTensorConst("strides"));


	private Expr? GetReplace(Slice slice, Call call, Expr input, TensorConst begin, TensorConst ends, TensorConst axes, TensorConst strides)
	{
		if (!(strides == new int[4] { 1, 1, 1, 1 }))
		{
			Shape checkedShape = input.CheckedShape;
			if (checkedShape[checkedShape.Count - 1].FixedValue % strides.Value.ToArray<int>()[strides.Value.ToArray<int>().Length - 1] == 0 || strides.Value.ToArray<int>()[strides.Value.ToArray<int>().Length - 1] == 1)
			{
				int[] array;
				if (strides.Value.Length == 4)
				{
					int num = 0;
					array = strides.Value.ToArray<int>();
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] > 1 && ++num == 4)
						{
							return null;
						}
					}
				}
				array = strides.CheckedShape.ToValueArray();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] < 0)
					{
						return null;
					}
				}
				List<int> list = new List<int>();
				List<int> list2 = new List<int>();
				List<int> list3 = new List<int>();
				List<int> list4 = new List<int>();
				for (int j = 0; j < input.CheckedShape.Count; j++)
				{
					if (input.CheckedShape[j].FixedValue > 1)
					{
						list.Add(begin.Value.ToArray<int>()[j]);
						list2.Add(ends.Value.ToArray<int>()[j]);
						list3.Add(strides.Value.ToArray<int>()[j]);
						list4.Add(input.CheckedShape[j].FixedValue);
					}
				}
				if (strides.Value.ToArray<int>()[3] > 1)
				{
					int num2 = list[list.Count - 1];
					int num3 = list3[list3.Count - 1];
					int num4 = list2[list2.Count - 1];
					int num5 = list4[list4.Count - 1];
					if (list.Count != 0)
					{
						list.RemoveAt(list.Count - 1);
					}
					if (list3.Count != 0)
					{
						list3.RemoveAt(list3.Count - 1);
					}
					if (list2.Count != 0)
					{
						list2.RemoveAt(list2.Count - 1);
					}
					if (list4.Count != 0)
					{
						list4.RemoveAt(list4.Count - 1);
					}
					list.Add(num2 / num3);
					list.Add(num2 % num3);
					list3.Add(1);
					list3.Add(1);
					list2.Add(num4 / num3 + ((num4 % num3 > num2 % num3) ? 1 : 0));
					list2.Add(num2 % num3 + 1);
					list4.Add(num5 / num3);
					list4.Add(num3);
				}
				for (int k = list4.Count; k < 4; k++)
				{
					list.Insert(list[0], 0);
					list2.Insert(list2[0], 1);
					list3.Insert(list3[0], 1);
				}
				Call input2 = Nncase.IR.F.Tensors.Slice(Nncase.IR.F.Tensors.Reshape(input, Enumerable.ToArray(list4)), list.ToArray(), list2.ToArray(), new int[4], new int[4] { 1, 1, 1, 1 });
				DataType obj = ((call.CheckedDataType == DataTypes.Float32) ? DataTypes.Float16 : call.CheckedDataType);
				return Nncase.IR.F.Tensors.Reshape(Nncase.IR.K230.F.Tensors.GNNEStore(obj, Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)obj, input2)), call.CheckedShape);
			}
		}
		return null;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Slice slice = (Slice)__result["slice"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		TensorConst begin = (TensorConst)__result["begin"];
		TensorConst ends = (TensorConst)__result["ends"];
		TensorConst axes = (TensorConst)__result["axes"];
		TensorConst strides = (TensorConst)__result["strides"];
		return GetReplace(slice, call, input, begin, ends, axes, strides);
	}
}
