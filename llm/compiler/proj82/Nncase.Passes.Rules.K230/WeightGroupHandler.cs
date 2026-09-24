using System;
using System.Collections.Generic;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

public class WeightGroupHandler
{
	private readonly DataType _weightDatatypeDdr;

	private readonly DataType _weightDatatypeGlb;

	private readonly List<SegmentND> _weightGroupSlice = new List<SegmentND>();

	private readonly List<Segment1D> _qargGroupSlice = new List<Segment1D>();

	private readonly List<SegmentND> _dwGroupSlice = new List<SegmentND>();

	private readonly List<Segment1D> _dwQargGroupSlice = new List<Segment1D>();

	private readonly Dictionary<SegmentND, int> _weightGroupOffset = new Dictionary<SegmentND, int>();

	private readonly Dictionary<SegmentND, int> _weightGroupAlignedOffset = new Dictionary<SegmentND, int>(0);

	private readonly Dictionary<SegmentND, int> _weightGroupAlignedGlbOffset = new Dictionary<SegmentND, int>(0);

	private readonly Dictionary<Segment1D, int> _qargAlignedOffset = new Dictionary<Segment1D, int>();

	private readonly Dictionary<SegmentND, int> _dwGroupOffset = new Dictionary<SegmentND, int>();

	private readonly Dictionary<SegmentND, int> _dwGroupAlignedOffset = new Dictionary<SegmentND, int>();

	private readonly Dictionary<Segment1D, int> _dwQargAlignedOffset = new Dictionary<Segment1D, int>();

	private int _currentOffset;

	private int _currentAlignedOffset;

	private int _currentQargOffset;

	private int _currentQargAlignedOffset;

	private int _currentDwOffset;

	private int _currentDwAlignedOffset;

	private int _currentDwQargAlignedOffset;

	public WeightGroupHandler(DataType weightDatatypeDdr, DataType weightDatatypeGlb)
	{
		_weightDatatypeDdr = weightDatatypeDdr;
		_weightDatatypeGlb = weightDatatypeGlb;
		_currentOffset = 0;
		_currentAlignedOffset = 0;
		_currentQargOffset = 0;
		_currentQargAlignedOffset = 0;
		_currentDwOffset = 0;
		_currentDwAlignedOffset = 0;
		_currentDwQargAlignedOffset = 0;
	}

	public void Current_aligned_offset_init()
	{
		_currentAlignedOffset = 0;
	}

	public void UpdateWeightGroup(SegmentND tensor4d)
	{
		if (!_weightGroupSlice.Contains(tensor4d))
		{
			_weightGroupSlice.Add(tensor4d);
			_weightGroupOffset.Add(tensor4d, _currentOffset);
			_weightGroupAlignedOffset.Add(tensor4d, _currentAlignedOffset);
			if (!_qargGroupSlice.Contains(tensor4d[0]))
			{
				_qargGroupSlice.Add(tensor4d[0]);
				_qargAlignedOffset.Add(tensor4d[0], _currentQargAlignedOffset);
				_currentQargOffset += tensor4d[0].Length;
				_currentQargAlignedOffset += (int)Math.Ceiling(1f * (float)tensor4d[0].Length / (float)GNNEEnv.PuWidth) * GNNEEnv.PuWidth;
			}
			_currentOffset += GetShapeSize(tensor4d);
			_currentAlignedOffset += GetAlignedShapeSize(tensor4d);
		}
	}

	public void UpdateDwWeightGroup(SegmentND tensor4d)
	{
		if (!_dwGroupSlice.Contains(tensor4d))
		{
			_dwGroupSlice.Add(tensor4d);
			_dwGroupOffset.Add(tensor4d, _currentDwOffset);
			_dwGroupAlignedOffset.Add(tensor4d, _currentDwAlignedOffset);
			if (!_dwQargGroupSlice.Contains(tensor4d[1]))
			{
				_dwQargGroupSlice.Add(tensor4d[1]);
				_dwQargAlignedOffset.Add(tensor4d[1], _currentDwQargAlignedOffset);
				_currentDwQargAlignedOffset += (int)Math.Ceiling(1f * (float)tensor4d[1].Length / (float)GNNEEnv.PuWidth) * GNNEEnv.PuWidth;
			}
			_currentDwOffset += GetShapeSize(tensor4d, GNNEEnv.PuWidth);
			_currentDwAlignedOffset += GetAlignedShapeSizeDw(tensor4d);
		}
	}

