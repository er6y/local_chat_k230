using System;
using System.Collections;
using System.IO;
using System.Linq;
using Nncase.Passes.Rules.K230;

namespace Nncase.IR.K230;

public abstract class ActParamBase
{
	private float[,] _xs;

	private float[,] _ks;

	private float[,] _bs;

	private ValueRange<float>[] _fusedClamp;

	private QuantizeParam _qp;

	private bool _isDeq;

	private int _channels;

	public float[,] Xs
	{
		get
		{
			return _xs;
		}
		set
		{
			_xs = value ?? throw new ArgumentNullException("value");
		}
	}

	public float[,] Ks
	{
		get
		{
			return _ks;
		}
		set
		{
			_ks = value ?? throw new ArgumentNullException("value");
		}
	}

	public float[,] Bs
	{
		get
		{
			return _bs;
		}
		set
		{
			_bs = value ?? throw new ArgumentNullException("value");
		}
	}

	public ValueRange<float>[] FusedClamp
	{
		get
		{
			return _fusedClamp;
		}
		set
		{
			_fusedClamp = value ?? throw new ArgumentNullException("value");
		}
	}

	public bool IsDeq
	{
		get
		{
			return _isDeq;
		}
		set
		{
			_isDeq = value;
		}
	}

	public int Channels
	{
		get
		{
			return _channels;
		}
		set
		{
			_channels = value;
		}
	}

	public QuantizeParam Qp
	{
		get
		{
			return _qp;
		}
		set
		{
			_qp = value;
		}
	}

	public virtual int N { get; } = 2;


	public virtual Shape Shape { get; }

	public int DataSizePerChannel => 3 * N - 1 + 2;

	public float[] GetAct0Data => Enumerable.Range(0, Channels).Select(delegate(int j)
	{
		float[] array = new float[N];
		float[] array2 = new float[N];
		float[] array3 = new float[N - 1];
		for (int i = 0; i < N; i++)
		{
			array[i] = Ks[i, j];
			array2[i] = Bs[i, j];
		}
		for (int k = 0; k < N - 1; k++)
		{
			array3[k] = Xs[k, j];
		}
		float[] second = new float[2]
		{
			FusedClamp[j].Min,
			FusedClamp[j].Max
		};
		return array.Concat(array2).Concat(second).Concat(array3)
			.ToArray();
	}).Aggregate(Array.Empty<float>(), (float[] a, float[] b) => a.Concat(b).ToArray());

	public float[] GetAct1Data => Enumerable.Range(0, Channels).Select(delegate(int j)
	{
		float[] array = new float[N];
		float[] array2 = new float[N];
		float[] array3 = new float[N - 1];
		for (int i = 0; i < N; i++)
		{
			array[i] = Ks[i, j];
			array2[i] = Bs[i, j];
		}
		for (int k = 0; k < N - 1; k++)
		{
			array3[k] = Xs[k, j];
		}
		float[] second = new float[2]
		{
			FusedClamp[j].Min,
			FusedClamp[j].Max
		};
		return array3.Concat(array).Concat(array2).Concat(second)
			.ToArray();
	}).Aggregate(Array.Empty<float>(), (float[] a, float[] b) => a.Concat(b).ToArray());

	public ActParamBase(int channel, QuantParam quantParam, bool isDeq = false)
	{
		if (quantParam.Equals(new QuantParam(0, 0f)))
		{
			quantParam = new QuantParam(0, 1f);
		}
		IsDeq = isDeq;
		Channels = channel;
		Qp = QuantHelper.GetQuantParam(quantParam);
	}

