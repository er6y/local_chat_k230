using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nncase.Passes.Rules;

public sealed class ActivationParameter<T> where T : unmanaged, IEquatable<T>, IComparable<T>
{
	private const int Maxshiftbits = 30;

	private readonly int _sizePreParam;

	private readonly Tensor<T> _data;

	private ActivationPartialParameter<T>? _xs;

	private ActivationPartialParameter<T>? _ks;

	private ActivationPartialParameter<T>? _bs;

	private ActivationPartialParameter<T>? _clamp;

	public ReadOnlySpan<int> Shape => _data.Dimensions.ToArray().SkipLast(1).ToArray();

	public sbyte ShiftBits
	{
		get
		{
			sbyte shiftBits = 30;
			Ks.ForEach(delegate(IReadOnlyList<int> _, T k)
			{
				sbyte val = ComputeShiftBits(k);
				shiftBits = Math.Min(shiftBits, val);
				return k;
			});
			return shiftBits;
		}
	}

	private int SizePerChannel => 3 * _sizePreParam - 1 + 2;

	private ActivationPartialParameter<T> Ks
	{
		get
		{
			if ((object)_ks == null)
			{
				_ks = new ActivationPartialParameter<T>(0, _sizePreParam, _data);
			}
			return _ks;
		}
	}

	private ActivationPartialParameter<T> Bs
	{
		get
		{
			if ((object)_bs == null)
			{
				_bs = new ActivationPartialParameter<T>(_sizePreParam, _sizePreParam, _data);
			}
			return _bs;
		}
	}

	private ActivationPartialParameter<T> Clamp
	{
		get
		{
			if ((object)_clamp == null)
			{
				_clamp = new ActivationPartialParameter<T>(_sizePreParam * 2, 2, _data);
			}
			return _clamp;
		}
	}

	private ActivationPartialParameter<T> Xs
	{
		get
		{
			if ((object)_xs == null)
			{
				_xs = new ActivationPartialParameter<T>(_sizePreParam * 2 + 2, _sizePreParam - 1, _data);
			}
			return _xs;
		}
	}

	public ActivationParameter(int[] shape, ValueRange<T> fusedClamp)
		: this(2, shape)
	{
		if (typeof(T) == typeof(Half))
		{
			Xs.Fill((T)(object)(Half)0f);
			Ks.Fill((T)(object)(Half)1f);
			Bs.Fill((T)(object)(Half)0f);
		}
		else
		{
			if (!(typeof(T) == typeof(float)))
			{
				throw new NotSupportedException();
			}
			Xs.Fill((T)(object)0f);
			Ks.Fill((T)(object)1f);
			Bs.Fill((T)(object)0f);
		}
		Clamp.ForEach((IReadOnlyList<int> idx, T _) => (idx[idx.Count - 1] != 0) ? fusedClamp.Max : fusedClamp.Min);
	}

	public ActivationParameter(Tensor<T> act0Tensor)
		: this(2, act0Tensor)
	{
	}

	private ActivationParameter(int sizePreParam)
	{
		_sizePreParam = sizePreParam;
		_xs = null;
		_ks = null;
		_bs = null;
		_clamp = null;
		_data = null;
	}

	private ActivationParameter(int sizePreParam, params int[] shape)
		: this(sizePreParam)
	{
		_data = new Tensor<T>(shape.Concat(new int[1] { SizePerChannel }).ToArray());
	}

	private ActivationParameter(int sizePreParam, Tensor<T> src)
		: this(sizePreParam)
	{
		ReadOnlySpan<int> dimensions = src.Dimensions;
		if (dimensions[dimensions.Length - 1] != SizePerChannel)
		{
			throw new InvalidDataException();
		}
		_data = src.Clone();
	}

	public void FusedScale(float scale)
	{
		Ks.ForEach(delegate(IReadOnlyList<int> _, T k)
		{
			if (k is Half half)
			{
				return (T)(object)(Half)((float)half * scale);
			}
			if (!(k is float num))
			{
				throw new NotSupportedException();
			}
			return (T)(object)(num * scale);
		});
	}

	public void FusedShiftBits(sbyte? shiftBits = null)
	{
		sbyte valueOrDefault = shiftBits.GetValueOrDefault();
		if (!shiftBits.HasValue)
		{
			valueOrDefault = ShiftBits;
			shiftBits = valueOrDefault;
		}
		FusedScale((1 << (int?)shiftBits).Value);
		ShiftBitsAdaptXs((1 << (int?)shiftBits).Value);
	}

	public Tensor<TTo> ToAct0Data<TTo>() where TTo : unmanaged, IEquatable<TTo>
	{
		if (_data.Rank > 4)
		{
			throw new InvalidDataException();
		}
		if (_data.Rank < 4)
		{
			return _data.Reshape(Enumerable.Repeat(1, 4 - _data.Rank).Concat(_data.Dimensions.ToArray()).ToArray()).Cast<TTo>(CastMode.KDefault);
		}
		return _data.Cast<TTo>(CastMode.KDefault);
	}

	private sbyte ComputeShiftBits(T scale)
	{
		float num2;
		if (!(scale is Half half))
		{
			if (!(scale is float num))
			{
				throw new NotSupportedException();
			}
			num2 = num;
		}
		else
		{
			num2 = (float)half;
		}
		float value = num2;
		if ((double)Math.Abs(value) <= 1E-30)
		{
			return 30;
		}
		return checked((sbyte)Math.Log2(65500f / Math.Abs(value)));
	}

	private void ShiftBitsAdaptXs(float scale)
	{
		Xs.ForEach(delegate(IReadOnlyList<int> _, T f)
		{
			if (f is Half half)
			{
				return (T)(object)(Half)((float)half / scale);
			}
			if (!(f is float num))
			{
				throw new NotSupportedException();
			}
			return (T)(object)(num / scale);
		});
	}
}