	public void UpdateWeightGroupOnGlb(SegmentND tensor4d, int offset)
	{
		if (!_weightGroupAlignedGlbOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		if (!_weightGroupAlignedGlbOffset.ContainsKey(tensor4d))
		{
			_weightGroupAlignedGlbOffset.Add(tensor4d, offset);
		}
	}

	public int WeightGroupOffset(SegmentND tensor4d)
	{
		if (!_weightGroupOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		return _weightGroupOffset[tensor4d];
	}

	public int WeightGroupAlignedOffset(SegmentND tensor4d)
	{
		if (!_weightGroupAlignedOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		return _weightGroupAlignedOffset[tensor4d];
	}

	public int WeightGroupAlignedGlbOffset(SegmentND tensor4d)
	{
		if (!_weightGroupAlignedGlbOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		return _weightGroupAlignedGlbOffset[tensor4d];
	}

	public int DwGroupOffset(SegmentND tensor4d)
	{
		if (!_dwGroupOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		return _dwGroupOffset[tensor4d];
	}

	public int DwGroupAlignedOffset(SegmentND tensor4d)
	{
		if (!_dwGroupAlignedOffset.ContainsKey(tensor4d))
		{
			throw new KeyNotFoundException();
		}
		return _dwGroupAlignedOffset[tensor4d];
	}

	public int QargAlignedOffset(Segment1D mSeg)
	{
		if (!_qargAlignedOffset.ContainsKey(mSeg))
		{
			throw new KeyNotFoundException();
		}
		return _qargAlignedOffset[mSeg];
	}

	public int DwQargAlignedOffset(Segment1D mSeg)
	{
		if (!_dwQargAlignedOffset.ContainsKey(mSeg))
		{
			throw new KeyNotFoundException();
		}
		return _dwQargAlignedOffset[mSeg];
	}

	public List<SegmentND> WeightGroupSlice()
	{
		return _weightGroupSlice;
	}

	public List<Segment1D> QargGroupSlice()
	{
		return _qargGroupSlice;
	}

	public List<SegmentND> DwGroupSlice()
	{
		return _dwGroupSlice;
	}

	public List<Segment1D> DwQargGroupSlice()
	{
		return _dwQargGroupSlice;
	}

	public int CurrentOffset()
	{
		return _currentOffset;
	}

	public int CurrentAlignedOffset()
	{
		return _currentAlignedOffset;
	}

	public int CurrentQargAlignedOffset()
	{
		return _currentQargAlignedOffset;
	}

	public int CurrentQargOffset()
	{
		return _currentQargOffset;
	}

	public int GetShapeSize(SegmentND tensor4d, int align = 1)
	{
		return tensor4d[0].Length * (int)Math.Ceiling(1f * (float)tensor4d[1].Length / (float)align) * align * tensor4d[2].Length * tensor4d[3].Length;
	}

	public int GetAlignedShapeSize(SegmentND tensor4d)
	{
		return GetShapeSize(tensor4d, GNNEEnv.WAlignNum);
	}

	public int GetAlignedShapeSizeDw(SegmentND tensor4d)
	{
		return GetShapeSize(tensor4d, GNNEEnv.PuWidth);
	}

	public int WeightBytesPerElementDdr()
	{
		return TileUtilities.GetBytesPerElement(_weightDatatypeDdr);
	}

	public int WeightBytesPerElementGlb()
	{
		return TileUtilities.GetBytesPerElement(_weightDatatypeGlb);
	}
}
