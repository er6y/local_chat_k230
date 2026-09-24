using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class Expand1x1Conv : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEConv2D("conv", "convCall", (GNNEConv2D _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad("ld", "ldCall", (GNNELoad _) => true), Nncase.PatternMatch.F.K230.IsGNNELoadW("lw", "lwCall", (GNNELoadW _) => true), Nncase.PatternMatch.F.K230.IsGNNELoadW("lwBias", "lwBiasCall", (GNNELoadW _) => true), Nncase.PatternMatch.Utility.IsWildcard("weightsBiasQint8"), Nncase.PatternMatch.Utility.IsWildcard("act"), Nncase.PatternMatch.Utility.IsWildcard("actQint8"), Nncase.PatternMatch.Utility.IsWildcard("deqBias"), Nncase.PatternMatch.Utility.IsWildcard("shiftBits"), Nncase.PatternMatch.Utility.IsWildcard("shiftBitsQint8"), Nncase.PatternMatch.Utility.IsWildcard("qint8Qp"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("stride"), Nncase.PatternMatch.Utility.IsWildcard("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsWildcard("is16Quant"), Nncase.PatternMatch.Utility.IsWildcard("padValue"), Nncase.PatternMatch.Utility.IsWildcard("weightsQInt8"));


	private Expr? GetReplace(GNNEConv2D conv, Call ldCall, Call convCall, Call lwCall, Call lwBiasCall, Expr weightsBiasQint8, Expr act, Expr actQint8, Expr deqBias, Expr shiftBits, Expr shiftBitsQint8, Expr qint8Qp, Tensor<int> padding, int[] stride, int[] dilation, int groups, Expr is16Quant, Expr padValue, Expr weightsQInt8)
	{
		if (!GNNETypePatternUtility.IsDepthWise(ldCall, lwCall, groups) && (ldCall.CheckedDataType == DataTypes.UInt8 || ldCall.CheckedDataType == DataTypes.Int8) && (lwCall.CheckedDataType == DataTypes.UInt8 || lwCall.CheckedDataType == DataTypes.Int8) && (ldCall.CheckedShape[0].FixedValue == 1 || ldCall.CheckedShape[2].FixedValue != 1 || ldCall.CheckedShape[3].FixedValue != 1) && lwCall.CheckedShape[2].FixedValue == 1 && lwCall.CheckedShape[3].FixedValue == 1 && stride[0] == 1 && stride[1] == 1 && dilation[0] == 1 && dilation[1] == 1 && padding.Sum() == 0 && groups == 1 && ldCall.CheckedShape[2].FixedValue * ldCall.CheckedShape[3].FixedValue <= 512 && lwCall.CheckedShape[1].FixedValue > 24 && lwCall.CheckedShape[1].FixedValue % 24 != 0)
		{
			byte[] array = ((TensorConst)lwCall[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
			byte[] array2 = ((TensorConst)lwBiasCall[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
			int num = (24 - lwCall.CheckedShape[1].FixedValue % 24) % 24;
			int num2 = ((!(lwCall.CheckedDataType == DataTypes.Float16)) ? 1 : 2);
			int num3 = array.Length / lwCall.CheckedShape[0].FixedValue;
			int num4 = num3 / lwCall.CheckedShape[1].FixedValue * (lwCall.CheckedShape[1].FixedValue + num);
			byte[] array3 = new byte[num4 * lwCall.CheckedShape[0].FixedValue];
			for (int i = 0; i < lwCall.CheckedShape[0].FixedValue; i++)
			{
				byte[] array4 = Enumerable.Repeat(array2[i], num * num2).ToArray();
				byte[] array5 = new byte[num4];
				Array.Copy(array, i * num3, array5, 0, num3);
				Array.Copy(array4, 0, array5, num3, array4.Length);
				Array.Copy(array5, 0, array3, i * num4, array5.Length);
			}
			int[] array6 = lwCall.CheckedShape.ToValueArray();
			array6[1] += num;
			Call weights = Nncase.IR.K230.F.Tensors.GNNELoadW((PrimType)lwCall.CheckedDataType, Tensor.FromBytes(new TensorType(lwCall.CheckedDataType, array6), array3));
			return Nncase.IR.K230.F.Tensors.GNNEConv2D((PrimType)convCall.CheckedDataType, ldCall, weights, weightsQInt8, lwBiasCall, weightsBiasQint8, act, actQint8, deqBias, shiftBits, shiftBitsQint8, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, conv.ActParam, conv.ActParamQInt8);
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEConv2D conv = (GNNEConv2D)__result["conv"];
		Call ldCall = (Call)__result["ldCall"];
		Call convCall = (Call)__result["convCall"];
		Call lwCall = (Call)__result["lwCall"];
		Call lwBiasCall = (Call)__result["lwBiasCall"];
		Expr weightsBiasQint = (Expr)__result["weightsBiasQint8"];
		Expr act = (Expr)__result["act"];
		Expr actQint = (Expr)__result["actQint8"];
		Expr deqBias = (Expr)__result["deqBias"];
		Expr shiftBits = (Expr)__result["shiftBits"];
		Expr shiftBitsQint = (Expr)__result["shiftBitsQint8"];
		Expr qint8Qp = (Expr)__result["qint8Qp"];
		Tensor<int> padding = ((TensorConst)__result["padding"]).Value.Cast<int>();
		int[] stride = ((TensorConst)__result["stride"]).Value.ToArray<int>();
		int[] dilation = ((TensorConst)__result["dilation"]).Value.ToArray<int>();
		int groups = ((TensorConst)__result["groups"]).Value.ToScalar<int>();
		Expr is16Quant = (Expr)__result["is16Quant"];
		Expr padValue = (Expr)__result["padValue"];
		Expr weightsQInt = (Expr)__result["weightsQInt8"];
		return GetReplace(conv, ldCall, convCall, lwCall, lwBiasCall, weightsBiasQint, act, actQint, deqBias, shiftBits, shiftBitsQint, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, weightsQInt);
	}
}
