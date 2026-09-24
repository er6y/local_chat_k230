#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

public class QuantizeManager
{
	private Tensor<float> _inRange;

	private Tensor<float> _wRange;

	private Tensor<float> _oRange;

	private ConvQuantConf _conf;

	private QuantizeParam[] _wQuantParam;

	private QuantizeParam[] _wQuantParamQint8;

	private Tensor _oldWeights;

	public ValueRange<float> InputRange => ToRange(_inRange);

	public ValueRange<float> OutRange => ToRange(_oRange);

	public QuantizeManager(Tensor<float> inRange, Tensor<float> wRange, Tensor<float> oRange, Tensor oldWeights, ConvQuantConf conf, bool is_matmul, int groupsConvTranspose = 1)
	{
		_inRange = inRange;
		_wRange = wRange;
		_oRange = oRange;
		_oldWeights = oldWeights;
		_conf = conf;
		_wQuantParam = WeightsByChannelQP(is_matmul, groupsConvTranspose);
		_wQuantParamQint8 = WeightsByChannelQPQint8(is_matmul, groupsConvTranspose);
	}

	public static ValueRange<float> ToRange(Tensor<float> t)
	{
		float[] array = t.ToArray();
		return new ValueRange<float>(array[0], array[1]);
	}

	public QuantizeParam GetIfQuantParam(DataType quantType, int inIdx = 0)
	{
		return QuantHelper.GetQuantParamFromDeqParam(GetIfDeqQuantParam(quantType, inIdx));
	}

	public DeQuantizeParam GetIfDeqQuantParam(DataType quantType, int inIdx = 0)
	{
		Trace.Assert(inIdx < 2);
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((quantType == DataTypes.Int16) ? 12 : 8);
		return QuantHelper.GetDeqParam(QuantUtility.GetQuantParam(InputRange, bits, quantMode));
	}

	public QuantizeParam[] GetWeightsQuantParam()
	{
		return _wQuantParam;
	}

	public QuantizeParam[] GetWeightsQuantParamQint8()
	{
		return _wQuantParamQint8;
	}

	public byte[] GetWeightsQuantBias()
	{
		QuantizeParam[] weightsQuantParam = GetWeightsQuantParam();
		byte[] array = new byte[weightsQuantParam.Length];
		for (int i = 0; i < weightsQuantParam.Length; i++)
		{
			array[i] = (byte)weightsQuantParam[i].ZeroPoint;
		}
		return array;
	}

	public byte[] GetWeightsQuantBiasQint8()
	{
		QuantizeParam[] weightsQuantParamQint = GetWeightsQuantParamQint8();
		byte[] array = new byte[weightsQuantParamQint.Length];
		for (int i = 0; i < weightsQuantParamQint.Length; i++)
		{
			array[i] = (byte)weightsQuantParamQint[i].ZeroPoint;
		}
		return array;
	}

	public float[] GetWeightsDeqScale()
	{
		QuantizeParam[] weightsQuantParam = GetWeightsQuantParam();
		float[] array = new float[weightsQuantParam.Length];
		for (int i = 0; i < weightsQuantParam.Length; i++)
		{
			array[i] = QuantHelper.GetDeqParamFromQuantParam(weightsQuantParam[i]).Scale;
		}
		return array;
	}

