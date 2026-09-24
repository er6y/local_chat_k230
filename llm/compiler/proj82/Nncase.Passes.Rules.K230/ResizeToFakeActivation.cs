using System;
using Nncase.IR;
using Nncase.IR.Imaging;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ResizeToFakeActivation : IRewriteRule
{
	public IPattern Pattern { get; }

	public bool OnTryMatch(ResizeImage resize, Expr input, TensorConst newSize)
	{
		if (resize.ResizeMode == ImageResizeMode.NearestNeighbor && input.CheckedShape.Count == 4 && newSize.Value.ToArray<int>()[2] % input.CheckedShape[2].FixedValue == 0 && newSize.Value.ToArray<int>()[3] % input.CheckedShape[3].FixedValue == 0)
		{
			return true;
		}
		return false;
	}

	private Expr? GetReplace(Call call, ResizeImage resize, Expr input, TensorConst newSize)
	{
		if (!OnTryMatch(resize, input, newSize))
		{
			return null;
		}
		int fixedValue = call.CheckedShape[1].FixedValue;
		ActParam2 actParam = new ActParam2(fixedValue, new QuantParam(0, 1f));
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { fixedValue, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, fixedValue, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray());
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		ResizeImage resize = (ResizeImage)__result["resize"];
		Expr input = (Expr)__result["input"];
		TensorConst newSize = (TensorConst)__result["newSize"];
		return GetReplace(call, resize, input, newSize);
	}

	public ResizeToFakeActivation()
	{
		Func<ResizeImage, bool> condition = (ResizeImage _) => true;
		ExprPattern input = Nncase.PatternMatch.Utility.IsWildcard("input")with
		{
			TypePattern = TypePatternUtility.HasFixedShape()
		};
		Pattern = Imaging.IsResizeImage("resize", "call", condition, input, Nncase.PatternMatch.Utility.IsWildcard("roi"), Nncase.PatternMatch.Utility.IsTensorConst("newSize"), Nncase.PatternMatch.Utility.IsWildcard("cubiccoeffa"), Nncase.PatternMatch.Utility.IsWildcard("excludeOutside"), Nncase.PatternMatch.Utility.IsWildcard("extrapolationValue"))with
		{
			TypePattern = TypePatternUtility.HasRank(4)
		};
	}
}
