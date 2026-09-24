using System;
using System.Linq;
using Nncase.Evaluator;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ClampToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.Math.IsClamp("clamp", "call", (Clamp _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r > 1 && r <= 4, "not support scalar, and only support rank <= 4"))
	}, Nncase.PatternMatch.Utility.IsTensorConst("min"), Nncase.PatternMatch.Utility.IsTensorConst("max"));


	public Expr? GetReplace(Clamp clamp, Call call, Expr input, Tensor<float> min, Tensor<float> max)
	{
		if (!TryMatch(min, max))
		{
			return null;
		}
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(input.CheckedShape.ToValueArray(), 0, array, array.Length - input.CheckedShape.Count, input.CheckedShape.Count);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num);
		for (int i = 0; i < num; i++)
		{
			actParam.FusedClamp[i] = new ValueRange<float>(min.ToArray()[i % min.Length], max.ToArray()[i % max.Length]);
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Add, actParam, call.CheckedShape.ToValueArray());
	}

	private bool TryMatch(Tensor<float> min, Tensor<float> max)
	{
		if (TypeInference.BroadcastType(new TensorType(DataTypes.Float32, min.Shape), new TensorType(DataTypes.Float32, max.Shape)) is TensorType tensorType)
		{
			if (tensorType.Shape.IsScalar)
			{
				return true;
			}
			int[] array = tensorType.Shape.ToValueArray();
			if (!array.All((int x) => x == 1))
			{
				if (array.Length > 2)
				{
					return array.Aggregate((int x, int y) => x * y) == array[^3];
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Clamp clamp = (Clamp)__result["clamp"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		Tensor<float> min = ((TensorConst)__result["min"]).Value.Cast<float>();
		Tensor<float> max = ((TensorConst)__result["max"]).Value.Cast<float>();
		return GetReplace(clamp, call, input, min, max);
	}
}
