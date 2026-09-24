using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

public class CcrHandler
{
	private readonly List<Ccr> _ccrs;

	private readonly List<int> _nonUsedItem;

	private readonly Dictionary<int, string> _itemToSpace;

	private readonly Dictionary<string, int> _spaceToItem;

	private int _nCcr = 64;

	public CcrHandler()
	{
		_ccrs = new List<Ccr>();
		_nonUsedItem = new List<int>();
		_spaceToItem = new Dictionary<string, int>();
		_itemToSpace = new Dictionary<int, string>();
		for (int i = 0; i < _nCcr; i++)
		{
			_ccrs.Add(new Ccr(i));
			_nonUsedItem.Add(i);
		}
	}

	public string GetName(ItemName itemName, int id = -1)
	{
		return $"{itemName}" + ((id >= 0) ? $"_{id}" : string.Empty);
	}

	public int GetCcrItem(string space)
	{
		if (_spaceToItem.TryGetValue(space, out var value))
		{
			return value;
		}
		value = GetNonUsedItem();
		_spaceToItem[space] = value;
		_itemToSpace[value] = space;
		return value;
	}

	public void FreeCcrItem(int item)
	{
		string key = _itemToSpace[item];
		_itemToSpace.Remove(item);
		_spaceToItem.Remove(key);
		_nonUsedItem.Add(item);
	}

	public void SetItem(int item, int value)
	{
		_ccrs[item].Value = value;
	}

	public void SetItems(List<CcrSet> ccrsToSet)
	{
		foreach (CcrSet item in ccrsToSet)
		{
			_ccrs[item.Ccr].Value = item.Value;
		}
	}

	public void AcquireItem(int item)
	{
		_ccrs[item].Acquire();
	}

	public int GetValue(int item)
	{
		return _ccrs[item].Value;
	}

	public void ClearItem(int item)
	{
		_ccrs[item].Clear();
	}

	public void ClearItems(List<CcrClr> ccrsToClr)
	{
		foreach (CcrClr item in ccrsToClr)
		{
			_ccrs[item.Ccr].Clear();
		}
	}

	public bool CcrSanityCheck()
	{
		bool result = true;
		for (int i = 0; i < _nCcr; i++)
		{
			if (_ccrs[i].Value != 0)
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private int GetNonUsedItem()
	{
		int result = _nonUsedItem[0];
		_nonUsedItem.Remove(_nonUsedItem[0]);
		return result;
	}
}