	protected ActParamBase(ActParamBase other)
	{
		Qp = new QuantizeParam(other.Qp);
		Xs = new float[other.Xs.GetLength(0), other.Xs.GetLength(1)];
		Array.Copy(other.Xs, 0, Xs, 0, other.Xs.Length);
		Ks = new float[other.Ks.GetLength(0), other.Ks.GetLength(1)];
		Array.Copy(other.Ks, 0, Ks, 0, other.Ks.Length);
		Bs = new float[other.Bs.GetLength(0), other.Bs.GetLength(1)];
		Array.Copy(other.Bs, 0, Bs, 0, other.Bs.Length);
		FusedClamp = new ValueRange<float>[other.FusedClamp.Length];
		Array.Copy(other.FusedClamp, FusedClamp, FusedClamp.Length);
		IsDeq = other.IsDeq;
		Channels = other.Channels;
	}

	public void InitValues()
	{
		Xs = new float[N - 1, Channels];
		Ks = new float[N, Channels];
		Bs = new float[N, Channels];
		for (int i = 0; i < N; i++)
		{
			for (int j = 0; j < Channels; j++)
			{
				Ks[i, j] = Qp.Scale;
				Bs[i, j] = ((!IsDeq) ? ((float)Qp.ZeroPoint) : 0f);
			}
		}
		SetFusedClamp(ValueRange<float>.Full);
	}

	public void SetFusedClamp(ValueRange<Half> range)
	{
		FusedClamp = Enumerable.Range(0, Channels).Select((Func<int, ValueRange<float>>)delegate
		{
			ValueRange<float> result = default(ValueRange<float>);
			result.Min = (float)range.Min;
			result.Max = (float)range.Max;
			return result;
		}).ToArray();
	}

	public void SetFusedClamp(ValueRange<float> range)
	{
		FusedClamp = Enumerable.Repeat(range, Channels).ToArray();
	}

	public void SetFusedClamp(DataType type)
	{
		ValueRange<float> full = ValueRange<float>.Full;
		if (type == DataTypes.UInt8)
		{
			full.Min = 0f;
			full.Min = 255f;
		}
		else if (type == DataTypes.Int8)
		{
			full.Min = -127f;
			full.Max = 127f;
		}
		else if (type == DataTypes.Int16)
		{
			full.Min = -2047f;
			full.Max = 2047f;
		}
		SetFusedClamp(full);
	}

	public void FusedScale(float scale)
	{
		Ks = ActHelper.NestSelect(Ks, (float k) => k * scale);
	}

	public void ShiftBitsAdaptXs(float scale)
	{
		for (int i = 0; i < Xs.GetLength(1); i++)
		{
			Xs[0, i] /= scale;
		}
	}

	public void FusedScaleRight(float scale)
	{
		Ks = ActHelper.NestSelect(Ks, (float k) => k / scale);
	}

	public void ShiftBitsAdaptXsRight(float scale)
	{
		for (int i = 0; i < Xs.GetLength(1); i++)
		{
			Xs[0, i] *= scale;
		}
	}

	public void FusedShiftBits(sbyte shiftBits)
	{
		FusedScale(1 << (int)shiftBits);
		ShiftBitsAdaptXs(1 << (int)shiftBits);
	}

	public void FusedShiftBitsRight(sbyte shiftBits)
	{
		FusedScaleRight(1 << (int)shiftBits);
		ShiftBitsAdaptXsRight(1 << (int)shiftBits);
	}

	public void FusedChannelScale(float[] scales)
	{
		for (int i = 0; i < Ks.GetLength(0); i++)
		{
			for (int j = 0; j < scales.Length; j++)
			{
				Ks[i, j] *= scales[j];
			}
		}
	}

	public void FusedXs()
	{
		for (int i = 0; i < Xs.GetLength(1); i++)
		{
			if (!Ks[0, i].Equals(Ks[1, i]))
			{
				Xs[0, i] = (Bs[1, i] - Bs[0, i]) / (Ks[0, i] - Ks[1, i]);
			}
		}
	}

	public void FusedBias(QuantizeParam qParam)
	{
		Bs = ActHelper.NestSelect(Bs, (float b) => b * qParam.Scale + (float)qParam.ZeroPoint);
	}

