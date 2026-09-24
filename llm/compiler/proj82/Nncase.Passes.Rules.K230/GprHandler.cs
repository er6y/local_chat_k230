using System.Collections.Generic;
using Nncase.IR;

namespace Nncase.Passes.Rules.K230;

public class GprHandler
{
	private readonly List<KeyValuePair<GprKey, int>> _gprIdx = new List<KeyValuePair<GprKey, int>>();

	private readonly Dictionary<GprKey, KeyValuePair<GprKey, int>> _gprIdxMap = new Dictionary<GprKey, KeyValuePair<GprKey, int>>();

	private readonly Dictionary<GprKey, int> _gprValMap = new Dictionary<GprKey, int>();

	private readonly int _nGpr;

	private int _idxToUse;

	private int _nextIdxToAllocate;

	public GprHandler(int nGpr = 31)
	{
		_nGpr = nGpr;
		_idxToUse = 0;
		_nextIdxToAllocate = 1;
		_gprIdx.Clear();
		_gprIdxMap.Clear();
		_gprValMap.Clear();
	}

	public Gpr GetGprItem(GprKey key)
	{
		if (_gprIdxMap.ContainsKey(key))
		{
			return new Gpr(-1, -1, needRenewal: false);
		}
		KeyValuePair<GprKey, int> item = _gprIdxMap[key];
		_gprIdx.Remove(_gprIdxMap[key]);
		_gprIdx.Insert(0, item);
		_gprIdxMap[key] = _gprIdx[0];
		return new Gpr(item.Value, _gprValMap[key], needRenewal: false);
	}

	public Gpr SetGprItem(int value, bool basement = false)
	{
		GprKey key = new GprKey(value, basement);
		if (value == 0 && !basement)
		{
			return new Gpr(0, 0, needRenewal: false);
		}
		bool needRenewal;
		if (!_gprIdxMap.ContainsKey(key) || basement)
		{
			if (_gprIdx.Count == _nGpr)
			{
				KeyValuePair<GprKey, int> item = _gprIdx[_nGpr - 1];
				_idxToUse = item.Value;
				_gprIdxMap.Remove(item.Key);
				_gprIdx.Remove(item);
			}
			else
			{
				_idxToUse = _nextIdxToAllocate;
				_nextIdxToAllocate = (_nextIdxToAllocate + 1) % _nGpr;
				if (_nextIdxToAllocate == 0)
				{
					_nextIdxToAllocate = _nGpr;
				}
			}
			needRenewal = true;
		}
		else
		{
			_idxToUse = _gprIdxMap[key].Value;
			_gprIdx.Remove(_gprIdxMap[key]);
			needRenewal = value != _gprValMap[key];
		}
		_gprIdx.Insert(0, new KeyValuePair<GprKey, int>(key, _idxToUse));
		_gprIdxMap[key] = _gprIdx[0];
		_gprValMap[key] = value;
		if (basement)
		{
			needRenewal = true;
		}
		return new Gpr(_idxToUse, value, needRenewal);
	}

	public Gpr SetGprItem1(Expr value)
	{
		GprKey key = new GprKey(-1, basement: true);
		if (_gprIdx.Count == _nGpr)
		{
			KeyValuePair<GprKey, int> item = _gprIdx[_nGpr - 1];
			_idxToUse = item.Value;
			_gprIdxMap.Remove(item.Key);
			_gprIdx.Remove(item);
		}
		else
		{
			_idxToUse = _nextIdxToAllocate;
			_nextIdxToAllocate = (_nextIdxToAllocate + 1) % _nGpr;
			if (_nextIdxToAllocate == 0)
			{
				_nextIdxToAllocate = _nGpr;
			}
		}
		bool needRenewal = true;
		_gprIdx.Insert(0, new KeyValuePair<GprKey, int>(key, _idxToUse));
		_gprIdxMap[key] = _gprIdx[0];
		_gprValMap[key] = -1;
		return new Gpr(_idxToUse, value, needRenewal);
	}
}
