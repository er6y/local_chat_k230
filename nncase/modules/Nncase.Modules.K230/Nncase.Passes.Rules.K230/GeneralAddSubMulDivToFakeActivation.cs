using System;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class GeneralAddSubMulDivToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; }

	public Expr? GetReplace(Binary bn, Marker inputMarkerL, Expr lhs, Tensor<float> inputRangeL, Marker inputMarkerR, Expr rhs, Tensor<float> inputRangeR, Marker outputMarker, Call output, Tensor<float> outputRange)
	{
		if (bn.BinaryOp != 0 && bn.BinaryOp != BinaryOp.Sub && bn.BinaryOp != BinaryOp.Mul)
		{
			return null;
		}
		if (lhs.CheckedDataType != DataTypes.Float32 || output.CheckedDataType != DataTypes.Float32 || rhs.CheckedDataType != DataTypes.Float32)
		{
			return null;
		}
		if (base.CompileSession.CompileOptions.QuantizeOptions.QuantScheme != string.Empty && (inputMarkerL?.MixQuantInfo?.MarkerQuantType == DataTypes.Float32 || inputMarkerR?.MixQuantInfo?.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		if (System.Math.Abs(inputRangeL[new int[1]]) >= 65504f || System.Math.Abs(inputRangeL[new int[1] { 1 }]) >= 65504f || System.Math.Abs(inputRangeR[new int[1]]) >= 65504f || System.Math.Abs(inputRangeR[new int[1] { 1 }]) >= 65504f || System.Math.Abs(outputRange[new int[1]]) >= 65504f || System.Math.Abs(outputRange[new int[1] { 1 }]) >= 65504f)
		{
			return null;
		}
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(lhs.CheckedShape.ToValueArray(), 0, array, array.Length - lhs.CheckedShape.Count, lhs.CheckedShape.Count);
		int[] array2 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(rhs.CheckedShape.ToValueArray(), 0, array2, array2.Length - rhs.CheckedShape.Count, rhs.CheckedShape.Count);
		int[] array3 = ((lhs.CheckedShape.Count > rhs.CheckedShape.Count) ? lhs.CheckedShape.ToValueArray() : rhs.CheckedShape.ToValueArray());
		int[] array4 = ((lhs.CheckedShape.Count > rhs.CheckedShape.Count) ? rhs.CheckedShape.ToValueArray() : lhs.CheckedShape.ToValueArray());
		for (int i = 0; i < array3.Length; i++)
		{
			if (i >= array3.Length - array4.Length && array3[i] < array4[i - (array3.Length - array4.Length)])
			{
				array3[i] = array4[i - (array3.Length - array4.Length)];
			}
		}
		int[] array5 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(array3, 0, array5, array5.Length - array3.Length, array3.Length);
		int num = array5[1];
		ActParam2 actParam = new ActParam2(num);
		GnneActivationType type = ((bn.BinaryOp != 0 && bn.BinaryOp != BinaryOp.Sub) ? GnneActivationType.Mul : GnneActivationType.Add);
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		Marker marker = Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.F.Math.Unary(UnaryOp.Neg, inputMarkerR), new float[2]
		{
			0f - inputRangeR[new int[1] { 1 }],
			0f - inputRangeR[new int[1]]
		}).With(null, null, null, adaQuantInfo: inputMarkerR?.AdaQuantInfo, mixQuantInfo: inputMarkerR?.MixQuantInfo);
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.FakeActivation(inputMarkerL.With(null, lhs, inputRangeL, null, null), (bn.BinaryOp == BinaryOp.Sub) ? marker : inputMarkerR.With(null, rhs, inputRangeR, null, null), tensor, num, 0, 0, 0, false, type, actParam, output.CheckedShape.ToValueArray()), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Binary bn = (Binary)__result["bn"];
		Marker inputMarkerL = (Marker)__result["inputMarkerL"];
		Expr lhs = (Expr)__result["lhs"];
		Tensor<float> inputRangeL = ((TensorConst)__result["inputRangeL"]).Value.Cast<float>();
		Marker inputMarkerR = (Marker)__result["inputMarkerR"];
		Expr rhs = (Expr)__result["rhs"];
		Tensor<float> inputRangeR = ((TensorConst)__result["inputRangeR"]).Value.Cast<float>();
		Marker outputMarker = (Marker)__result["outputMarker"];
		Call output = (Call)__result["output"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		return GetReplace(bn, inputMarkerL, lhs, inputRangeL, inputMarkerR, rhs, inputRangeR, outputMarker, output, outputRange);
	}

	public GeneralAddSubMulDivToFakeActivation()
	{
		Func<Binary, bool> condition = (Binary _) => true;
		MarkerPattern lhs = Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarkerL", Nncase.PatternMatch.Utility.IsWildcard("lhs")with
		{
			TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
		}, Nncase.PatternMatch.Utility.IsTensorConst("inputRangeL"));
		ExprPattern target = Nncase.PatternMatch.Utility.IsWildcard("rhs")with
		{
			TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
		};
		Pattern = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.Math.IsBinary("bn", "output", condition, lhs, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarkerR", target, Nncase.PatternMatch.Utility.IsTensorConst("inputRangeR")))with
		{
			TypePattern = TypePatternUtility.HasDataType(DataTypes.Float32)
		}, Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));
	}
}
