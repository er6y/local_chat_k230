using System;
using System.Runtime.InteropServices;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules;

public static class GetReplaceHelper
{
	public static Func<Expr, Expr> WithTmpGNNEShape(Func<Expr, Expr> inputCtor, int[] originOutShape)
	{
		return Utility.WithTmp4DShape(inputCtor, originOutShape);
	}

	public static Func<Expr, Expr> WithTmpGNNEShape(Func<Expr, Expr> inputCtor)
	{
		Func<Expr, Expr> inputCtor2 = inputCtor;
		return (Expr input) => WithTmpGNNEShape(inputCtor2, input.CheckedShape.ToValueArray())(input);
	}

	public static Expr ToGNNEShape(Expr input)
	{
		return Nncase.IR.F.Tensors.Reshape(input, GNNETypePatternUtility.GetGNNEShape(input.CheckedShape.ToValueArray()));
	}

	public static Expr ToF16(Expr input)
	{
		Call call = Nncase.IR.F.Tensors.Cast(input, DataTypes.Float16);
		if (input is TensorConst)
		{
			return call.Evaluate().AsTensor();
		}
		return call;
	}

	public static Tensor ToTensor(Expr expr)
	{
		if (expr is TensorConst tensorConst)
		{
			return tensorConst.Value;
		}
		throw new InvalidOperationException("Expr is not a TensorCosnt");
	}

	public static T[] ToArray<T>(Expr expr) where T : unmanaged, IEquatable<T>
	{
		return ToTensor(expr).ToArray<T>();
	}

	public static T ToScalar<T>(Expr expr) where T : unmanaged, IEquatable<T>
	{
		return ToTensor(expr).ToScalar<T>();
	}

	public static Func<Expr, Expr> WithFullLower(Func<Expr, Expr> inputCtor, int[] outShape)
	{
		return WithTmpGNNEShape(WithLoadStore(inputCtor), outShape);
	}

	public static Func<Expr, Expr> WithLoadStore(Func<Expr, Expr> inputCtor)
	{
		return Utility.Apply(WithLoadStoreImpl, inputCtor);
		static Func<Expr, Expr> WithLoadStoreImpl(Func<Expr, Expr> inCtor)
		{
			Func<Expr, Expr> inCtor2 = inCtor;
			return (Expr input) => Nncase.IR.K230.F.Tensors.GNNEStore(input.CheckedDataType, inCtor2(Nncase.IR.K230.F.Tensors.GNNELoad((input.CheckedDataType == DataTypes.Float32) ? DataTypes.Float16 : ((PrimType)input.CheckedDataType), input)));
		}
	}

