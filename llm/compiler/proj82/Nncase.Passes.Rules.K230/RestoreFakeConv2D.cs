using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class RestoreFakeConv2D : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv", "call", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsConst("inputRange")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsRangeOfMarker("weightsMarker", Nncase.PatternMatch.Utility.IsTensorConst("weights"), Nncase.PatternMatch.Utility.IsConst("weightsRange")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("padding"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsTensorConst("padValue"));


	public Expr? GetReplace(FakeConv2D fakeConv, Call call, Expr inputMarker, Expr input, Tensor<float> inputRange, Expr weightsMarker, Tensor weights, Tensor<float> weightsRange, TensorConst act, Expr padding, Expr stride, Expr dilation, int groups, Tensor<float> padValue)
	{
		int fixedValue = call.CheckedShape[1].FixedValue;
		ActParam2 ap = fakeConv.ActParam;
		if (ap.FusedClamp.All((ValueRange<float> x) => x.Max == ap.FusedClamp[0].Max && x.Min == ap.FusedClamp[0].Min))
		{
			float[] array = weights.ToArray<float>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] *= ap.Ks[0, i / (array.Length / fixedValue)];
			}
			float[] array2 = new float[fixedValue];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = ap.Bs[0, j];
			}
			Tensor<float> tensor = Tensor.From(array2, new int[1] { fixedValue });
			Tensor<float> tensor2 = Tensor.From(array, weights.Shape);
			return Nncase.IR.F.NN.Conv2D(input, tensor2, tensor, stride, padding, dilation, PadMode.Constant, groups, new float[2]
			{
				ap.FusedClamp[0].Min,
				ap.FusedClamp[0].Max
			});
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeConv2D fakeConv = (FakeConv2D)__result["fakeConv"];
		Call call = (Call)__result["call"];
		Expr inputMarker = (Expr)__result["inputMarker"];
		Expr input = (Expr)__result["input"];
		Tensor<float> inputRange = ((TensorConst)__result["inputRange"]).Value.Cast<float>();
		Expr weightsMarker = (Expr)__result["weightsMarker"];
		Tensor value = ((TensorConst)__result["weights"]).Value;
		Tensor<float> weightsRange = ((TensorConst)__result["weightsRange"]).Value.Cast<float>();
		TensorConst act = (TensorConst)__result["act"];
		Expr padding = (Expr)__result["padding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Tensor<float> padValue = ((TensorConst)__result["padValue"]).Value.Cast<float>();
		return GetReplace(fakeConv, call, inputMarker, input, inputRange, weightsMarker, value, weightsRange, act, padding, stride, dilation, groups, padValue);
	}
}
