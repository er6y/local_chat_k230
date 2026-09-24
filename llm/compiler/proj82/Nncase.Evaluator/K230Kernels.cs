#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nncase.IR;
using Nncase.IR.Imaging;
using Nncase.IR.K230;
using Nncase.Passes.Rules.K230;
using Nncase.PatternMatch;
using OrtKISharp;

namespace Nncase.Evaluator;

public static class K230Kernels
{
	public static float ApplyAct0(float value, ReadOnlySpan<Half> actData, int channel, sbyte shiftBits)
	{
		int num = 7 * channel;
		return ApplyActivation(ApplyGnneActivation(value, shiftBits, (float)actData[num + 6], (float)actData[num], (float)actData[num + 2], (float)actData[num + 1], (float)actData[num + 3]), (Min: (float)actData[num + 4], Max: (float)actData[num + 5]));
	}

	public static float ApplyAct01(float value, ReadOnlySpan<float> actData, int channel, sbyte shiftBits)
	{
		int num = 7 * channel;
		return ApplyActivation(ApplyGnneActivation(value, shiftBits, actData[num + 6], actData[num], actData[num + 2], actData[num + 1], actData[num + 3]), (Min: actData[num + 4], Max: actData[num + 5]));
	}

	public static float FakeApplyAct0(float value, float[] actData, int channel, sbyte shiftBits)
	{
		int num = 7 * channel;
		float result = value * actData[num] / (float)(1L << (int)shiftBits) + ((value.CompareTo(actData[num + 6]) < 0) ? actData[num + 2] : actData[num + 3]);
		ValueRange<float> valueRange = (Min: actData[num + 4], Max: actData[num + 5]);
		float min = valueRange.Min;
		float max = valueRange.Max;
		if (result.CompareTo(max) <= 0)
		{
			if (result.CompareTo(min) <= 0)
			{
				return min;
			}
			if (result.CompareTo(max) <= 0)
			{
				return result;
			}
			return max;
		}
		return max;
	}

	public static int GetWindowedOutputSize(int size, int filter, int stride, int dilation, bool same, bool ceilMode = false)
	{
		int num = (filter - 1) * dilation + 1;
		if (same)
		{
			return (size + stride - 1) / stride;
		}
		if (!ceilMode)
		{
			return (size - num + stride) / stride;
		}
		return (int)((float)(size - num + stride) / (float)stride);
	}

	public static float ApplyMultiSegmentsAct1(int n, float value, ReadOnlySpan<Half> actData)
	{
		for (int i = 0; i < n - 1; i++)
		{
			if (value < (float)actData[i])
			{
				return value * (float)actData[n - 1 + i] + (float)actData[n - 1 + n + i];
			}
		}
		return value * (float)actData[n - 1 + n - 1] + (float)actData[n - 1 + n + n - 1];
	}

	public static float ApplyMultiSegmentsAct1(int n, float value, ReadOnlySpan<float> actData)
	{
		for (int i = 0; i < n - 1; i++)
		{
			if (value < actData[i])
			{
				return value * actData[(n - 1 + i) % actData.Length] + actData[(n - 1 + n + i) % actData.Length];
			}
		}
		return value * actData[n - 1 + n - 1] + actData[n - 1 + n + n - 1];
	}

	public static Const FakeGnneActivation(bool is16Segments, Tensor inputa, Tensor inputb, bool hasUninitailizedInput, Shape outputShape, ReadOnlySpan<float> actData, int outChannels, GnneActivationType actType)
	{
		GNNEShape gNNEShape = new GNNEShape(inputa.Shape.ToValueArray());
		GNNEShape gNNEShape2 = new GNNEShape(inputb.Shape.ToValueArray());
		float[] array = inputa.ToArray<float>();
		float[] array2 = inputb.ToArray<float>();
		float[] array3 = new float[ComputeSize(outputShape)];
		int[] array4 = new int[outputShape.Count];
		for (int i = 0; i < outputShape.Count; i++)
		{
			array4[i] = outputShape[i].FixedValue;
		}
		GNNEShape gNNEShape3 = new GNNEShape(array4);
		outChannels = gNNEShape3[1];
		if (hasUninitailizedInput)
		{
			if (inputa.Shape.ToValueArray() == outputShape)
			{
				int num = gNNEShape[2] * gNNEShape[3];
				int num2 = gNNEShape[1] * num;
				for (int j = 0; j < gNNEShape[0]; j++)
				{
					int num3 = j * num2;
					int num4 = j * outChannels * num;
					for (int k = 0; k < outChannels; k++)
					{
						int num5 = num3 + k * num;
						int num6 = num4 + k * num;
						for (int l = 0; l < num; l++)
						{
							float num7 = ApplyAct1(array[num5], actData, k, is16Segments);
							array3[num6] = num7;
							num5++;
							num6++;
						}
					}
				}
			}
			else
			{
				int num8 = gNNEShape[2] * gNNEShape[3];
				int num9 = gNNEShape3[2] * gNNEShape3[3];
				int num10 = gNNEShape[1] * num8;
				for (int m = 0; m < gNNEShape[0]; m++)
				{
					int num11 = m * num10;
					int num12 = m * outChannels * num9;
					for (int n = 0; n < outChannels; n++)
					{
						int num13 = num11 + n * num8;
						int num14 = num12 + n * num9;
						for (int num15 = 0; num15 < gNNEShape3[2]; num15++)
						{
							for (int num16 = 0; num16 < gNNEShape3[3]; num16++)
							{
								float num17 = ApplyAct1(array[num13 + num15 * gNNEShape[2] / gNNEShape3[2] * gNNEShape[3] + num16 * gNNEShape[3] / gNNEShape3[3]], actData, n, is16Segments);
								array3[num14 + num15 * gNNEShape3[3] + num16] = num17;
							}
						}
					}
				}
			}
		}
		else if (inputa.Shape.ToValueArray().SequenceEqual(inputb.Shape.ToValueArray()))
		{
			int num18 = gNNEShape[2] * gNNEShape[3];
			int num19 = gNNEShape[1] * num18;
			for (int num20 = 0; num20 < gNNEShape[0]; num20++)
			{
				int num21 = num20 * num19;
				int num22 = num20 * num19;
				int num23 = num20 * outChannels * num18;
				for (int num24 = 0; num24 < outChannels; num24++)
				{
					int num25 = num21 + num24 * num18;
					int num26 = num22 + num24 * num18;
					int num27 = num23 + num24 * num18;
					for (int num28 = 0; num28 < num18; num28++)
					{
						float value = ((actType != GnneActivationType.Mul) ? (array[num25] + array2[num26]) : (array[num25] * array2[num26]));
						float num29 = ApplyAct1(value, actData, num24, is16Segments);
						array3[num27] = num29;
						num25++;
						num26++;
						num27++;
					}
				}
			}
		}
		else
		{
			int num30 = gNNEShape3[2] * gNNEShape3[3];
			for (int num31 = 0; num31 < gNNEShape3[0]; num31++)
			{
				int num32 = num31 * outChannels * num30;
				for (int num33 = 0; num33 < outChannels; num33++)
				{
					int num34 = num32 + num33 * num30;
					for (int num35 = 0; num35 < gNNEShape3[2]; num35++)
					{
						for (int num36 = 0; num36 < gNNEShape3[3]; num36++)
						{
							int num37 = ((gNNEShape[0] != 1) ? (num31 * gNNEShape[1] * gNNEShape[2] * gNNEShape[3]) : 0);
							int num38 = ((gNNEShape[1] != 1) ? (num33 * gNNEShape[2] * gNNEShape[3]) : 0);
							int num39 = ((gNNEShape[2] != 1) ? (num35 * gNNEShape[3]) : 0);
							int num40 = ((gNNEShape[3] != 1) ? num36 : 0);
							int num41 = ((gNNEShape2[0] != 1) ? (num31 * gNNEShape2[1] * gNNEShape2[2] * gNNEShape2[3]) : 0);
							int num42 = ((gNNEShape2[1] != 1) ? (num33 * gNNEShape2[2] * gNNEShape2[3]) : 0);
							int num43 = ((gNNEShape2[2] != 1) ? (num35 * gNNEShape2[3]) : 0);
							int num44 = ((gNNEShape2[3] != 1) ? num36 : 0);
							float num45 = inputa.ToArray<float>()[num37 + num38 + num39 + num40];
							float num46 = inputb.ToArray<float>()[num41 + num42 + num43 + num44];
							float value2 = ((actType != GnneActivationType.Mul) ? (num45 + num46) : (num45 * num46));
							float num47 = ApplyAct1(value2, actData, num33, is16Segments);
							array3[num34] = num47;
							num34++;
						}
					}
				}
			}
		}
		return Const.FromTensor(Tensor.From(array3, outputShape));
	}

