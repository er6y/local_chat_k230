using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class DWToPdp : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEConv2D("dw", "dwCall", (GNNEConv2D _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad("ld", "ldCall", (GNNELoad _) => true, Nncase.PatternMatch.F.K230.IsGNNEStore("st", "stCall", (GNNEStore _) => true, Nncase.PatternMatch.F.K230.IsGNNEConv2D("conv", "convCall", (GNNEConv2D _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad("convInput", "convInputCall", (GNNELoad _) => true), null, null, null, null, null, null, null, null, null, null, null, null, Nncase.PatternMatch.Utility.IsTensorConst("convGroups")))), Nncase.PatternMatch.F.K230.IsGNNELoadW("weights", "weightsCall", (GNNELoadW _) => true), Nncase.PatternMatch.Utility.IsWildcard("weightsBias"), Nncase.PatternMatch.Utility.IsWildcard("weightsBiasQint8"), Nncase.PatternMatch.Utility.IsWildcard("act"), Nncase.PatternMatch.Utility.IsWildcard("actQint8"), Nncase.PatternMatch.Utility.IsWildcard("deqBias"), Nncase.PatternMatch.Utility.IsWildcard("shiftBits"), Nncase.PatternMatch.Utility.IsWildcard("shiftBitsQint8"), Nncase.PatternMatch.Utility.IsWildcard("qint8Qp"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("stride"), Nncase.PatternMatch.Utility.IsWildcard("dilation"), Nncase.PatternMatch.Utility.IsTensorConst("groups"), Nncase.PatternMatch.Utility.IsWildcard("is16Quant"), Nncase.PatternMatch.Utility.IsWildcard("padValue"), Nncase.PatternMatch.Utility.IsWildcard("weightsQInt8"));


	private Expr? GetReplace(Call dwCall, GNNEConv2D dw, Call ldCall, Call stCall, Call convCall, Call convInputCall, Expr convGroups, Call weightsCall, Expr weightsBias, Expr weightsBiasQint8, Expr act, Expr actQint8, Expr deqBias, Expr shiftBits, Expr shiftBitsQint8, Expr qint8Qp, Expr padding, Expr stride, Expr dilation, Expr groups, Expr is16Quant, Expr padValue, Expr weightsQInt8, RunPassContext context)
	{
		IExprUserAnalysisResult analysis = context.GetAnalysis<IExprUserAnalysisResult>();
		int fixedValue = ldCall.CheckedShape[1].FixedValue;
		int fixedValue2 = dwCall.CheckedShape[1].FixedValue;
		bool flag = fixedValue == fixedValue2 && fixedValue2 == groups && groups != 1;
		int[] subArray = weightsCall.CheckedShape.ToValueArray()[2..];
		int fixedValue3 = convInputCall.CheckedShape[1].FixedValue;
		int fixedValue4 = convCall.CheckedShape[1].FixedValue;
		bool flag2 = fixedValue3 == fixedValue4 && fixedValue4 == convGroups && convGroups != 1;
		DataType checkedDataType = ldCall.CheckedDataType;
		DataType checkedDataType2 = weightsCall.CheckedDataType;
		if (analysis[stCall].Count() <= 1 && analysis[ldCall].Count() <= 1 && flag && subArray[0] <= 3 && subArray[1] <= 3 && !flag2 && (checkedDataType == DataTypes.UInt8 || checkedDataType == DataTypes.Int8) && (checkedDataType2 == DataTypes.UInt8 || checkedDataType2 == DataTypes.Int8))
		{
			int[] array = dwCall[GNNEConv2D.Weights].CheckedShape.ToValueArray();
			array[0] = TileUtilities.GetAlignedNum(array[0], GNNEEnv.PuWidth);
			byte[] array2 = ((TensorConst)weightsCall[GNNELoadW.Input]).Value.ToArray<byte>();
			byte[] array3 = Enumerable.Repeat((byte)0, array.Aggregate((int d0, int d1) => d0 * d1)).ToArray();
			Array.Copy(array2, array3, array2.Length);
			weightsCall = Nncase.IR.K230.F.Tensors.GNNELoadW((PrimType)checkedDataType2, Tensor.FromBytes<byte>(array3, array));
			return Nncase.IR.K230.F.Tensors.GNNEPdp0DW(dw.DestType, ldCall, weightsCall, weightsQInt8, weightsBias, weightsBiasQint8, act, actQint8, deqBias, shiftBits, shiftBitsQint8, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, dw.ActParam, dw.ActParamQInt8);
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call dwCall = (Call)__result["dwCall"];
		GNNEConv2D dw = (GNNEConv2D)__result["dw"];
		Call ldCall = (Call)__result["ldCall"];
		Call stCall = (Call)__result["stCall"];
		Call convCall = (Call)__result["convCall"];
		Call convInputCall = (Call)__result["convInputCall"];
		Expr convGroups = (Expr)__result["convGroups"];
		Call weightsCall = (Call)__result["weightsCall"];
		Expr weightsBias = (Expr)__result["weightsBias"];
		Expr weightsBiasQint = (Expr)__result["weightsBiasQint8"];
		Expr act = (Expr)__result["act"];
		Expr actQint = (Expr)__result["actQint8"];
		Expr deqBias = (Expr)__result["deqBias"];
		Expr shiftBits = (Expr)__result["shiftBits"];
		Expr shiftBitsQint = (Expr)__result["shiftBitsQint8"];
		Expr qint8Qp = (Expr)__result["qint8Qp"];
		Expr padding = (Expr)__result["padding"];
		Expr stride = (Expr)__result["stride"];
		Expr dilation = (Expr)__result["dilation"];
		Expr groups = (Expr)__result["groups"];
		Expr is16Quant = (Expr)__result["is16Quant"];
		Expr padValue = (Expr)__result["padValue"];
		Expr weightsQInt = (Expr)__result["weightsQInt8"];
		return GetReplace(dwCall, dw, ldCall, stCall, convCall, convInputCall, convGroups, weightsCall, weightsBias, weightsBiasQint, act, actQint, deqBias, shiftBits, shiftBitsQint, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, weightsQInt, __context);
	}
}