	public void FusedFusedClamp(QuantizeParam qParam)
	{
		for (int i = 0; i < FusedClamp.Length; i++)
		{
			FusedClamp[i].Min = FusedClamp[i].Min * qParam.Scale + (float)qParam.ZeroPoint;
			FusedClamp[i].Max = FusedClamp[i].Max * qParam.Scale + (float)qParam.ZeroPoint;
		}
	}

	public void FusedActParam(ActParamBase rhs)
	{
		if (Channels != rhs.Channels)
		{
			throw new InvalidDataException("act param should same channel");
		}
		for (int i = 0; i < N; i++)
		{
			for (int j = 0; j < Channels; j++)
			{
				Xs[i, j] *= rhs.Xs[i, j];
				Bs[i, j] += rhs.Bs[i, j];
			}
		}
		for (int k = 0; k < Channels; k++)
		{
			FusedClamp[k].Max = System.Math.Max(FusedClamp[k].Max, rhs.FusedClamp[k].Max);
			FusedClamp[k].Min = System.Math.Min(FusedClamp[k].Min, rhs.FusedClamp[k].Min);
		}
	}

	public void FusedQuantParam(QuantParam quantParam)
	{
		QuantizeParam quantParam2 = QuantHelper.GetQuantParam(quantParam);
		FusedFusedClamp(quantParam2);
		FusedScale(quantParam2.Scale);
		FusedBias(quantParam2);
	}

	public Tensor<float> ToFakeActData()
	{
		float[] getAct0Data = GetAct0Data;
		Span<int> span = Shape.ToValueArray().AsSpan();
		return Tensor.From(getAct0Data, span.Slice(2, span.Length - 2));
	}

	public Tensor<Half> ToAct0Data()
	{
		return Tensor.From(GetAct0Data.Select((float f) => (Half)f).ToArray(), Shape);
	}

	public Tensor<Half> ToAct1Data()
	{
		return Tensor.From(GetAct1Data.Select((float f) => (Half)f).ToArray(), Shape);
	}

	public abstract int FusedShiftBits();

	public override bool Equals(object? obj)
	{
		if (!(obj is ActParamBase actParamBase))
		{
			return false;
		}
		if (!Qp.Equals(actParamBase.Qp) && StructuralComparisons.StructuralEqualityComparer.Equals(FusedClamp, actParamBase.FusedClamp) && IsDeq == actParamBase.IsDeq && Channels == actParamBase.Channels && N == actParamBase.N && Shape.Equals(actParamBase.Shape))
		{
			return false;
		}
		if (Comparer(Xs, actParamBase.Xs) && Comparer(Ks, actParamBase.Ks))
		{
			return Comparer(Bs, actParamBase.Bs);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Qp);
		float[,] xs = Xs;
		foreach (float num in xs)
		{
			hashCode.Add(StructuralComparisons.StructuralEqualityComparer.GetHashCode(num));
		}
		xs = Ks;
		foreach (float num2 in xs)
		{
			hashCode.Add(StructuralComparisons.StructuralEqualityComparer.GetHashCode(num2));
		}
		xs = Bs;
		foreach (float num3 in xs)
		{
			hashCode.Add(StructuralComparisons.StructuralEqualityComparer.GetHashCode(num3));
		}
		hashCode.Add(StructuralComparisons.StructuralEqualityComparer.GetHashCode(FusedClamp));
		hashCode.Add(IsDeq);
		hashCode.Add(Channels);
		hashCode.Add(N);
		hashCode.Add(Shape);
		return hashCode.ToHashCode();
	}

	public bool HasBiasOnly()
	{
		return (Ks.Clone() as float[]).All((float v) => v.Equals(1f));
	}

	private static bool Comparer(float[,] a, float[,] b)
	{
		if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
		{
			return false;
		}
		for (int i = 0; i < a.GetLength(0); i++)
		{
			for (int j = 0; j < a.GetLength(1); j++)
			{
				if (!a[i, j].Equals(b[i, j]))
				{
					return false;
				}
			}
		}
		return true;
	}
}
