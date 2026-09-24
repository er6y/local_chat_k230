using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Imaging;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToFakeAi2dResize : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; }

	private Expr? GetReplace(Call call, ResizeImage resize, Expr input, TensorConst newSize)
	{
		if (input.CheckedDataType != DataTypes.Float32 && input.CheckedDataType != DataTypes.UInt8 && input.CheckedDataType != DataTypes.Int8)
		{
			return null;
		}
		int[] array = input.CheckedShape.ToValueArray();
		int[] array2 = call.CheckedShape.ToValueArray();
		int num = array[2] * array[3];
		int num2 = array2[2] * array2[3];
		if (array.SequenceEqual(array2))
		{
			return input;
		}
		if ((num + num2) * GNNEEnv.NPingPongSplit + SpaceSearcher.GetBasementSize() >= GNNEEnv.GlbSize)
		{
			return null;
		}
		if (array[2] == 1 && array[3] == 1)
		{
			return Nncase.IR.F.Tensors.Broadcast(input, call.CheckedShape);
		}
		MFU_CROP_RESIZE resizeMethod = (MFU_CROP_RESIZE)((resize.ResizeMode == ImageResizeMode.Bilinear) ? 0 : 1);
		int[] array3 = new int[2]
		{
			newSize.Value.ToArray<int>()[2],
			newSize.Value.ToArray<int>()[3]
		};
		return Nncase.IR.K230.F.Tensors.FakeAi2dResize(resizeMethod, resize.TransformationMode == ImageResizeTransformationMode.PytorchHalfPixel, resize.TransformationMode == ImageResizeTransformationMode.AlignCorners, input, array3);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		ResizeImage resize = (ResizeImage)__result["resize"];
		Expr input = (Expr)__result["input"];
		TensorConst newSize = (TensorConst)__result["newSize"];
		return GetReplace(call, resize, input, newSize);
	}

	public ToFakeAi2dResize()
	{
		Func<ResizeImage, bool> condition = (ResizeImage r) => true;
		ExprPattern input = Nncase.PatternMatch.Utility.IsWildcard("input")with
		{
			TypePattern = TypePatternUtility.HasFixedShape()
		};
		Pattern = Nncase.PatternMatch.F.Imaging.IsResizeImage("resize", "call", condition, input, Nncase.PatternMatch.Utility.IsWildcard("roi"), Nncase.PatternMatch.Utility.IsTensorConst("newSize"), Nncase.PatternMatch.Utility.IsWildcard("cubiccoeffa"), Nncase.PatternMatch.Utility.IsWildcard("excludeOutside"), Nncase.PatternMatch.Utility.IsWildcard("extrapolationValue"))with
		{
			TypePattern = TypePatternUtility.HasRank(4)
		};
	}
}
