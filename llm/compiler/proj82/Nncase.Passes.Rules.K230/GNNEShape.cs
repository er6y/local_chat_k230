using System.IO;
using System.Linq;

namespace Nncase.Passes.Rules.K230;

public class GNNEShape
{
	private int[] _dims;

	private int[] _shape;

	public int[] Dims
	{
		get
		{
			return _dims;
		}
		set
		{
			_dims = value;
		}
	}

	public int Size => Enumerable.Range(0, Dims.Length).Aggregate(1, (int size, int i) => size * Dims[i]);

	public int N
	{
		get
		{
			return Dims[0];
		}
		set
		{
			Dims[0] = value;
		}
	}

	public int C
	{
		get
		{
			return Dims[1];
		}
		set
		{
			Dims[1] = value;
		}
	}

	public int H
	{
		get
		{
			return Dims[2];
		}
		set
		{
			Dims[2] = value;
		}
	}

	public int W
	{
		get
		{
			return Dims[3];
		}
		set
		{
			Dims[3] = value;
		}
	}

	public int this[int i]
	{
		get
		{
			return Dims[i];
		}
		set
		{
			Dims[i] = value;
		}
	}

	public GNNEShape(int[] dims)
	{
		if (dims.Length > 4)
		{
			throw new InvalidDataException("GNNEShape rank must <= 4, but get shape:" + dims.Aggregate(string.Empty, (string s, int i) => s + i + ","));
		}
		if (dims.Length < 4)
		{
			Dims = Enumerable.Repeat(1, 4 - dims.Length).Concat(dims).ToArray();
		}
		else
		{
			Dims = dims;
		}
	}

	public GNNEShape(int[] shape, int a)
	{
		if (shape.Length > 4)
		{
			throw new InvalidDataException("GNNEShape rank must <= 4, but get shape:" + shape.Aggregate(string.Empty, (string s, int i) => s + i + ","));
		}
		if (shape.Length < 4)
		{
			_shape = Enumerable.Repeat(1, 4 - shape.Length).Concat(shape).ToArray();
		}
		else
		{
			_shape = shape;
		}
	}

	public GNNEShape(int n, int c, int h, int w)
	{
		Dims = new int[4] { n, c, h, w };
	}

	public GNNEShape WithN(int n)
	{
		return WithIndex(0, n);
	}

	public GNNEShape WithC(int c)
	{
		return WithIndex(1, c);
	}

	public GNNEShape WithH(int h)
	{
		return WithIndex(2, h);
	}

	public GNNEShape WithW(int w)
	{
		return WithIndex(3, w);
	}

	public GNNEShape WithIndex(int i, int v)
	{
		int[] dims = Dims;
		dims[i] = v;
		return new GNNEShape(dims);
	}
}