	public static Const Gnne_activation(bool is16Segments, Tensor inputa, Tensor inputb, bool has_uninitailized_input, ReadOnlySpan<float> actData, int outChannels, DeQuantizeParam deQuantizeParamA, DeQuantizeParam deQuantizeParamB, GnneActivationType actType, TensorType outputType, Shape outputShape)
	{
		int[] array = inputa.Shape.ToValueArray();
		float[] array2 = inputa.ToArray<float>();
		if (has_uninitailized_input)
		{
			if (array == outputShape)
			{
				int num = array[2] * array[3];
				int num2 = array[1] * num;
				if (outputType.DType == DataTypes.Int8)
				{
					sbyte[] array3 = new sbyte[ComputeSize(outputShape)];
					for (int i = 0; i < array[0]; i++)
					{
						int num3 = i * num2;
						int num4 = i * outChannels * num;
						for (int j = 0; j < outChannels; j++)
						{
							int num5 = num3 + j * num;
							int num6 = num4 + j * num;
							for (int k = 0; k < num; k++)
							{
								sbyte b = (sbyte)ApplyAct1((array2[num5] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, j, is16Segments);
								array3[num6] = b;
								num5++;
								num6++;
							}
						}
					}
					return Const.FromTensor(Tensor.From(array3, inputa.Shape));
				}
				if (outputType.DType == DataTypes.Float16)
				{
					Half[] array4 = new Half[ComputeSize(outputShape)];
					for (int l = 0; l < array[0]; l++)
					{
						int num7 = l * num2;
						int num8 = l * outChannels * num;
						for (int m = 0; m < outChannels; m++)
						{
							int num9 = num7 + m * num;
							int num10 = num8 + m * num;
							for (int n = 0; n < num; n++)
							{
								Half half = (Half)ApplyAct1((array2[num9] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, m, is16Segments);
								array4[num10] = half;
								num9++;
								num10++;
							}
						}
					}
					return Const.FromTensor(Tensor.From(array4, inputa.Shape));
				}
				if (outputType.DType == DataTypes.UInt8)
				{
					byte[] array5 = new byte[ComputeSize(outputShape)];
					for (int num11 = 0; num11 < array[0]; num11++)
					{
						int num12 = num11 * num2;
						int num13 = num11 * outChannels * num;
						for (int num14 = 0; num14 < outChannels; num14++)
						{
							int num15 = num12 + num14 * num;
							int num16 = num13 + num14 * num;
							for (int num17 = 0; num17 < num; num17++)
							{
								byte b2 = (byte)ApplyAct1((array2[num15] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num14, is16Segments);
								array5[num16] = b2;
								num15++;
								num16++;
							}
						}
					}
					return Const.FromTensor(Tensor.From(array5, inputa.Shape));
				}
				short[] array6 = new short[ComputeSize(outputShape)];
				for (int num18 = 0; num18 < array[0]; num18++)
				{
					int num19 = num18 * num2;
					int num20 = num18 * outChannels * num;
					for (int num21 = 0; num21 < outChannels; num21++)
					{
						int num22 = num19 + num21 * num;
						int num23 = num20 + num21 * num;
						for (int num24 = 0; num24 < num; num24++)
						{
							short num25 = (short)ApplyAct1((array2[num22] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num21, is16Segments);
							array6[num23] = num25;
							num22++;
							num23++;
						}
					}
				}
				return Const.FromTensor(Tensor.From(array6, inputa.Shape));
			}
			int num26 = array[2] * array[3];
			int num27 = outputShape[2].FixedValue * outputShape[3].FixedValue;
			int num28 = array[1] * num26;
			if (outputType.DType == DataTypes.Int8)
			{
				sbyte[] array7 = new sbyte[ComputeSize(outputShape)];
				for (int num29 = 0; num29 < array[0]; num29++)
				{
					int num30 = num29 * num28;
					int num31 = num29 * outChannels * num27;
					for (int num32 = 0; num32 < outChannels; num32++)
					{
						int num33 = num30 + num32 * num26;
						int num34 = num31 + num32 * num27;
						for (int num35 = 0; num35 < outputShape[2].FixedValue; num35++)
						{
							for (int num36 = 0; num36 < outputShape[3].FixedValue; num36++)
							{
								sbyte b3 = (sbyte)ApplyAct1((array2[num33 + num35 * array[2] / outputShape[2].FixedValue * array[3] + num36 * array[3] / outputShape[3].FixedValue] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num32, is16Segments);
								array7[num34 + num35 * outputShape[3].FixedValue + num36] = b3;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array7, inputa.Shape));
			}
			if (outputType.DType == DataTypes.Float16)
			{
				Half[] array8 = new Half[ComputeSize(outputShape)];
				for (int num37 = 0; num37 < array[0]; num37++)
				{
					int num38 = num37 * num28;
					int num39 = num37 * outChannels * num27;
					for (int num40 = 0; num40 < outChannels; num40++)
					{
						int num41 = num38 + num40 * num26;
						int num42 = num39 + num40 * num27;
						for (int num43 = 0; num43 < outputShape[2].FixedValue; num43++)
						{
							for (int num44 = 0; num44 < outputShape[3].FixedValue; num44++)
							{
								Half half2 = (Half)ApplyAct1((array2[num41 + num43 * array[2] / outputShape[2].FixedValue * array[3] + num44 * array[3] / outputShape[3].FixedValue] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num40, is16Segments);
								array8[num42 + num43 * outputShape[3].FixedValue + num44] = half2;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array8, inputa.Shape));
			}
			if (outputType.DType == DataTypes.UInt8)
			{
				byte[] array9 = new byte[ComputeSize(outputShape)];
				for (int num45 = 0; num45 < array[0]; num45++)
				{
					int num46 = num45 * num28;
					int num47 = num45 * outChannels * num27;
					for (int num48 = 0; num48 < outChannels; num48++)
					{
						int num49 = num46 + num48 * num26;
						int num50 = num47 + num48 * num27;
						for (int num51 = 0; num51 < outputShape[2].FixedValue; num51++)
						{
							for (int num52 = 0; num52 < outputShape[3].FixedValue; num52++)
							{
								byte b4 = (byte)ApplyAct1((array2[num49 + num51 * array[2] / outputShape[2].FixedValue * array[3] + num52 * array[3] / outputShape[3].FixedValue] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num48, is16Segments);
								array9[num50 + num51 * outputShape[3].FixedValue + num52] = b4;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array9, inputa.Shape));
			}
			short[] array10 = new short[ComputeSize(outputShape)];
			for (int num53 = 0; num53 < array[0]; num53++)
			{
				int num54 = num53 * num28;
				int num55 = num53 * outChannels * num27;
				for (int num56 = 0; num56 < outChannels; num56++)
				{
					int num57 = num54 + num56 * num26;
					int num58 = num55 + num56 * num27;
					for (int num59 = 0; num59 < outputShape[2].FixedValue; num59++)
					{
						for (int num60 = 0; num60 < outputShape[3].FixedValue; num60++)
						{
							short num61 = (short)ApplyAct1((array2[num57 + num59 * array[2] / outputShape[2].FixedValue * array[3] + num60 * array[3] / outputShape[3].FixedValue] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale, actData, num56, is16Segments);
							array10[num58 + num59 * outputShape[3].FixedValue + num60] = num61;
						}
					}
				}
			}
			return Const.FromTensor(Tensor.From(array10, inputa.Shape));
		}
		int[] array11 = inputb.Shape.ToValueArray();
		float[] array12 = inputb.ToArray<float>();
		if (array == array11)
		{
			int num62 = array[2] * array[3];
			int num63 = array[1] * num62;
			if (outputType.DType == DataTypes.Int8)
			{
				sbyte[] array13 = new sbyte[ComputeSize(outputShape)];
				for (int num64 = 0; num64 < array[0]; num64++)
				{
					int num65 = num64 * num63;
					int num66 = num64 * num63;
					int num67 = num64 * outChannels * num62;
					for (int num68 = 0; num68 < outChannels; num68++)
					{
						int num69 = num65 + num68 * num62;
						int num70 = num66 + num68 * num62;
						int num71 = num67 + num68 * num62;
						for (int num72 = 0; num72 < num62; num72++)
						{
							float value = ((actType != GnneActivationType.Mul) ? ((array2[num69] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (array12[num70] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((array2[num69] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (array12[num70] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
							sbyte b5 = (sbyte)ApplyAct1(value, actData, num68, is16Segments);
							array13[num71] = b5;
							num69++;
							num70++;
							num71++;
						}
					}
				}
				return Const.FromTensor(Tensor.From(array13, inputa.Shape));
			}
			if (outputType.DType == DataTypes.UInt8)
			{
				byte[] array14 = new byte[ComputeSize(outputShape)];
				for (int num73 = 0; num73 < array[0]; num73++)
				{
					int num74 = num73 * num63;
					int num75 = num73 * num63;
					int num76 = num73 * outChannels * num62;
					for (int num77 = 0; num77 < outChannels; num77++)
					{
						int num78 = num74 + num77 * num62;
						int num79 = num75 + num77 * num62;
						int num80 = num76 + num77 * num62;
						for (int num81 = 0; num81 < num62; num81++)
						{
							float value2 = ((actType != GnneActivationType.Mul) ? ((array2[num78] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (array12[num79] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((array2[num78] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (array12[num79] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
							byte b6 = (byte)ApplyAct1(value2, actData, num77, is16Segments);
							array14[num80] = b6;
							num78++;
							num79++;
							num80++;
						}
					}
				}
				return Const.FromTensor(Tensor.From(array14, inputa.Shape));
			}
			if (outputType.DType == DataTypes.Float16)
			{
				Half[] array15 = new Half[ComputeSize(outputShape)];
				for (int num82 = 0; num82 < array[0]; num82++)
				{
					int num83 = num82 * num63;
					int num84 = num82 * num63;
					int num85 = num82 * outChannels * num62;
					for (int num86 = 0; num86 < outChannels; num86++)
					{
						int num87 = num83 + num86 * num62;
						int num88 = num84 + num86 * num62;
						int num89 = num85 + num86 * num62;
						for (int num90 = 0; num90 < num62; num90++)
						{
							float value3 = ((actType != GnneActivationType.Mul) ? ((array2[num87] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (array12[num88] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((array2[num87] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (array12[num88] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
							Half half3 = (Half)ApplyAct1(value3, actData, num86, is16Segments);
							array15[num89] = half3;
							num87++;
							num88++;
							num89++;
						}
					}
				}
				return Const.FromTensor(Tensor.From(array15, inputa.Shape));
			}
			if (outputType.DType == DataTypes.Int16)
			{
				short[] array16 = new short[ComputeSize(outputShape)];
				for (int num91 = 0; num91 < array[0]; num91++)
				{
					int num92 = num91 * num63;
					int num93 = num91 * num63;
					int num94 = num91 * outChannels * num62;
					for (int num95 = 0; num95 < outChannels; num95++)
					{
						int num96 = num92 + num95 * num62;
						int num97 = num93 + num95 * num62;
						int num98 = num94 + num95 * num62;
						for (int num99 = 0; num99 < num62; num99++)
						{
							float value4 = ((actType != GnneActivationType.Mul) ? ((array2[num96] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (array12[num97] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((array2[num96] - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (array12[num97] - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
							short num100 = (short)ApplyAct1(value4, actData, num95, is16Segments);
							array16[num98] = num100;
							num96++;
							num97++;
							num98++;
						}
					}
				}
				return Const.FromTensor(Tensor.From(array16, inputa.Shape));
			}
		}
		else
		{
			int num101 = outputShape[2].FixedValue * outputShape[3].FixedValue;
			if (outputType.DType == DataTypes.Int8)
			{
				sbyte[] array17 = new sbyte[ComputeSize(outputShape)];
				for (int num102 = 0; num102 < outputShape[0].FixedValue; num102++)
				{
					int num103 = num102 * outChannels * num101;
					for (int num104 = 0; num104 < outChannels; num104++)
					{
						int num105 = num103 + num104 * num101;
						for (int num106 = 0; num106 < outputShape[2].FixedValue; num106++)
						{
							for (int num107 = 0; num107 < outputShape[3].FixedValue; num107++)
							{
								int num108 = ((array[0] != 1) ? (num102 * array[1] * array[2] * array[3]) : 0);
								int num109 = ((array[1] != 1) ? (num104 * array[2] * array[3]) : 0);
								int num110 = ((array[2] != 1) ? (num106 * array[3]) : 0);
								int num111 = ((array[3] != 1) ? num107 : 0);
								int num112 = ((array11[0] != 1) ? (num102 * array11[1] * array11[2] * array11[3]) : 0);
								int num113 = ((array11[1] != 1) ? (num104 * array11[2] * array11[3]) : 0);
								int num114 = ((array11[2] != 1) ? (num106 * array11[3]) : 0);
								int num115 = ((array11[3] != 1) ? num107 : 0);
								float num116 = inputa.ToArray<float>()[num108 + num109 + num110 + num111];
								float num117 = inputb.ToArray<float>()[num112 + num113 + num114 + num115];
								float value5 = ((actType != GnneActivationType.Mul) ? ((num116 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (num117 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((num116 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (num117 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
								sbyte b7 = (sbyte)ApplyAct1(value5, actData, num104, is16Segments);
								array17[num105] = b7;
								num105++;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array17, inputa.Shape));
			}
			if (outputType.DType == DataTypes.Float16)
			{
				Half[] array18 = new Half[ComputeSize(outputShape)];
				for (int num118 = 0; num118 < outputShape[0].FixedValue; num118++)
				{
					int num119 = num118 * outChannels * num101;
					for (int num120 = 0; num120 < outChannels; num120++)
					{
						int num121 = num119 + num120 * num101;
						for (int num122 = 0; num122 < outputShape[2].FixedValue; num122++)
						{
							for (int num123 = 0; num123 < outputShape[3].FixedValue; num123++)
							{
								int num124 = ((array[0] != 1) ? (num118 * array[1] * array[2] * array[3]) : 0);
								int num125 = ((array[1] != 1) ? (num120 * array[2] * array[3]) : 0);
								int num126 = ((array[2] != 1) ? (num122 * array[3]) : 0);
								int num127 = ((array[3] != 1) ? num123 : 0);
								int num128 = ((array11[0] != 1) ? (num118 * array11[1] * array11[2] * array11[3]) : 0);
								int num129 = ((array11[1] != 1) ? (num120 * array11[2] * array11[3]) : 0);
								int num130 = ((array11[2] != 1) ? (num122 * array11[3]) : 0);
								int num131 = ((array11[3] != 1) ? num123 : 0);
								float num132 = inputa.ToArray<float>()[num124 + num125 + num126 + num127];
								float num133 = inputb.ToArray<float>()[num128 + num129 + num130 + num131];
								float value6 = ((actType != GnneActivationType.Mul) ? ((num132 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (num133 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((num132 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (num133 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
								Half half4 = (Half)ApplyAct1(value6, actData, num120, is16Segments);
								array18[num121] = half4;
								num121++;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array18, inputa.Shape));
			}
			if (outputType.DType == DataTypes.UInt8)
			{
				byte[] array19 = new byte[ComputeSize(outputShape)];
				for (int num134 = 0; num134 < outputShape[0].FixedValue; num134++)
				{
					int num135 = num134 * outChannels * num101;
					for (int num136 = 0; num136 < outChannels; num136++)
					{
						int num137 = num135 + num136 * num101;
						for (int num138 = 0; num138 < outputShape[2].FixedValue; num138++)
						{
							for (int num139 = 0; num139 < outputShape[3].FixedValue; num139++)
							{
								int num140 = ((array[0] != 1) ? (num134 * array[1] * array[2] * array[3]) : 0);
								int num141 = ((array[1] != 1) ? (num136 * array[2] * array[3]) : 0);
								int num142 = ((array[2] != 1) ? (num138 * array[3]) : 0);
								int num143 = ((array[3] != 1) ? num139 : 0);
								int num144 = ((array11[0] != 1) ? (num134 * array11[1] * array11[2] * array11[3]) : 0);
								int num145 = ((array11[1] != 1) ? (num136 * array11[2] * array11[3]) : 0);
								int num146 = ((array11[2] != 1) ? (num138 * array11[3]) : 0);
								int num147 = ((array11[3] != 1) ? num139 : 0);
								float num148 = inputa.ToArray<float>()[num140 + num141 + num142 + num143];
								float num149 = inputb.ToArray<float>()[num144 + num145 + num146 + num147];
								float value7 = ((actType != GnneActivationType.Mul) ? ((num148 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (num149 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((num148 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (num149 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
								byte b8 = (byte)ApplyAct1(value7, actData, num136, is16Segments);
								array19[num137] = b8;
								num137++;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array19, inputa.Shape));
			}
			if (outputType.DType == DataTypes.Int16)
			{
				short[] array20 = new short[ComputeSize(outputShape)];
				for (int num150 = 0; num150 < outputShape[0].FixedValue; num150++)
				{
					int num151 = num150 * outChannels * num101;
					for (int num152 = 0; num152 < outChannels; num152++)
					{
						int num153 = num151 + num152 * num101;
						for (int num154 = 0; num154 < outputShape[2].FixedValue; num154++)
						{
							for (int num155 = 0; num155 < outputShape[3].FixedValue; num155++)
							{
								int num156 = ((array[0] != 1) ? (num150 * array[1] * array[2] * array[3]) : 0);
								int num157 = ((array[1] != 1) ? (num152 * array[2] * array[3]) : 0);
								int num158 = ((array[2] != 1) ? (num154 * array[3]) : 0);
								int num159 = ((array[3] != 1) ? num155 : 0);
								int num160 = ((array11[0] != 1) ? (num150 * array11[1] * array11[2] * array11[3]) : 0);
								int num161 = ((array11[1] != 1) ? (num152 * array11[2] * array11[3]) : 0);
								int num162 = ((array11[2] != 1) ? (num154 * array11[3]) : 0);
								int num163 = ((array11[3] != 1) ? num155 : 0);
								float num164 = inputa.ToArray<float>()[num156 + num157 + num158 + num159];
								float num165 = inputb.ToArray<float>()[num160 + num161 + num162 + num163];
								float value8 = ((actType != GnneActivationType.Mul) ? ((num164 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale + (num165 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale) : ((num164 - (float)deQuantizeParamA.ZeroPoint) * deQuantizeParamA.Scale * (num165 - (float)deQuantizeParamB.ZeroPoint) * deQuantizeParamB.Scale));
								short num166 = (short)ApplyAct1(value8, actData, num152, is16Segments);
								array20[num153] = num166;
								num153++;
							}
						}
					}
				}
				return Const.FromTensor(Tensor.From(array20, inputa.Shape));
			}
		}
		return null;
	}

	public static float ApplyActivation(float value, float[] clamp)
	{
		return System.Math.Clamp(value, clamp[0], clamp[1]);
	}

	public static float Tanh(float x, Tensor<Half> segFittingParamGt)
	{
		for (int i = 0; i < 15; i++)
		{
			if ((Half)x < segFittingParamGt.ToArray()[i])
			{
				return (float)segFittingParamGt.ToArray()[15 + i] * x + (float)segFittingParamGt.ToArray()[31 + i];
			}
		}
		return (float)segFittingParamGt.ToArray()[30] * x + (float)segFittingParamGt.ToArray()[46];
	}

	public static float Sigmoid(float x, Tensor<Half> segFittingParamFt)
	{
		for (int i = 0; i < 15; i++)
		{
			if ((Half)x < segFittingParamFt.ToArray()[i])
			{
				return (float)segFittingParamFt.ToArray()[15 + i] * x + (float)segFittingParamFt.ToArray()[31 + i];
			}
		}
		return (float)segFittingParamFt.ToArray()[30] * x + (float)segFittingParamFt.ToArray()[46];
	}

	public static Tensor<float>[] FakeGnneLstm(Tensor<float> input, Tensor<float> wXc, Tensor<Half> actXc, Tensor<float> wRc, Tensor<Half> actRc, Tensor<float> initH, Tensor<float> initC, Tensor<Half> segFittingParamFt, Tensor<Half> segFittingParamGt, Tensor<float> output, Tensor<float> outputH, Tensor<float> outputC, LSTMDirection direction, int outputSize)
	{
		float[] array = new float[ComputeSize(initH.Shape)];
		float[] array2 = initH.ToArray();
		float[] array3 = initC.ToArray();
		float[] array4 = output.ToArray();
		float[] array5 = outputH.ToArray();
		float[] array6 = outputC.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array2[i];
		}
		float[] array7 = new float[ComputeSize(initC.Shape)];
		for (int j = 0; j < array7.Length; j++)
		{
			array7[j] = array3[j];
		}
		List<int> list = new List<int>();
		for (int k = 0; k < input.Shape[1].FixedValue; k++)
		{
			list.Add(k);
		}
		if (direction == LSTMDirection.Reverse)
		{
			list.Reverse();
		}
		for (int l = 0; l < output.Shape[1].FixedValue; l++)
		{
			if (l == 1)
			{
				list.Reverse();
			}
			for (int m = 0; m < input.Shape[2].FixedValue; m++)
			{
				for (int n = 0; n < list.Count; n++)
				{
					float[] array8 = new float[output.Shape[3].FixedValue * 4];
					float[] array9 = new float[output.Shape[3].FixedValue * 4];
					for (int num = 0; num < array8.Length; num++)
					{
						float[] array10 = new float[input.Shape[3].FixedValue];
						float[] array11 = new float[input.Shape[3].FixedValue];
						Array.Copy(input.ToArray(), m * input.Shape[3].FixedValue + list[n] * input.Shape[2].FixedValue * input.Shape[3].FixedValue, array10, 0, input.Shape[3].FixedValue);
						Array.Copy(wXc.ToArray(), num * wXc.Shape[3].FixedValue + l * wXc.Shape[2].FixedValue * wXc.Shape[3].FixedValue, array11, 0, input.Shape[3].FixedValue);
						array8[num] = OrtKI.MatMul(array10, array11).ToArray<float>()[0];
						int num2 = l * wXc.Shape[2].FixedValue;
						array8[num] = ApplyAct0(array8[num], actXc.ToArray<Half>(), num2 + num, 0);
						float[] array12 = new float[input.Shape[3].FixedValue];
						float[] array13 = new float[input.Shape[3].FixedValue];
						Array.Copy(array.ToArray(), m * output.Shape[3].FixedValue + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue, array12, 0, output.Shape[3].FixedValue);
						Array.Copy(wRc.ToArray(), num * wRc.Shape[3].FixedValue + l * wRc.Shape[2].FixedValue * wRc.Shape[3].FixedValue, array13, 0, output.Shape[3].FixedValue);
						array9[num] = OrtKI.MatMul(array12, array13).ToArray<float>()[0];
						num2 = l * wRc.Shape[2].FixedValue;
						array9[num] = ApplyAct0(array9[num], actRc.ToArray<Half>(), num2 + num, 0);
						array8[num] += array9[num];
					}
					for (int num3 = 0; num3 < output.Shape[3].FixedValue; num3++)
					{
						array8[num3 + output.Shape[3].FixedValue * 2] = Sigmoid(array8[num3 + output.Shape[3].FixedValue * 2], segFittingParamFt);
					}
					for (int num4 = 0; num4 < output.Shape[3].FixedValue; num4++)
					{
						array8[num4 + output.Shape[3].FixedValue * 2] *= array7[num4 + m * output.Shape[3].FixedValue + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue];
					}
					for (int num5 = 0; num5 < output.Shape[3].FixedValue; num5++)
					{
						int num6 = num5;
						_ = output.Shape[3].FixedValue;
						int num7 = num6 + 0;
						int num8 = num5;
						_ = output.Shape[3].FixedValue;
						array8[num7] = Sigmoid(array8[num8 + 0], segFittingParamFt);
					}
					for (int num9 = 0; num9 < output.Shape[3].FixedValue; num9++)
					{
						array8[num9 + output.Shape[3].FixedValue * 3] = Tanh(array8[num9 + output.Shape[3].FixedValue * 3], segFittingParamGt);
					}
					for (int num10 = 0; num10 < output.Shape[3].FixedValue; num10++)
					{
						int num11 = num10;
						_ = output.Shape[3].FixedValue;
						array8[num11 + 0] *= array8[num10 + output.Shape[3].FixedValue * 3];
					}
					for (int num12 = 0; num12 < output.Shape[3].FixedValue; num12++)
					{
						int num13 = num12 + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue;
						float num14 = array8[num12 + output.Shape[3].FixedValue * 2];
						int num15 = num12;
						_ = output.Shape[3].FixedValue;
						array7[num13] = num14 + array8[num15 + 0];
					}
					for (int num16 = 0; num16 < output.Shape[3].FixedValue; num16++)
					{
						array8[num16 + output.Shape[3].FixedValue] = Sigmoid(array8[num16 + output.Shape[3].FixedValue], segFittingParamFt);
					}
					for (int num17 = 0; num17 < output.Shape[3].FixedValue; num17++)
					{
						array8[num17 + output.Shape[3].FixedValue * 3] = Tanh(array7[num17 + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue], segFittingParamGt);
					}
					for (int num18 = 0; num18 < output.Shape[3].FixedValue; num18++)
					{
						array[num18 + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue] = array8[num18 + output.Shape[3].FixedValue * 3] * array8[num18 + output.Shape[3].FixedValue];
					}
					for (int num19 = 0; num19 < output.Shape[3].FixedValue; num19++)
					{
						array4[m * output.Shape[3].FixedValue + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[n] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num19] = array[l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num19];
					}
					if (n == list.Count - 1)
					{
						for (int num20 = 0; num20 < output.Shape[3].FixedValue; num20++)
						{
							array5[m * output.Shape[3].FixedValue + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num20] = array[l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num20];
						}
						for (int num21 = 0; num21 < output.Shape[3].FixedValue; num21++)
						{
							array6[m * output.Shape[3].FixedValue + l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num21] = array7[l * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num21];
						}
					}
				}
			}
		}
		output = Tensor.From(array4, output.Shape);
		outputC = Tensor.From(array6, outputC.Shape);
		outputH = Tensor.From(array5, outputH.Shape);
		return outputSize switch
		{
			1 => new Tensor<float>[1] { output }, 
			2 => new Tensor<float>[2] { output, outputH }, 
			3 => new Tensor<float>[3] { output, outputH, outputC }, 
			_ => null, 
		};
	}

	public static Tensor[] GnneLstmImpl(DataType dataTypeO, DataType dataTypeOH, Tensor<float> input, Tensor<float> wXc, Tensor<Half> actXc, Tensor<float> wRc, Tensor<Half> actRc0, Tensor<Half> actRc1, Tensor<float> initH, Tensor<float> initC, Tensor<Half> segFittingParamFt, Tensor<Half> segFittingParamGt, Tensor output, Tensor outputH, Tensor outputC, LSTMDirection direction, Tensor<Half> wXcQarg, Tensor<Half> wRcQarg, Tensor<Half> binAct, Tensor<Half> binQAct, int ifDeqBias, int hDeqBias0, int hDeqBias1, int xcShiftBits, int rcShiftBits0, int rcShiftBits1, int outputSize)
	{
		Tensor<Half> wXcQarg2 = wXcQarg;
		Tensor<Half> wRcQarg2 = wRcQarg;
		float[] array = new float[ComputeSize(initH.Shape)];
		float[] array2 = initH.ToArray();
		float[] array3 = initC.ToArray();
		Half[] array4 = outputC.ToArray<Half>();
		Half[] array5 = new Half[ComputeSize(output.Shape)];
		sbyte[] array6 = new sbyte[ComputeSize(output.Shape)];
		byte[] array7 = new byte[ComputeSize(output.Shape)];
		short[] array8 = new short[ComputeSize(output.Shape)];
		Half[] array9 = new Half[ComputeSize(outputH.Shape)];
		sbyte[] array10 = new sbyte[ComputeSize(outputH.Shape)];
		byte[] array11 = new byte[ComputeSize(outputH.Shape)];
		short[] array12 = new short[ComputeSize(outputH.Shape)];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = array2[j];
		}
		float[] array13 = new float[ComputeSize(initC.Shape)];
		for (int k = 0; k < array13.Length; k++)
		{
			array13[k] = array3[k];
		}
		List<int> list = new List<int>();
		for (int m = 0; m < input.Shape[1].FixedValue; m++)
		{
			list.Add(m);
		}
		if (direction == LSTMDirection.Reverse)
		{
			list.Reverse();
		}
		for (int n = 0; n < output.Shape[1].FixedValue; n++)
		{
			if (n == 1)
			{
				list.Reverse();
			}
			for (int num = 0; num < input.Shape[2].FixedValue; num++)
			{
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					float[] array14 = new float[output.Shape[3].FixedValue * 4];
					float[] array15 = new float[output.Shape[3].FixedValue * 4];
					for (int num3 = 0; num3 < output.Shape[3].FixedValue * 4; num3++)
					{
						float[] inMul1 = new float[input.Shape[3].FixedValue];
						float[] inMul2 = new float[input.Shape[3].FixedValue];
						Array.Copy(input.ToArray(), num * input.Shape[3].FixedValue + list[num2] * input.Shape[2].FixedValue * input.Shape[3].FixedValue, inMul1, 0, input.Shape[3].FixedValue);
						Array.Copy(wXc.ToArray(), num3 * wXc.Shape[3].FixedValue + n * wXc.Shape[2].FixedValue * wXc.Shape[3].FixedValue, inMul2, 0, input.Shape[3].FixedValue);
						inMul1.Select((float _, int i) => inMul1[i] -= ifDeqBias);
						int o1 = num3;
						inMul2.Select((float _, int i) => inMul2[i] -= (float)wXcQarg2.ToArray()[o1]);
						array14[num3] = OrtKI.MatMul(inMul1, inMul2).ToArray<float>()[0];
						int num4 = n * wXc.Shape[2].FixedValue;
						array14[num3] = ApplyAct0(array14[num3] / (float)(1 << xcShiftBits), actXc.ToArray<Half>(), num4 + num3, 0);
						float[] inMul3 = new float[input.Shape[3].FixedValue];
						float[] inMul4 = new float[input.Shape[3].FixedValue];
						Array.Copy(array.ToArray(), num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue, inMul3, 0, output.Shape[3].FixedValue);
						Array.Copy(wRc.ToArray(), num3 * wRc.Shape[3].FixedValue + n * wRc.Shape[2].FixedValue * wRc.Shape[3].FixedValue, inMul4, 0, output.Shape[3].FixedValue);
						int l1 = num2;
						inMul3.Select((float _, int i) => inMul3[i] -= ((l1 == 0) ? hDeqBias0 : hDeqBias1));
						int o2 = num3;
						inMul4.Select((float _, int i) => inMul4[i] -= (float)wRcQarg2.ToArray()[o2]);
						array15[num3] = OrtKI.MatMul(inMul3, inMul4).ToArray<float>()[0];
						int num5 = ((list[num2] == list[0]) ? rcShiftBits0 : rcShiftBits1);
						num4 = n * wRc.Shape[2].FixedValue;
						array15[num3] = ApplyAct0(array15[num3] / (float)(1 << num5), (list[num2] == list[0]) ? actRc0.ToArray<Half>() : actRc1.ToArray<Half>(), num4 + num3, 0);
						array14[num3] += array15[num3];
					}
					for (int num6 = 0; num6 < output.Shape[3].FixedValue; num6++)
					{
						array14[num6 + output.Shape[3].FixedValue * 2] = Sigmoid(array14[num6 + output.Shape[3].FixedValue * 2], segFittingParamFt);
					}
					for (int num7 = 0; num7 < output.Shape[3].FixedValue; num7++)
					{
						array14[num7 + output.Shape[3].FixedValue * 2] *= array13[num7 + num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue];
						array14[num7 + output.Shape[3].FixedValue * 2] = ApplyAct1(array14[num7 + output.Shape[3].FixedValue * 2], binAct.ToArray<Half>(), num7, is16Segments: false);
					}
					for (int num8 = 0; num8 < output.Shape[3].FixedValue; num8++)
					{
						int num9 = num8;
						_ = output.Shape[3].FixedValue;
						int num10 = num9 + 0;
						int num11 = num8;
						_ = output.Shape[3].FixedValue;
						array14[num10] = Sigmoid(array14[num11 + 0], segFittingParamFt);
					}
					for (int num12 = 0; num12 < output.Shape[3].FixedValue; num12++)
					{
						array14[num12 + output.Shape[3].FixedValue * 3] = Tanh(array14[num12 + output.Shape[3].FixedValue * 3], segFittingParamGt);
					}
					for (int num13 = 0; num13 < output.Shape[3].FixedValue; num13++)
					{
						int num14 = num13;
						_ = output.Shape[3].FixedValue;
						array14[num14 + 0] *= array14[num13 + output.Shape[3].FixedValue * 3];
						int num15 = num13;
						_ = output.Shape[3].FixedValue;
						int num16 = num15 + 0;
						int num17 = num13;
						_ = output.Shape[3].FixedValue;
						array14[num16] = ApplyAct1(array14[num17 + 0], binAct.ToArray<Half>(), num13, is16Segments: false);
					}
					for (int num18 = 0; num18 < output.Shape[3].FixedValue; num18++)
					{
						int num19 = num18 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue;
						float num20 = array14[num18 + output.Shape[3].FixedValue * 2];
						int num21 = num18;
						_ = output.Shape[3].FixedValue;
						array13[num19] = num20 + array14[num21 + 0];
					}
					for (int num22 = 0; num22 < output.Shape[3].FixedValue; num22++)
					{
						array14[num22 + output.Shape[3].FixedValue] = Sigmoid(array14[num22 + output.Shape[3].FixedValue], segFittingParamFt);
					}
					for (int num23 = 0; num23 < output.Shape[3].FixedValue; num23++)
					{
						array14[num23 + output.Shape[3].FixedValue * 3] = Tanh(array13[num23 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue], segFittingParamGt);
					}
					for (int num24 = 0; num24 < output.Shape[3].FixedValue; num24++)
					{
						array[num24 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue] = array14[num24 + output.Shape[3].FixedValue * 3] * array14[num24 + output.Shape[3].FixedValue];
						array[num24 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue] = ApplyAct1(array[num24 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue], binQAct.ToArray<Half>(), num24, is16Segments: false);
						int val = (int)System.Math.Round(array[num24 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue]);
						if (dataTypeO == DataTypes.UInt8)
						{
							array7[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num24] = (byte)System.Math.Min(System.Math.Max(0, val), 255);
						}
						else if (dataTypeO == DataTypes.Int8)
						{
							array6[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num24] = (sbyte)System.Math.Min(System.Math.Max(-127, val), 127);
						}
						else if (dataTypeO == DataTypes.Int16)
						{
							array8[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num24] = (short)System.Math.Min(System.Math.Max(-2047, val), 2047);
						}
						else
						{
							array5[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num24] = (Half)array[n * output.Shape[2].FixedValue * output.Shape[3].FixedValue];
						}
					}
					if (num2 != list.Count - 1)
					{
						continue;
					}
					if (dataTypeOH == DataTypes.UInt8)
					{
						for (int num25 = 0; num25 < output.Shape[3].FixedValue; num25++)
						{
							array11[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num25] = array7[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num25];
						}
					}
					else if (dataTypeOH == DataTypes.Int8)
					{
						for (int num26 = 0; num26 < output.Shape[3].FixedValue; num26++)
						{
							array10[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num26] = array6[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num26];
						}
					}
					else if (dataTypeOH == DataTypes.Int16)
					{
						for (int num27 = 0; num27 < output.Shape[3].FixedValue; num27++)
						{
							array12[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num27] = array8[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num27];
						}
					}
					else
					{
						for (int num28 = 0; num28 < output.Shape[3].FixedValue; num28++)
						{
							array9[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num28] = array5[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + list[num2] * output.Shape[1].FixedValue * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num28];
						}
					}
					for (int num29 = 0; num29 < output.Shape[3].FixedValue; num29++)
					{
						array4[num * output.Shape[3].FixedValue + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue + num29] = (Half)array13[num29 + n * output.Shape[2].FixedValue * output.Shape[3].FixedValue];
					}
				}
			}
		}
		output = ((dataTypeO == DataTypes.Float16) ? Tensor.From(array5, output.Shape) : ((dataTypeO == DataTypes.Int8) ? Tensor.From(array6, output.Shape) : ((!(dataTypeO == DataTypes.Int16)) ? ((Tensor)Tensor.From(array7, output.Shape)) : ((Tensor)Tensor.From(array8, output.Shape)))));
		outputH = ((dataTypeOH == DataTypes.Float16) ? Tensor.From(array9, outputH.Shape) : ((dataTypeOH == DataTypes.Int8) ? Tensor.From(array10, outputH.Shape) : ((!(dataTypeOH == DataTypes.Int16)) ? ((Tensor)Tensor.From(array11, outputH.Shape)) : ((Tensor)Tensor.From(array12, outputH.Shape)))));
		outputC = Tensor.From(array4, outputC.Shape);
		return outputSize switch
		{
			1 => new Tensor[1] { output }, 
			2 => new Tensor[2] { output, outputH }, 
			3 => new Tensor[3] { output, outputH, outputC }, 
			_ => null, 
		};
	}

	public static Tensor FakeAi2dResizeBilinear(Tensor input, int[] newSize, bool alignCorners, bool halfPixelCenters)
	{
		int num = newSize[0];
		int num2 = newSize[1];
		float[] resizeScales = GetResizeScales(input.Shape.ToValueArray(), num, num2, alignCorners);
		float num3 = resizeScales[0];
		float num4 = resizeScales[1];
		float num5 = 0f;
		float[] array = input.ToArray<float>();
		float[] array2 = new float[input.Shape.ToValueArray()[0] * input.Shape.ToValueArray()[1] * newSize[0] * newSize[1]];
		for (int i = 0; i < input.Shape.ToValueArray()[0]; i++)
		{
			for (int j = 0; j < input.Shape.ToValueArray()[1]; j++)
			{
				for (int k = 0; k < num; k++)
				{
					double[] array3 = SetResizeBilinear(k, num3, halfPixelCenters, input.Shape.ToValueArray()[2], 0.0, 0, 0);
					float num6 = (float)array3[0];
					int num7 = (int)array3[1];
					int num8 = (int)array3[2];
					for (int l = 0; l < num2; l++)
					{
						double[] array4 = SetResizeBilinear(l, num4, halfPixelCenters, input.Shape.ToValueArray()[3], 0.0, 0, 0);
						float num9 = (float)array4[0];
						int num10 = (int)array4[1];
						int num11 = (int)array4[2];
						float num12 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num7 * input.Shape.ToValueArray()[3] + num10];
						float num13 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num8 * input.Shape.ToValueArray()[3] + num10];
						float num14 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num7 * input.Shape.ToValueArray()[3] + num11];
						float num15 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num8 * input.Shape.ToValueArray()[3] + num11];
						float num16 = (1f - (num6 - (float)num7)) * (1f - (num9 - (float)num10));
						float num17 = (num6 - (float)num7) * (1f - (num9 - (float)num10));
						float num18 = (1f - (num6 - (float)num7)) * (num9 - (float)num10);
						float num19 = (num6 - (float)num7) * (num9 - (float)num10);
						array2[i * input.Shape.ToValueArray()[1] * num * num2 + j * num * num2 + k * num2 + l] = num12 * num16 + num13 * num17 + num14 * num18 + num15 * num19 + num5;
					}
				}
			}
		}
		return new Tensor<float>(array2, new int[4]
		{
			input.Shape.ToValueArray()[0],
			input.Shape.ToValueArray()[1],
			num,
			num2
		});
	}

	public static Tensor FakeAi2dResizeNearestNeighbor(Tensor input, int[] newSize, bool alignCorners, bool halfPixelCenters)
	{
		int num = newSize[0];
		int num2 = newSize[1];
		float[] resizeScales = GetResizeScales(input.Shape.ToValueArray(), num, num2, alignCorners);
		float scale = resizeScales[0];
		float scale2 = resizeScales[1];
		float[] array = input.ToArray<float>();
		float[] array2 = new float[input.Shape.ToValueArray()[0] * input.Shape.ToValueArray()[1] * newSize[0] * newSize[1]];
		for (int i = 0; i < input.Shape.ToValueArray()[0]; i++)
		{
			for (int j = 0; j < input.Shape.ToValueArray()[1]; j++)
			{
				for (int k = 0; k < num; k++)
				{
					int nearestNeighbor = GetNearestNeighbor(k, input.Shape.ToValueArray()[2], scale, alignCorners, halfPixelCenters);
					for (int l = 0; l < num2; l++)
					{
						int nearestNeighbor2 = GetNearestNeighbor(l, input.Shape.ToValueArray()[3], scale2, alignCorners, halfPixelCenters);
						array2[i * input.Shape.ToValueArray()[1] * num * num2 + j * num * num2 + k * num2 + l] = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + nearestNeighbor * input.Shape.ToValueArray()[3] + nearestNeighbor2];
					}
				}
			}
		}
		return new Tensor<float>(array2, new int[4]
		{
			input.Shape.ToValueArray()[0],
			input.Shape.ToValueArray()[1],
			num,
			num2
		});
	}

	public static Tensor Ai2dResizeBilinear(PrimType dataTypes, Tensor input, int[] newSize, bool alignCorners, bool halfPixelCenters, int inDeqBias, QuantParam quantParam)
	{
		int num = newSize[0];
		int num2 = newSize[1];
		float[] resizeScales = GetResizeScales(input.Shape.ToValueArray(), num, num2, alignCorners);
		float num3 = resizeScales[0];
		float num4 = resizeScales[1];
		float num5 = 0f;
		float[] array = input.ToArray<float>();
		float[] array2 = new float[input.Shape.ToValueArray()[0] * input.Shape.ToValueArray()[1] * newSize[0] * newSize[1]];
		for (int i = 0; i < input.Shape.ToValueArray()[0]; i++)
		{
			for (int j = 0; j < input.Shape.ToValueArray()[1]; j++)
			{
				for (int k = 0; k < num; k++)
				{
					double[] array3 = SetResizeBilinear(k, num3, halfPixelCenters, input.Shape.ToValueArray()[2], 0.0, 0, 0);
					float num6 = (float)array3[0];
					int num7 = (int)array3[1];
					int num8 = (int)array3[2];
					for (int l = 0; l < num2; l++)
					{
						double[] array4 = SetResizeBilinear(l, num4, halfPixelCenters, input.Shape.ToValueArray()[3], 0.0, 0, 0);
						float num9 = (float)array4[0];
						int num10 = (int)array4[1];
						int num11 = (int)array4[2];
						float num12 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num7 * input.Shape.ToValueArray()[3] + num10];
						float num13 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num8 * input.Shape.ToValueArray()[3] + num10];
						float num14 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num7 * input.Shape.ToValueArray()[3] + num11];
						float num15 = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + num8 * input.Shape.ToValueArray()[3] + num11];
						float num16 = (1f - (num6 - (float)num7)) * (1f - (num9 - (float)num10));
						float num17 = (num6 - (float)num7) * (1f - (num9 - (float)num10));
						float num18 = (1f - (num6 - (float)num7)) * (num9 - (float)num10);
						float num19 = (num6 - (float)num7) * (num9 - (float)num10);
						float num20 = num12 * num16 + num13 * num17 + num14 * num18 + num15 * num19 + num5;
						array2[i * input.Shape.ToValueArray()[1] * num * num2 + j * num * num2 + k * num2 + l] = num20;
					}
				}
			}
		}
		if (dataTypes == DataTypes.UInt8)
		{
			byte[] array5 = new byte[array2.Length];
			int num21 = 0;
			float[] array6 = array2;
			foreach (float num22 in array6)
			{
				array5[num21++] = (byte)num22;
			}
			return new Tensor<byte>(array5, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Int8)
		{
			sbyte[] array7 = new sbyte[array2.Length];
			int num23 = 0;
			float[] array6 = array2;
			foreach (float num24 in array6)
			{
				array7[num23++] = (sbyte)num24;
			}
			return new Tensor<sbyte>(array7, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Float16)
		{
			Half[] array8 = new Half[array2.Length];
			int num25 = 0;
			float[] array6 = array2;
			foreach (float num26 in array6)
			{
				array8[num25++] = (Half)num26;
			}
			return new Tensor<Half>(array8, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Int16)
		{
			short[] array9 = new short[array2.Length];
			int num27 = 0;
			float[] array6 = array2;
			foreach (float num28 in array6)
			{
				array9[num27++] = (short)num28;
			}
			return new Tensor<short>(array9, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		return new Tensor<float>(array2, new int[4]
		{
			input.Shape.ToValueArray()[0],
			input.Shape.ToValueArray()[1],
			num,
			num2
		});
	}

	public static Tensor Ai2dResizeNearestNeighbor(PrimType dataTypes, Tensor input, int[] newSize, bool alignCorners, bool halfPixelCenters, int inDeqBias, QuantParam quantParam)
	{
		int num = newSize[0];
		int num2 = newSize[1];
		float[] resizeScales = GetResizeScales(input.Shape.ToValueArray(), num, num2, alignCorners);
		float scale = resizeScales[0];
		float scale2 = resizeScales[1];
		float[] array = input.ToArray<float>();
		float[] array2 = new float[input.Shape.ToValueArray()[0] * input.Shape.ToValueArray()[1] * newSize[0] * newSize[1]];
		for (int i = 0; i < input.Shape.ToValueArray()[0]; i++)
		{
			for (int j = 0; j < input.Shape.ToValueArray()[1]; j++)
			{
				for (int k = 0; k < num; k++)
				{
					int nearestNeighbor = GetNearestNeighbor(k, input.Shape.ToValueArray()[2], scale, alignCorners, halfPixelCenters);
					for (int l = 0; l < num2; l++)
					{
						int nearestNeighbor2 = GetNearestNeighbor(l, input.Shape.ToValueArray()[3], scale2, alignCorners, halfPixelCenters);
						array2[i * input.Shape.ToValueArray()[1] * num * num2 + j * num * num2 + k * num2 + l] = array[i * input.Shape.ToValueArray()[1] * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + j * input.Shape.ToValueArray()[2] * input.Shape.ToValueArray()[3] + nearestNeighbor * input.Shape.ToValueArray()[3] + nearestNeighbor2];
					}
				}
			}
		}
		if (dataTypes == DataTypes.UInt8)
		{
			byte[] array3 = new byte[array2.Length];
			int num3 = 0;
			float[] array4 = array2;
			foreach (float num4 in array4)
			{
				array3[num3++] = (byte)num4;
			}
			return new Tensor<byte>(array3, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Int8)
		{
			sbyte[] array5 = new sbyte[array2.Length];
			int num5 = 0;
			float[] array4 = array2;
			foreach (float num6 in array4)
			{
				array5[num5++] = (sbyte)num6;
			}
			return new Tensor<sbyte>(array5, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Float16)
		{
			Half[] array6 = new Half[array2.Length];
			int num7 = 0;
			float[] array4 = array2;
			foreach (float num8 in array4)
			{
				array6[num7++] = (Half)num8;
			}
			return new Tensor<Half>(array6, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		if (dataTypes == DataTypes.Int16)
		{
			short[] array7 = new short[array2.Length];
			int num9 = 0;
			float[] array4 = array2;
			foreach (float num10 in array4)
			{
				array7[num9++] = (short)num10;
			}
			return new Tensor<short>(array7, new int[4]
			{
				input.Shape.ToValueArray()[0],
				input.Shape.ToValueArray()[1],
				num,
				num2
			});
		}
		return new Tensor<float>(array2, new int[4]
		{
			input.Shape.ToValueArray()[0],
			input.Shape.ToValueArray()[1],
			num,
			num2
		});
	}

	public static float[] GetResizeScales(int[] inShape, int outH, int outW, bool alignCorners)
	{
		float num = (float)inShape[2] / (float)outH;
		float num2 = (float)inShape[3] / (float)outW;
		if (alignCorners && outH > 1)
		{
			num = (float)(inShape[2] - 1) / (float)(outH - 1);
		}
		if (alignCorners && outW > 1)
		{
			num2 = (float)(inShape[3] - 1) / (float)(outW - 1);
		}
		return new float[2] { num, num2 };
	}

	public static double[] SetResizeBilinear(int value, double scale, bool halfPixelCenters, int shapeSize, double scaledValue, int v0, int v1)
	{
		scaledValue = ((!halfPixelCenters) ? ((double)value * scale) : (((double)value + 0.5) * scale - 0.5));
		v0 = (int)System.Math.Max(System.Math.Floor(scaledValue), 0.0);
		v1 = (int)System.Math.Min(System.Math.Ceiling(scaledValue), shapeSize - 1);
		return new double[3] { scaledValue, v0, v1 };
	}

	public static int GetNearestNeighbor(float input, int shapeSize, float scale, bool alignCorners, bool halfPixelCenters)
	{
		float num = (halfPixelCenters ? 0.5f : 0f);
		float num2 = (input + num) * scale;
		int num3 = System.Math.Min((int)(alignCorners ? System.Math.Round(num2) : System.Math.Floor(num2)), shapeSize - 1);
		if (halfPixelCenters)
		{
			num3 = System.Math.Max(0, num3);
		}
		return num3;
	}

	public static int ComputeSize(Const input)
	{
		return ComputeSize(input.CheckedShape);
	}

	public static int ComputeSize(int[] shape)
	{
		return shape.Aggregate(1, (int sum, int x) => sum * x);
	}

	public static int ComputeSize(Shape shape)
	{
		return shape.Prod().FixedValue;
	}

	public static int LinearIndex(int[] shape, int[] index)
	{
		int num = index[0];
		for (int i = 1; i < shape.Length; i++)
		{
			num = num * shape[i] + index[i];
		}
		return num;
	}

	public static int[] GetDefaultStrides(int[] shape)
	{
		int[] array = new int[shape.Length];
		int num = 1;
		for (int num2 = shape.Length - 1; num2 >= 0; num2--)
		{
			array[num2] = num;
			num = array[num2] * shape[num2];
			if (shape[num2] == 1)
			{
				array[num2] = 0;
			}
		}
		return array;
	}

	public static Const ConcatOutput<T>(List<T[]> outputTmp, int[] outShape) where T : unmanaged, IEquatable<T>
	{
		IEnumerable<T> seed = Array.Empty<T>().AsEnumerable();
		return Const.FromTensor(Tensor.From(outputTmp.Aggregate(seed, (IEnumerable<T> sum, T[] output) => sum.Concat(output)).ToArray(), outShape));
	}

	public static OrtKISharp.Tensor Proc(int oc)
	{
		return Enumerable.Repeat(0, oc).Select((Func<int, float>)((int x) => x)).ToArray();
	}

	public static OrtKISharp.Tensor DefaultBias(int oc)
	{
		return Tensor.FromArray(Enumerable.Repeat(0, oc).Select((Func<int, float>)((int x) => x)).ToArray()).ToOrtTensor();
	}

	public static bool IsAnyHalfPixel(ImageResizeTransformationMode mode)
	{
		if ((uint)mode <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool CanBeLoweredToCrop(ResizeImage r)
	{
		return CanBeLoweredToCrop(r.ResizeMode, r.NearestMode, r.TransformationMode);
	}

	public static bool CanBeLoweredToCrop(ImageResizeMode resizeMode, ImageResizeNearestMode nearestMode, ImageResizeTransformationMode transformationMode)
	{
		if (transformationMode == ImageResizeTransformationMode.TFCropAndResize)
		{
			return false;
		}
		if (resizeMode == ImageResizeMode.Bilinear && IsAnyHalfPixel(transformationMode))
		{
			return false;
		}
		if (resizeMode == ImageResizeMode.NearestNeighbor)
		{
			switch (nearestMode)
			{
			case ImageResizeNearestMode.Ceil:
				return false;
			case ImageResizeNearestMode.Floor:
				return transformationMode == ImageResizeTransformationMode.Asymmetric;
			case ImageResizeNearestMode.RoundPreferCeil:
				if (transformationMode == ImageResizeTransformationMode.Asymmetric)
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public static void DynamicGnneMatmul<TIA, TIB, TO>(ReadOnlySpan<TIA> inputA, ReadOnlySpan<TIB> inputB, Span<TO> output, ReadOnlySpan<Half> act, ReadOnlySpan<byte> inABias, int aBatch0, int aBatch1, int aRows, int aCols, int bBatch0, int bBatch1, int bCols, byte inBBias, int inAShiftBits, int inBShiftBits, sbyte shiftBits, bool dynamicChannel = true) where TIA : unmanaged where TIB : unmanaged where TO : unmanaged
	{
		if (aBatch0 != bBatch0 && new int[2] { aBatch0, bBatch0 }.All((int x) => x != 1))
		{
			throw new ArgumentOutOfRangeException("inputA");
		}
		if (aBatch1 != bBatch1 && new int[2] { aBatch1, bBatch1 }.All((int x) => x != 1))
		{
			throw new ArgumentOutOfRangeException("inputA");
		}
		int[] strides = TensorUtilities.GetStrides(new int[4] { aBatch0, aBatch1, aRows, aCols });
		int[] strides2 = TensorUtilities.GetStrides(new int[4] { bBatch0, bBatch1, aCols, bCols });
		int num = System.Math.Max(aBatch0, bBatch0);
		int num2 = System.Math.Max(aBatch1, bBatch1);
		int[] strides3 = TensorUtilities.GetStrides(new int[4] { num, num2, aRows, bCols });
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				for (int k = 0; k < aRows; k++)
				{
					int index = TensorUtilities.GetIndex(strides3, new int[4] { i, j, k, 0 });
					for (int l = 0; l < bCols; l++)
					{
						float num3 = 0f;
						int index2 = TensorUtilities.GetIndex(strides, new int[4]
						{
							(i >= aBatch0) ? (aBatch0 - 1) : i,
							(j >= aBatch1) ? (aBatch1 - 1) : j,
							k,
							0
						});
						int index3 = TensorUtilities.GetIndex(strides2, new int[4]
						{
							(i >= bBatch0) ? (bBatch0 - 1) : i,
							(j >= bBatch1) ? (bBatch1 - 1) : j,
							0,
							l
						});
						for (int m = 0; m < aCols; m++)
						{
							TIA val = inputA[index2 + m];
							TIB val2 = inputB[index3 + m * bCols];
							num3 += ((float)Convert.ChangeType(val, typeof(float)) - (float)(int)inABias[k]) * ((float)Convert.ChangeType(val2, typeof(float)) - (float)(int)inBBias);
						}
						if (typeof(TO) == typeof(float))
						{
							output[index + l] = (TO)(object)ApplyAct0(num3, act, (!dynamicChannel) ? k : 0, shiftBits);
							continue;
						}
						if (typeof(TO) == typeof(Half))
						{
							output[index + l] = (TO)(object)(Half)ApplyAct0(num3, act, (!dynamicChannel) ? k : 0, shiftBits);
							continue;
						}
						if (typeof(TO) == typeof(byte) || typeof(TO) == typeof(sbyte) || typeof(TO) == typeof(short))
						{
							output[index + l] = (TO)Convert.ChangeType(System.Math.Round(ApplyAct0(num3, act, (!dynamicChannel) ? k : 0, shiftBits)), typeof(TO));
							continue;
						}
						throw new ArgumentOutOfRangeException("inputA");
					}
				}
			}
		}
	}

	public static void FakeDynamicGnneMatmul(IEvaluateContext context, Span<float> inputA, Span<float> inputB, Span<float> output, ReadOnlySpan<float> act, int aBatch0, int aBatch1, int aRows, int aCols, int bBatch0, int bBatch1, int bCols, bool dynamicChannel)
	{
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null)
		{
			MarkerPattern markerPattern = Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard());
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
			{
				MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
				if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
				{
					List<QuantParam> list = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo?.QuantParameter;
					Trace.Assert(list.Count == 1);
					for (int i = 0; i < inputA.Length; i++)
					{
						double num = (double)inputA[i] / (double)list[0].Scale + (double)list[0].ZeroPoint;
						if (list[0].Scale != 1f || list[0].ZeroPoint != 0)
						{
							num = System.Math.Round(num);
						}
						double num2 = (num - (double)list[0].ZeroPoint) * (double)list[0].Scale;
						inputA[i] = (float)num2;
					}
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[1]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo)
				{
					List<QuantParam> list2 = ((Marker)context.CurrentCall.Arguments[1]).MixQuantInfo?.QuantParameter;
					int count = list2.Count;
					int num3 = inputB.Length / count;
					for (int j = 0; j < inputB.Length; j++)
					{
						double num4 = (double)inputB[j] / (double)list2[j / num3].Scale + (double)list2[j / num3].ZeroPoint;
						if (list2[j / num3].Scale != 1f || list2[j / num3].ZeroPoint != 0)
						{
							num4 = System.Math.Round(num4);
						}
						double num5 = (num4 - (double)list2[j / num3].ZeroPoint) * (double)list2[j / num3].Scale;
						inputB[j] = (float)num5;
					}
				}
			}
		}
		if (aBatch0 != bBatch0 && new int[2] { aBatch0, bBatch0 }.All((int x) => x != 1))
		{
			throw new ArgumentOutOfRangeException("context");
		}
		if (aBatch1 != bBatch1 && new int[2] { aBatch1, bBatch1 }.All((int x) => x != 1))
		{
			throw new ArgumentOutOfRangeException("context");
		}
		int[] strides = TensorUtilities.GetStrides(new int[4] { aBatch0, aBatch1, aRows, aCols });
		int[] strides2 = TensorUtilities.GetStrides(new int[4] { bBatch0, bBatch1, aCols, bCols });
		int num6 = System.Math.Max(aBatch0, bBatch0);
		int num7 = System.Math.Max(aBatch1, bBatch1);
		int[] strides3 = TensorUtilities.GetStrides(new int[4] { num6, num7, aRows, bCols });
		for (int k = 0; k < num6; k++)
		{
			for (int l = 0; l < num7; l++)
			{
				for (int m = 0; m < aRows; m++)
				{
					int index = TensorUtilities.GetIndex(strides3, new int[4] { k, l, m, 0 });
					for (int n = 0; n < bCols; n++)
					{
						float num8 = 0f;
						int index2 = TensorUtilities.GetIndex(strides, new int[4]
						{
							(k >= aBatch0) ? (aBatch0 - 1) : k,
							(l >= aBatch1) ? (aBatch1 - 1) : l,
							m,
							0
						});
						int index3 = TensorUtilities.GetIndex(strides2, new int[4]
						{
							(k >= bBatch0) ? (bBatch0 - 1) : k,
							(l >= bBatch1) ? (bBatch1 - 1) : l,
							0,
							n
						});
						for (int num9 = 0; num9 < aCols; num9++)
						{
							float num10 = inputA[index2 + num9];
							float num11 = inputB[index3 + num9 * bCols];
							num8 += num10 * num11;
						}
						output[index + n] = ApplyAct01(num8, act, (!dynamicChannel) ? m : 0, 0);
					}
				}
			}
		}
	}

	private static T Clamp<T>(T value, T min, T max) where T : unmanaged, IComparable<T>
	{
		T result = ((value.CompareTo(max) > 0) ? max : value);
		if (result.CompareTo(min) <= 0)
		{
			return min;
		}
		return result;
	}

	private static T ApplyActivation<T>(T value, ValueRange<T> activation) where T : unmanaged, IEquatable<T>, IComparable<T>
	{
		return Clamp(value, activation.Min, activation.Max);
	}

	private static T ApplyGnneActivation<T>(float value, sbyte shiftBits, T x0, T kl, T bl, T kr, T br) where T : unmanaged, IComparable<T>
	{
		if (value.CompareTo(x0) < 0)
		{
			if (typeof(T) == typeof(Half))
			{
				return (T)(object)(Half)(value * (float)Convert.ChangeType(kl, typeof(float)) / (float)(1 << (int)shiftBits) + (float)Convert.ChangeType(bl, typeof(float)));
			}
			if (typeof(T) == typeof(float))
			{
				return (T)(object)(value * (float)(object)kl / (float)(1L << (int)shiftBits) + (float)(object)bl);
			}
			throw new ArgumentOutOfRangeException("value");
		}
		if (typeof(T) == typeof(Half))
		{
			return (T)(object)(Half)(value * (float)Convert.ChangeType(kr, typeof(float)) / (float)(1L << (int)shiftBits) + (float)Convert.ChangeType(br, typeof(float)));
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)(value * (float)(object)kr / (float)(1L << (int)shiftBits) + (float)(object)br);
		}
		throw new ArgumentOutOfRangeException("value");
	}

	private static float ApplyAct1(float value, ReadOnlySpan<float> actData, int channel, bool is16Segments)
	{
		if (is16Segments)
		{
			float value2 = ApplyMultiSegmentsAct1(16, value, actData);
			int num = 47;
			return ApplyActivation(value2, (Min: actData[num % actData.Length], Max: actData[(num + 1) % actData.Length]));
		}
		int num2 = 7 * channel;
		return ApplyActivation(ApplyGnneActivation(value, 0, actData[num2], actData[num2 + 1], actData[num2 + 3], actData[num2 + 2], actData[num2 + 4]), (Min: actData[num2 + 5], Max: actData[num2 + 6]));
	}

	private static float ApplyAct1(float value, ReadOnlySpan<Half> actData, int channel, bool is16Segments)
	{
		if (is16Segments)
		{
			float value2 = ApplyMultiSegmentsAct1(16, value, actData);
			int num = 47;
			return ApplyActivation(value2, (Min: (float)actData[num], Max: (float)actData[num + 1]));
		}
		int num2 = 7 * channel;
		return ApplyActivation(ApplyGnneActivation(value, 0, (float)actData[num2], (float)actData[num2 + 1], (float)actData[num2 + 3], (float)actData[num2 + 2], (float)actData[num2 + 4]), (Min: (float)actData[num2 + 5], Max: (float)actData[num2 + 6]));
	}
}