	public static Expr LoadActIF(Expr input)
	{
		return Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.Float16, input);
	}

	public static Expr LoadAct0(ActParam2 actParam)
	{
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam.ToAct0Data());
	}

	public static Expr LoadAct1(ActParamBase actParam)
	{
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam.ToAct1Data());
	}

	public static Expr SuppressPattern(RunPassContext options, Expr expr, IPattern pattern)
	{
		options.MatchOptions.SuppressPattern(expr, pattern);
		return expr;
	}

	public static ActParam2? FoldAct0WithAct1(ActParam2 act0Param, Expr is16Segments, TensorConst outChannels, ActParam2 act1Param, TensorConst deqParams = null)
	{
		if (is16Segments == true)
		{
			return null;
		}
		int channels = act0Param.Channels;
		int num = outChannels.Value.ToScalar<int>();
		if (channels != num && num != 1)
		{
			return null;
		}
		if (deqParams != null)
		{
			for (int i = 0; i < channels; i++)
			{
				float num2 = MemoryMarshal.Cast<byte, float>(deqParams.Value.BytesBuffer)[1];
				int num3 = MemoryMarshal.Cast<byte, int>(deqParams.Value.BytesBuffer)[0];
				act0Param.Ks[0, i] *= num2;
				act0Param.Ks[1, i] *= num2;
				act0Param.Bs[0, i] = (act0Param.Bs[0, i] - (float)num3) * num2;
				act0Param.Bs[1, i] = (act0Param.Bs[1, i] - (float)num3) * num2;
				act0Param.FusedClamp[i].Max = (act0Param.FusedClamp[i].Max - (float)num3) * num2;
				act0Param.FusedClamp[i].Min = (act0Param.FusedClamp[i].Min - (float)num3) * num2;
			}
		}
		ActFoldParam actFoldParam = new ActFoldParam(channels);
		for (int j = 0; j < channels; j++)
		{
			float num4 = ((num == 1) ? act1Param.Ks[0, 0] : act1Param.Ks[0, j]);
			float num5 = ((num == 1) ? act1Param.Ks[1, 0] : act1Param.Ks[1, j]);
			float num6 = ((num == 1) ? act1Param.Bs[0, 0] : act1Param.Bs[0, j]);
			float num7 = ((num == 1) ? act1Param.Bs[1, 0] : act1Param.Bs[1, j]);
			float num8 = ((num == 1) ? act1Param.Xs[0, 0] : act1Param.Xs[0, j]);
			float num9 = ((num == 1) ? act1Param.FusedClamp[0].Min : act1Param.FusedClamp[j].Min);
			float num10 = ((num == 1) ? act1Param.FusedClamp[0].Max : act1Param.FusedClamp[j].Max);
			if (num8 >= act0Param.FusedClamp[j].Max)
			{
				if (num4 > 0f)
				{
					float num11 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? act0Param.FusedClamp[j].Max : (act0Param.FusedClamp[j].Max * num4 + num6));
					float num12 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? act0Param.FusedClamp[j].Min : (act0Param.FusedClamp[j].Min * num4 + num6));
					actFoldParam.FusedClamp[j].Max = ((num11 < num10) ? ((Half)num11) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = ((num12 > num9) ? ((Half)num12) : ((Half)num9));
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
				else if (num4 < 0f)
				{
					float num13 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? (0f - act0Param.FusedClamp[j].Max) : (act0Param.FusedClamp[j].Max * num4 + num6));
					float num14 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? (0f - act0Param.FusedClamp[j].Min) : (act0Param.FusedClamp[j].Min * num4 + num6));
					actFoldParam.FusedClamp[j].Max = ((num14 < num10) ? ((Half)num14) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = ((num13 > num9) ? ((Half)num13) : ((Half)num9));
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
				else
				{
					actFoldParam.FusedClamp[j].Max = (Half)num10;
					actFoldParam.FusedClamp[j].Min = (Half)num9;
				}
				continue;
			}
			if (num8 <= act0Param.FusedClamp[j].Min)
			{
				if (!(num5 > 0f))
				{
					if (num5 < 0f)
					{
						float num15 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? (0f - act0Param.FusedClamp[j].Max) : (act0Param.FusedClamp[j].Max * num5 + num7));
						float num16 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? (0f - act0Param.FusedClamp[j].Min) : (act0Param.FusedClamp[j].Min * num5 + num7));
						actFoldParam.FusedClamp[j].Max = ((num16 < num10) ? ((Half)num16) : ((Half)num10));
						actFoldParam.FusedClamp[j].Min = ((num15 > num9) ? ((Half)num15) : ((Half)num9));
						if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
						{
							return null;
						}
					}
					else
					{
						actFoldParam.FusedClamp[j].Max = (Half)num10;
						actFoldParam.FusedClamp[j].Min = (Half)num9;
					}
				}
				else
				{
					float num17 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? act0Param.FusedClamp[j].Max : (act0Param.FusedClamp[j].Max * num5 + num7));
					float num18 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? act0Param.FusedClamp[j].Min : (act0Param.FusedClamp[j].Min * num5 + num7));
					actFoldParam.FusedClamp[j].Max = ((num17 < num10) ? ((Half)num17) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = ((num18 > num9) ? ((Half)num18) : ((Half)num9));
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
				continue;
			}
			float num19 = num4;
			if (!(num19 > 0f))
			{
				if (!(num19 < 0f))
				{
					if (num19 != 0f)
					{
						continue;
					}
					if (num5 > 0f)
					{
						float num20 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? act0Param.FusedClamp[j].Max : (act0Param.FusedClamp[j].Max * num5 + num7));
						actFoldParam.FusedClamp[j].Max = ((num20 < num10) ? ((Half)num20) : ((Half)num10));
						actFoldParam.FusedClamp[j].Min = (Half)num9;
						if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
						{
							return null;
						}
					}
					else if (num5 < 0f)
					{
						float num21 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? (0f - act0Param.FusedClamp[j].Max) : (act0Param.FusedClamp[j].Max * num5 + num7));
						actFoldParam.FusedClamp[j].Max = (Half)num10;
						actFoldParam.FusedClamp[j].Min = ((num21 > num9) ? ((Half)num21) : ((Half)num9));
						if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
						{
							return null;
						}
					}
					else if (num5 == 0f)
					{
						actFoldParam.FusedClamp[j].Max = (Half)num10;
						actFoldParam.FusedClamp[j].Min = (Half)num9;
					}
				}
				else if (num5 < 0f)
				{
					float num22 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? (0f - act0Param.FusedClamp[j].Max) : (act0Param.FusedClamp[j].Max * num5 + num7));
					float num23 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? (0f - act0Param.FusedClamp[j].Min) : (act0Param.FusedClamp[j].Min * num4 + num6));
					actFoldParam.FusedClamp[j].Max = ((num23 < num10) ? ((Half)num23) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = ((num22 > num9) ? ((Half)num22) : ((Half)num9));
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
				else if (num5 > 0f)
				{
					float num24 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? act0Param.FusedClamp[j].Max : (act0Param.FusedClamp[j].Max * num5 + num7));
					float num25 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? (0f - act0Param.FusedClamp[j].Min) : (act0Param.FusedClamp[j].Min * num4 + num6));
					float num26 = ((num24 > num25) ? num25 : num24);
					actFoldParam.FusedClamp[j].Max = ((num26 < num10) ? ((Half)num26) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = (Half)num9;
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
				else if (num5 == 0f)
				{
					float num27 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? (0f - act0Param.FusedClamp[j].Min) : (act0Param.FusedClamp[j].Min * num4 + num6));
					actFoldParam.FusedClamp[j].Max = ((num27 < num10) ? ((Half)num27) : ((Half)num10));
					actFoldParam.FusedClamp[j].Min = (Half)num9;
					if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
					{
						return null;
					}
				}
			}
			else if (num5 > 0f)
			{
				float num28 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? act0Param.FusedClamp[j].Max : (act0Param.FusedClamp[j].Max * num5 + num7));
				float num29 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? act0Param.FusedClamp[j].Min : (act0Param.FusedClamp[j].Min * num4 + num6));
				actFoldParam.FusedClamp[j].Max = ((num28 < num10) ? ((Half)num28) : ((Half)num10));
				actFoldParam.FusedClamp[j].Min = ((num29 > num9) ? ((Half)num29) : ((Half)num9));
				if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
				{
					return null;
				}
			}
			else if (num5 < 0f)
			{
				float num30 = (double.IsPositiveInfinity(act0Param.FusedClamp[j].Max) ? (0f - act0Param.FusedClamp[j].Max) : (act0Param.FusedClamp[j].Max * num5 + num7));
				float num31 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? act0Param.FusedClamp[j].Min : (act0Param.FusedClamp[j].Min * num4 + num6));
				float num32 = ((num30 > num31) ? num30 : num31);
				actFoldParam.FusedClamp[j].Max = (Half)num10;
				actFoldParam.FusedClamp[j].Min = ((num32 > num9) ? ((Half)num32) : ((Half)num9));
				if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
				{
					return null;
				}
			}
			else if (num5 == 0f)
			{
				float num33 = (double.IsNegativeInfinity(act0Param.FusedClamp[j].Min) ? act0Param.FusedClamp[j].Min : (act0Param.FusedClamp[j].Min * num4 + num6));
				actFoldParam.FusedClamp[j].Max = (Half)num10;
				actFoldParam.FusedClamp[j].Min = ((num33 > num9) ? ((Half)num33) : ((Half)num9));
				if (actFoldParam.FusedClamp[j].Max < actFoldParam.FusedClamp[j].Min)
				{
					return null;
				}
			}
		}
		for (int k = 0; k < channels; k++)
		{
			float num34 = ((num == 1) ? act1Param.Ks[0, 0] : act1Param.Ks[0, k]);
			float num35 = ((num == 1) ? act1Param.Ks[1, 0] : act1Param.Ks[1, k]);
			float num36 = ((num == 1) ? act1Param.Bs[0, 0] : act1Param.Bs[0, k]);
			float num37 = ((num == 1) ? act1Param.Bs[1, 0] : act1Param.Bs[1, k]);
			float num38 = ((num == 1) ? act1Param.Xs[0, 0] : act1Param.Xs[0, k]);
			if (num != 1)
			{
				_ = act1Param.FusedClamp[k].Min;
			}
			else
			{
				_ = act1Param.FusedClamp[0].Min;
			}
			if (num != 1)
			{
				_ = act1Param.FusedClamp[k].Max;
			}
			else
			{
				_ = act1Param.FusedClamp[0].Max;
			}
			float num19 = act0Param.Ks[0, k];
			if (!(num19 > 0f))
			{
				if (!(num19 < 0f))
				{
					if (num19 == 0f)
					{
						if (act0Param.Ks[1, k] > 0f)
						{
							if ((act0Param.Bs[0, k] >= num38 || act0Param.Bs[1, k] + act0Param.Xs[0, k] * act0Param.Ks[1, k] <= num38) && (num34 != num35 || num36 != num37))
							{
								return null;
							}
							actFoldParam.Ks[0, k] = 0f;
							actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num35;
							actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
							actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
							actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
							continue;
						}
						if (act0Param.Ks[1, k] < 0f)
						{
							if ((act0Param.Bs[0, k] <= num38 || act0Param.Bs[1, k] - act0Param.Xs[0, k] * act0Param.Ks[1, k] >= num38) && (num34 != num35 || num36 != num37))
							{
								return null;
							}
							actFoldParam.Ks[0, k] = 0f;
							actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num34;
							actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num35 + num37;
							actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num34 + num36;
							actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
							continue;
						}
						if (act0Param.Ks[1, k] == 0f)
						{
							if ((act0Param.Bs[0, k] >= num38 || act0Param.Bs[1, k] <= num38) && (num34 != num35 || num36 != num37))
							{
								return null;
							}
							actFoldParam.Ks[0, k] = 0f;
							actFoldParam.Ks[1, k] = 0f;
							actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
							actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
							actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
							continue;
						}
					}
				}
				else
				{
					if (act0Param.Ks[1, k] < 0f)
					{
						if (act0Param.Xs[0, k] != 0f && System.Math.Abs(num38 - (act0Param.Xs[0, k] * act0Param.Ks[0, k] + act0Param.Bs[0, k])) > float.Epsilon)
						{
							return null;
						}
						if ((act0Param.Bs[0, k] < num38 || act0Param.Bs[1, k] > num38) && (num34 != num35 || num36 != num37) && (act0Param.Ks[0, k] != act0Param.Ks[1, k] || act0Param.Bs[0, k] != act0Param.Bs[1, k]))
						{
							return null;
						}
						if (act0Param.Ks[0, k] == act0Param.Ks[1, k] && act0Param.Bs[0, k] == act0Param.Bs[1, k] && (num34 != num35 || num36 != num37))
						{
							actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num35;
							actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num34;
							actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num35 + num37;
							actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num34 + num36;
							actFoldParam.Xs[0, k] = (num38 - act0Param.Bs[0, k]) / act0Param.Ks[0, k];
						}
						else
						{
							actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num34;
							actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num35;
							actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
							actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
							actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
						}
						continue;
					}
					if (act0Param.Ks[1, k] == 0f)
					{
						if ((act0Param.Bs[0, k] - act0Param.Xs[0, k] * act0Param.Ks[0, k] <= num38 || act0Param.Bs[1, k] >= num38) && (num34 != num35 || num36 != num37))
						{
							return null;
						}
						actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num35;
						actFoldParam.Ks[1, k] = 0f;
						actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num35 + num37;
						actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num34 + num36;
						actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
						continue;
					}
				}
			}
			else
			{
				if (act0Param.Ks[1, k] > 0f)
				{
					if (act0Param.Xs[0, k] != 0f && System.Math.Abs(num38 - (act0Param.Xs[0, k] * act0Param.Ks[0, k] + act0Param.Bs[0, k])) > float.Epsilon)
					{
						return null;
					}
					if ((act0Param.Bs[0, k] > num38 || act0Param.Bs[1, k] < num38) && (num34 != num35 || num36 != num37) && (act0Param.Ks[0, k] != act0Param.Ks[1, k] || act0Param.Bs[0, k] != act0Param.Bs[1, k]))
					{
						return null;
					}
					if (act0Param.Ks[0, k] == act0Param.Ks[1, k] && act0Param.Bs[0, k] == act0Param.Bs[1, k] && (num34 != num35 || num36 != num37))
					{
						actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num34;
						actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num35;
						actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
						actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
						actFoldParam.Xs[0, k] = (num38 - act0Param.Bs[0, k]) / act0Param.Ks[0, k];
					}
					else
					{
						actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num34;
						actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num35;
						actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
						actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
						actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
					}
					continue;
				}
				if (act0Param.Ks[1, k] == 0f)
				{
					if ((act0Param.Bs[0, k] + act0Param.Xs[0, k] * act0Param.Ks[0, k] >= num38 || act0Param.Bs[1, k] <= num38) && (num34 != num35 || num36 != num37))
					{
						return null;
					}
					actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num34;
					actFoldParam.Ks[1, k] = 0f;
					actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
					actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
					actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
					continue;
				}
			}
			if (num34 != num35 || num36 != num37)
			{
				return null;
			}
			actFoldParam.Ks[0, k] = act0Param.Ks[0, k] * num34;
			actFoldParam.Ks[1, k] = act0Param.Ks[1, k] * num35;
			actFoldParam.Bs[0, k] = act0Param.Bs[0, k] * num34 + num36;
			actFoldParam.Bs[1, k] = act0Param.Bs[1, k] * num35 + num37;
			actFoldParam.Xs[0, k] = act0Param.Xs[0, k];
		}
		ActParam2 actParam = new ActParam2(channels);
		for (int l = 0; l < channels; l++)
		{
			actParam.Ks[0, l] = actFoldParam.Ks[0, l];
			actParam.Ks[1, l] = actFoldParam.Ks[1, l];
			actParam.Bs[0, l] = actFoldParam.Bs[0, l];
			actParam.Bs[1, l] = actFoldParam.Bs[1, l];
			actParam.Xs[0, l] = actFoldParam.Xs[0, l];
			actParam.FusedClamp[l].Min = (float)actFoldParam.FusedClamp[l].Min;
			actParam.FusedClamp[l].Max = (float)actFoldParam.FusedClamp[l].Max;
		}
		actParam.Qp = act0Param.Qp;
		return actParam;
	}
}
