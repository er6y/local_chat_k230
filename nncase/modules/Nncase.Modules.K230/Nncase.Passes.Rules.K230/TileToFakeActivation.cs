using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("marker", Nncase.PatternMatch.F.Tensors.IsTile("tile", "call", (Nncase.IR.Tensors.Tile _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("repeats")), Nncase.PatternMatch.Utility.IsTensorConst("range"));


	public Expr? GetReplace(Nncase.IR.Tensors.Tile tile, Call call, Expr input, int[] repeats, Expr marker, Expr range)
	{
		if (call.CheckedShape.Rank > 4 || repeats.Count((int r) => r > 1) > 1)
		{
			return null;
		}
		if (repeats.Length == 4 && repeats[0] > 1 && input.CheckedShape[0].Value > 1)
		{
			return null;
		}
		if (call.CheckedDataType == DataTypes.Boolean)
		{
			return null;
		}
		int[] newRepeats = new int[4] { 1, 1, 1, 1 };
		Array.Copy(repeats, 0, newRepeats, newRepeats.Length - repeats.Length, repeats.Length);
		for (int j = 0; j < repeats.Length - 1; j++)
		{
			newRepeats[j + newRepeats.Length - repeats.Length] = repeats[j + 1];
		}
		newRepeats[^1] = 1;
		newRepeats[0] *= ((repeats.Length != 4) ? 1 : repeats[0]);
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(input.CheckedShape.ToValueArray(), 0, array, array.Length - input.CheckedShape.Rank, input.CheckedShape.Rank);
		array = array.Select((int x, int i) => x * newRepeats[i]).ToArray();
		int num = array[1];
		ActParam2 actParam = new ActParam2(num);
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Add, actParam, array), range).With(null, null, null, ((Marker)marker).MixQuantInfo, ((Marker)marker).AdaQuantInfo), call.CheckedShape);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Nncase.IR.Tensors.Tile tile = (Nncase.IR.Tensors.Tile)__result["tile"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		int[] repeats = ((TensorConst)__result["repeats"]).Value.ToArray<int>();
		Expr marker = (Expr)__result["marker"];
		Expr range = (Expr)__result["range"];
		return GetReplace(tile, call, input, repeats, marker, range);
	}
}
