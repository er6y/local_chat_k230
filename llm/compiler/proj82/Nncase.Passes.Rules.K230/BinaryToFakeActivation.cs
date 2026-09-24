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
public class BinaryToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; }

	public virtual BinaryOp Op => BinaryOp.Add;

	public virtual bool CanSupportedConstantLhs => true;

	public BinaryToFakeActivation()
	{
		Func<Binary, bool> condition = (Binary b) => b.BinaryOp == Op;
		MarkerPattern lhs = Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarkerL", Nncase.PatternMatch.Utility.IsWildcard("lhs")with
		{
			TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
		}, Nncase.PatternMatch.Utility.IsConst("inputRangeL"));
		ExprPattern target = Nncase.PatternMatch.Utility.IsWildcard("rhs")with
		{
			TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
		};
		Pattern = Nncase.PatternMatch.Utility.IsRangeOfMarker("outputMarker", Nncase.PatternMatch.F.Math.IsBinary("bn", "call", condition, lhs, Nncase.PatternMatch.Utility.IsRangeOfMarker("inputMarkerR", target, Nncase.PatternMatch.Utility.IsConst("inputRangeR")))with
		{
			TypePattern = TypePatternUtility.HasDataType(DataTypes.Float32)
		}, Nncase.PatternMatch.Utility.IsTensorConst("outputRange"));
	}

	public virtual void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
	}

	public Expr? GetReplace(Binary bn, Call call, Expr lhs, Expr rhs, Tensor<float> inputRangeL, Marker inputMarkerL, Tensor<float> inputRangeR, Marker inputMarkerR, Tensor<float> outputRange, Marker outputMarker)
	{
		if (!TryMatch(bn, lhs, rhs))
		{
			return null;
		}
		bool isConstRHS = false;
		TensorConst tensorConst = null;
		if (rhs is TensorConst tensorConst2)
		{
			isConstRHS = true;
			tensorConst = tensorConst2;
		}
		else if (lhs is TensorConst tensorConst3)
		{
			tensorConst = tensorConst3;
		}
		if (System.Math.Abs(inputRangeL[new int[1]]) >= 65504f || System.Math.Abs(inputRangeL[new int[1] { 1 }]) >= 65504f || System.Math.Abs(inputRangeR[new int[1]]) >= 65504f || System.Math.Abs(inputRangeR[new int[1] { 1 }]) >= 65504f || System.Math.Abs(outputRange[new int[1]]) >= 65504f || System.Math.Abs(outputRange[new int[1] { 1 }]) >= 65504f)
		{
			return null;
		}
		if (base.CompileSession.CompileOptions.QuantizeOptions.QuantScheme != string.Empty && (inputMarkerL?.MixQuantInfo?.MarkerQuantType == DataTypes.Float32 || inputMarkerR?.MixQuantInfo?.MarkerQuantType == DataTypes.Float32))
		{
			return null;
		}
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(lhs.CheckedShape.ToValueArray(), 0, array, array.Length - lhs.CheckedShape.Count, lhs.CheckedShape.Count);
		int[] array2 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(rhs.CheckedShape.ToValueArray(), 0, array2, array2.Length - rhs.CheckedShape.Count, rhs.CheckedShape.Count);
		int[] array3 = ((lhs.CheckedShape.Count > rhs.CheckedShape.Count) ? lhs.CheckedShape.ToValueArray() : rhs.CheckedShape.ToValueArray());
		int[] array4 = ((lhs.CheckedShape.Count > rhs.CheckedShape.Count) ? rhs.CheckedShape.ToValueArray() : lhs.CheckedShape.ToValueArray());
		for (int j = 0; j < array3.Length; j++)
		{
			if (j >= array3.Length - array4.Length && array3[j] < array4[j - (array3.Length - array4.Length)])
			{
				array3[j] = array4[j - (array3.Length - array4.Length)];
			}
		}
		int[] array5 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(array3, 0, array5, array5.Length - array3.Length, array3.Length);
		int num = array5[1];
		ActParam2 actParam = new ActParam2(num);
		if (tensorConst.CheckedShape.IsScalar)
		{
			float v3 = tensorConst.Value.ToScalar<float>();
			actParam.ForEachChannel(delegate(ActParam2 act, int i)
			{
				ProcessActParam(act, isConstRHS, i, v3);
			});
		}
		else if (tensorConst.CheckedShape.Prod() == 1)
		{
			float v2 = tensorConst.Value.ToArray<float>()[0];
			actParam.ForEachChannel(delegate(ActParam2 act, int i)
			{
				ProcessActParam(act, isConstRHS, i, v2);
			});
		}
		else
		{
			float[] v = tensorConst.Value.ToArray<float>();
			actParam.ForEachChannel(delegate(ActParam2 act, int i)
			{
				ProcessActParam(act, isConstRHS, i, v[i]);
			});
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.F.Math.RangeOfMarker(Nncase.IR.K230.F.Tensors.FakeActivation(isConstRHS ? Nncase.IR.F.Math.RangeOfMarker(lhs, inputRangeL).With(null, null, null, adaQuantInfo: inputMarkerL?.AdaQuantInfo, mixQuantInfo: inputMarkerL?.MixQuantInfo) : Nncase.IR.F.Math.RangeOfMarker(rhs, inputRangeR).With(null, null, null, adaQuantInfo: inputMarkerR?.AdaQuantInfo, mixQuantInfo: inputMarkerR?.MixQuantInfo), None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray()), outputRange).With(null, null, null, adaQuantInfo: outputMarker.AdaQuantInfo, mixQuantInfo: outputMarker.MixQuantInfo);
	}

	private bool TryMatch(Binary bn, Expr lhs, Expr rhs)
	{
		int[] array = lhs.CheckedShape.ToValueArray();
		int[] array2 = rhs.CheckedShape.ToValueArray();
		if (IsChannelwiseBinary(array, array2))
		{
			if (rhs is TensorConst && rhs.CheckedDataType == DataTypes.Float32)
			{
				return true;
			}
		}
		else if (CanSupportedConstantLhs && IsChannelwiseBinary(array2, array) && lhs is TensorConst && lhs.CheckedDataType == DataTypes.Float32)
		{
			return true;
		}
		return false;
	}

	private bool IsChannelwiseBinary(int[] shapeA, int[] shapeB)
	{
		if (shapeA.Length == 4 && shapeB.Length == 3)
		{
			if (shapeB[0] == shapeA[1] && shapeB[1] == 1)
			{
				return shapeB[2] == 1;
			}
			return false;
		}
		if (shapeA.Length == 4 && shapeB.Length == 4)
		{
			if (shapeB[0] == 1 && (shapeB[1] == shapeA[1] || shapeB[1] == 1) && shapeB[2] == 1)
			{
				return shapeB[3] == 1;
			}
			return false;
		}
		if (shapeB.Length == 1)
		{
			return shapeB[0] == 1;
		}
		if (shapeB.Length == 0)
		{
			return true;
		}
		if (shapeA.Length == 4)
		{
			if (shapeB.Length == 1)
			{
				return shapeB[0] == 1;
			}
			if (shapeB.Length == 0)
			{
				return true;
			}
		}
		return false;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Binary bn = (Binary)__result["bn"];
		Call call = (Call)__result["call"];
		Expr lhs = (Expr)__result["lhs"];
		Expr rhs = (Expr)__result["rhs"];
		Tensor<float> inputRangeL = ((TensorConst)__result["inputRangeL"]).Value.Cast<float>();
		Marker inputMarkerL = (Marker)__result["inputMarkerL"];
		Tensor<float> inputRangeR = ((TensorConst)__result["inputRangeR"]).Value.Cast<float>();
		Marker inputMarkerR = (Marker)__result["inputMarkerR"];
		Tensor<float> outputRange = ((TensorConst)__result["outputRange"]).Value.Cast<float>();
		Marker outputMarker = (Marker)__result["outputMarker"];
		return GetReplace(bn, call, lhs, rhs, inputRangeL, inputMarkerL, inputRangeR, inputMarkerR, outputRange, outputMarker);
	}
}