	public QuantT[] GetQuantWeights(bool isMatmul = false, int groupsConvTranspose = 1)
	{
		_ = _oldWeights.Shape;
		int num = _oldWeights.Shape[0].FixedValue * groupsConvTranspose;
		if (isMatmul)
		{
			num = _oldWeights.Shape[1].FixedValue * _oldWeights.Shape[2].FixedValue;
		}
		float[] array = _oldWeights.ToArray<float>();
		int num2 = array.Length / num;
		QuantT[] array2 = (from _ in Enumerable.Range(0, array.Length)
			select new QuantT(_conf.WQuantType == DataTypes.UInt8)).ToArray();
		QuantizeParam[] weightsQuantParam = GetWeightsQuantParam();
		QuantizeParam[] weightsQuantParamQint = GetWeightsQuantParamQint8();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				int num3 = i * num2 + j;
				if (_conf.WQuantType == DataTypes.Int8)
				{
					array2[num3].I8 = (sbyte)Math.Clamp(Convert.ToInt32(QuantHelper.Quantize<sbyte>(array[num3], weightsQuantParam[i])), -127, 127);
					continue;
				}
				if (_conf.WQuantType == DataTypes.UInt8)
				{
					array2[num3].U8 = QuantHelper.Quantize<byte>(array[num3], weightsQuantParam[i]);
					continue;
				}
				if (_conf.WQuantType == DataTypes.Int16)
				{
					array2[num3].U8 = QuantHelper.Quantize<byte>(array[num3], weightsQuantParamQint[i]);
					continue;
				}
				throw new InvalidDataException("Invalid w_quant_type");
			}
		}
		return array2;
	}

	public short[] GetQuantWeightsI16(bool isMatmul = false, int groupsConvTranspose = 1)
	{
		_ = _oldWeights.Shape;
		int num = _oldWeights.Shape[0].FixedValue * groupsConvTranspose;
		if (isMatmul)
		{
			num = _oldWeights.Shape[1].FixedValue * _oldWeights.Shape[2].FixedValue;
		}
		float[] array = _oldWeights.ToArray<float>();
		int num2 = array.Length / num;
		short[] array2 = new short[array.Length];
		QuantizeParam[] weightsQuantParam = GetWeightsQuantParam();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				int num3 = i * num2 + j;
				if (_conf.WQuantType == DataTypes.Int16)
				{
					array2[num3] = (short)Math.Clamp((int)QuantHelper.Quantize<short>(array[num3], weightsQuantParam[i]), -2047, 2047);
					continue;
				}
				throw new InvalidDataException("Invalid w_quant_type");
			}
		}
		return array2;
	}

	public byte[] GetQuantWeightsU8(bool isMatmul = false, int groupsConvTranspose = 1)
	{
		_ = _oldWeights.Shape;
		int num = _oldWeights.Shape[0].FixedValue * groupsConvTranspose;
		if (isMatmul)
		{
			num = _oldWeights.Shape[1].FixedValue * _oldWeights.Shape[2].FixedValue;
		}
		float[] array = _oldWeights.ToArray<float>();
		int num2 = array.Length / num;
		byte[] array2 = new byte[array.Length];
		QuantizeParam[] weightsQuantParamQint = GetWeightsQuantParamQint8();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				int num3 = i * num2 + j;
				array2[num3] = QuantHelper.Quantize<byte>(array[num3], weightsQuantParamQint[i]);
			}
		}
		return array2;
	}

	public QuantizeParam GetOfQuantParam(DataType quantType, int outIdx = 0)
	{
		return QuantHelper.GetQuantParamFromDeqParam(GetOfDeqQuantParam(quantType, outIdx));
	}

	public DeQuantizeParam GetOfDeqQuantParam(DataType quantType, int outIdx = 0)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((quantType == DataTypes.Int16) ? 12 : 8);
		return QuantHelper.GetDeqParam(QuantUtility.GetQuantParam(OutRange, bits, quantMode));
	}

	public QuantizeParam[] WeightsByChannelQP(bool isMatmul = false, int groupConvTranspose = 1)
	{
		int num = _oldWeights.Shape[0].FixedValue * groupConvTranspose;
		if (isMatmul)
		{
			num = (_oldWeights.Shape[1] * _oldWeights.Shape[2]).FixedValue;
		}
		float[] array = _oldWeights.ToArray<float>();
		QuantizeParam[] array2 = new QuantizeParam[num];
		int num2 = _oldWeights.Shape.Prod().FixedValue / num;
		int bits = ((_conf.WQuantType == DataTypes.Int16) ? 12 : 8);
		for (int i = 0; i < num; i++)
		{
			new Span<float>(array, i * num2, num2);
			bool symmetric = _conf.WQuantType != DataTypes.UInt8;
			QuantMode quantMode = ((!(_conf.WQuantType == DataTypes.UInt8)) ? QuantMode.SignedAsymmetricMode : QuantMode.UnsignedMode);
			float min = ((_wRange.Rank == 1) ? _wRange[new int[1]] : _wRange[new int[2] { i, 0 }]);
			float max = ((_wRange.Rank == 1) ? _wRange[new int[1] { 1 }] : _wRange[new int[2] { i, 1 }]);
			ValueRange<float> range = default(ValueRange<float>);
			range.Min = min;
			range.Max = max;
			QuantizeParam quantParam = QuantHelper.GetQuantParam(QuantUtility.GetQuantParam(QuantUtility.FixupRange(range, symmetric), bits, quantMode));
			array2[i].Scale = quantParam.Scale;
			array2[i].ZeroPoint = quantParam.ZeroPoint;
		}
		return array2;
	}

	public QuantizeParam[] WeightsByChannelQPQint8(bool isMatmul = false, int groupsConvTranspose = 1)
	{
		int num = _oldWeights.Shape[0].FixedValue * groupsConvTranspose;
		if (isMatmul)
		{
			num = (_oldWeights.Shape[1] * _oldWeights.Shape[2]).FixedValue;
		}
		float[] array = _oldWeights.ToArray<float>();
		QuantizeParam[] array2 = new QuantizeParam[num];
		int num2 = _oldWeights.Shape.Prod().FixedValue / num;
		if (_conf.UseMseQuantW)
		{
			QuantParam qp = default(QuantParam);
			QuantParam qp2 = default(QuantParam);
			for (int i = 0; i < num; i++)
			{
				Span<float> span = new Span<float>(array, i * num2, num2);
				float num3 = 0f;
				float num4 = 0f;
				float num5 = float.MaxValue;
				float[] array3 = Enumerable.Repeat(0f, num2).ToArray();
				byte[] array4 = Enumerable.Repeat((byte)0, num2).ToArray();
				float min = ((_wRange.Rank == 1) ? _wRange[new int[1]] : _wRange[new int[2] { i, 0 }]);
				float max = ((_wRange.Rank == 1) ? _wRange[new int[1] { 1 }] : _wRange[new int[2] { i, 1 }]);
				ValueRange<float> range = default(ValueRange<float>);
				range.Min = min;
				range.Max = max;
				ValueRange<float> valueRange = QuantUtility.FixupRange(range);
				float num6 = (valueRange.Max - valueRange.Min) / 256f;
				for (uint num7 = 0u; num7 < 32; num7++)
				{
					range = default(ValueRange<float>);
					range.Min = valueRange.Min - num6 * (float)num7;
					range.Max = valueRange.Max;
					QuantizeParam quantParam = QuantHelper.GetQuantParam(QuantUtility.GetQuantParam(QuantUtility.FixupRange(range), 8, QuantMode.UnsignedMode));
					qp.Scale = 1f / quantParam.Scale;
					qp.ZeroPoint = quantParam.ZeroPoint;
					for (int j = 0; j < span.Length; j++)
					{
						array4[j] = QuantHelper.Quantize1<byte>(span[j], qp);
						array3[j] = (float)(array4[j] - qp.ZeroPoint) * qp.Scale;
					}
					float num8 = 0f;
					for (int k = 0; k < num2; k++)
					{
						num8 += (float)Math.Pow(span[k] - array3[k], 2.0);
					}
					if (num5 > num8)
					{
						num3 = num6 * (float)num7;
						num5 = num8;
					}
				}
				for (uint num9 = 0u; num9 < 32; num9++)
				{
					range = default(ValueRange<float>);
					range.Min = valueRange.Min;
					range.Max = valueRange.Max + num6 * (float)num9;
					QuantizeParam quantParam2 = QuantHelper.GetQuantParam(QuantUtility.GetQuantParam(QuantUtility.FixupRange(range), 8, QuantMode.UnsignedMode));
					qp2.Scale = 1f / quantParam2.Scale;
					qp2.ZeroPoint = quantParam2.ZeroPoint;
					for (int l = 0; l < span.Length; l++)
					{
						array4[l] = QuantHelper.Quantize1<byte>(span[l], qp2);
						array3[l] = (float)(array4[l] - qp2.ZeroPoint) * qp2.Scale;
					}
					float num10 = 0f;
					for (int m = 0; m < num2; m++)
					{
						num10 += (float)Math.Pow(span[m] - array3[m], 2.0);
					}
					if (num5 > num10)
					{
						num4 = num6 * (float)num9;
						num5 = num10;
					}
				}
				ValueRange<float> range2 = default(ValueRange<float>);
				if (num4 > 0f)
				{
					range2.Max = valueRange.Max + num4;
					range2.Min = valueRange.Min;
				}
				else if (num3 > 0f)
				{
					range2.Max = valueRange.Max;
					range2.Min = valueRange.Min - num3;
				}
				else
				{
					range2.Max = valueRange.Max;
					range2.Min = valueRange.Min;
				}
				array2[i] = QuantHelper.GetQuantParam(QuantUtility.GetQuantParam(range2, 8, QuantMode.UnsignedMode));
			}
			return array2;
		}
		int bits = 8;
		for (int n = 0; n < num; n++)
		{
			new Span<float>(array, n * num2, num2);
			bool symmetric = false;
			QuantMode quantMode = QuantMode.UnsignedMode;
			float min2 = ((_wRange.Rank == 1) ? _wRange[new int[1]] : _wRange[new int[2] { n, 0 }]);
			float max2 = ((_wRange.Rank == 1) ? _wRange[new int[1] { 1 }] : _wRange[new int[2] { n, 1 }]);
			ValueRange<float> range = default(ValueRange<float>);
			range.Min = min2;
			range.Max = max2;
			QuantizeParam quantParam3 = QuantHelper.GetQuantParam(QuantUtility.GetQuantParam(QuantUtility.FixupRange(range, symmetric), bits, quantMode));
			array2[n].Scale = quantParam3.Scale;
			array2[n].ZeroPoint = quantParam3.ZeroPoint;
		}
		return array2;
	}
}
