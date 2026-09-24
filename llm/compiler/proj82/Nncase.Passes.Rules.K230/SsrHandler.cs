using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

public class SsrHandler
{
	private readonly int _nSsr;

	private readonly List<KeyValuePair<SsrKey, int>> _ssrIdx = new List<KeyValuePair<SsrKey, int>>();

	private readonly Dictionary<SsrKey, KeyValuePair<SsrKey, int>> _ssrIdxMap = new Dictionary<SsrKey, KeyValuePair<SsrKey, int>>();

	private readonly Dictionary<SsrKey, long> _ssrValMap = new Dictionary<SsrKey, long>();

	private int _idxToUse;

	private int _nextIdxToAllocate;

	public SsrHandler(int nSsr = 8)
	{
		_nextIdxToAllocate = 0;
		_nSsr = nSsr;
		_idxToUse = 0;
		_ssrIdx.Clear();
		_ssrIdxMap.Clear();
		_ssrValMap.Clear();
	}

	public Ssr GetSsrItem(SsrKey key)
	{
		if (_ssrIdxMap.ContainsKey(key))
		{
			return new Ssr(-1, -1L, needRenewal: false);
		}
		KeyValuePair<SsrKey, int> item = _ssrIdxMap[key];
		_ssrIdx.Remove(_ssrIdxMap[key]);
		_ssrIdx.Insert(0, item);
		_ssrIdxMap[key] = _ssrIdx[0];
		return new Ssr(item.Value, _ssrValMap[key], needRenewal: false);
	}

	public Ssr SetSsrItem(long value, bool forceNew = false)
	{
		SsrKey key = new SsrKey(value);
		bool needRenewal;
		if (!_ssrIdxMap.ContainsKey(key) || forceNew)
		{
			if (_ssrIdx.Count == _nSsr)
			{
				KeyValuePair<SsrKey, int> item = _ssrIdx[_nSsr - 1];
				_idxToUse = item.Value;
				_ssrIdxMap.Remove(item.Key);
				_ssrIdx.Remove(item);
			}
			else
			{
				_idxToUse = _nextIdxToAllocate;
				_nextIdxToAllocate = (_nextIdxToAllocate + 1) % _nSsr;
			}
			needRenewal = true;
		}
		else
		{
			_idxToUse = _ssrIdxMap[key].Value;
			_ssrIdx.Remove(_ssrIdxMap[key]);
			needRenewal = value != _ssrValMap[key];
		}
		_ssrIdx.Insert(0, new KeyValuePair<SsrKey, int>(key, _idxToUse));
		_ssrIdxMap[key] = _ssrIdx[0];
		_ssrValMap[key] = value;
		return new Ssr(_idxToUse, value, needRenewal);
	}
}
