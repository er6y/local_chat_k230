using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReplaceFakeDynamicMatMulConstInBRangeToByChannel : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeDynamicGNNEMatMul("fakeMatmul", "call", (FakeDynamicGNNEMatMul _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputAMarker", Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsConst("inputARange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputBMarker", Nncase.PatternMatch.Utility.IsConst("inputB"), Nncase.PatternMatch.Utility.IsConst("inputBRange"))with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsConst("act0"));


	private Expr? GetReplace(Expr inputA, Marker inputAMarker, Tensor<float> inputARange, Expr inputB, Marker inputBMarker, RunPassContext options)
	{
		if (inputBMarker.MixQuantInfo == null)
		{
			inputBMarker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		}
		Expr act = new ActivationParameter<float>((from d in inputA.CheckedShape.SkipLast(1)
			select (!d.IsFixed) ? 1 : d.FixedValue).ToArray(), ValueRange<float>.Full).ToAct0Data<float>();
		float[] array = ((TensorConst)inputB).Value.ToArray<float>();
		int fixedValue = inputB.CheckedShape[-1].FixedValue;
		float num = float.MaxValue;
		float num2 = float.MinValue;
		List<float> list = new List<float>();
		for (int i = 0; i < fixedValue; i++)
		{
			for (int j = i; j < array.Length; j += fixedValue)
			{
				if (j < fixedValue)
				{
					num = float.MaxValue;
					num2 = float.MinValue;
				}
				if (array[j] < num)
				{
					num = array[j];
				}
				if (array[j] > num2)
				{
					num2 = array[j];
				}
				if (j >= array.Length - fixedValue)
				{
					list.Add(num);
					list.Add(num2);
				}
			}
		}
		TensorConst range = new TensorConst(Tensor.From(list.ToArray(), new int[2] { fixedValue, 2 }));
		Marker marker = Nncase.IR.F.Math.RangeOfMarker(inputB, range);
		if (marker.MixQuantInfo == null)
		{
			marker.MixQuantInfo = new K230Target.K230MixQuantInfo();
		}
		marker.MixQuantInfo = inputBMarker.MixQuantInfo;
		Shape checkedShape = inputA.CheckedShape;
		Call call = Nncase.IR.K230.F.Tensors.FakeDynamicGNNEMatMul(inputAMarker, marker, act, checkedShape[checkedShape.Count - 2].IsUnknown);
		options.MatchOptions.SuppressPattern(call, Pattern);
		return call;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr inputA = (Expr)__result["inputA"];
		Marker inputAMarker = (Marker)__result["inputAMarker"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Expr inputB = (Expr)__result["inputB"];
		Marker inputBMarker = (Marker)__result["inputBMarker"];
		return GetReplace(inputA, inputAMarker, inputARange, inputB, inputBMarker, __context);
	}
}
