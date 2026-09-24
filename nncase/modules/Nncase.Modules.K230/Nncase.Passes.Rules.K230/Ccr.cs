using System;

namespace Nncase.Passes.Rules.K230;

internal class Ccr
{
	private int _item;

	private int _value;

	public int Item => _item;

	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			TileUtilities.Assert(_value == 0, "_value == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 1240);
			if (_value != 0)
			{
				throw new ArgumentOutOfRangeException($"[Error] in ccr set, ccr id:{_item}");
			}
			_value = value;
		}
	}

	public Ccr(int item)
	{
		_item = item;
		_value = 0;
	}

	public void Clear()
	{
		if (_value <= 0)
		{
			throw new ArgumentOutOfRangeException($"[Error] in ccr clr, ccr id:{_item}");
		}
		_value--;
	}

	public void Acquire()
	{
		TileUtilities.Assert(_value > 0, "_value > 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 1262);
	}
}
