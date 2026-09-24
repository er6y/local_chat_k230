using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nncase.Passes.Rules.K230;

internal class BoxPacker
{
	private readonly ILogger<BoxPacker> _logger = CompileSessionScope.GetCurrentThrowIfNull().GetRequiredService<ILogger<BoxPacker>>();

	private readonly int _width;

	private readonly int _height;

	private readonly int _nMaxMmuNum;

	private readonly int[,] _space;

	private readonly List<MmuItem> _items;

	private List<BoxOnGlb> _boxes;

	public List<BoxOnGlb> Boxes
	{
		get
		{
			return _boxes;
		}
		set
		{
			_boxes = value;
		}
	}

	public BoxPacker(int maxMmuNum)
		: this(GNNEEnv.GlbWidth, GNNEEnv.GlbDepth, maxMmuNum)
	{
	}

	public BoxPacker(int width, int height, int nMaxMmuNum)
	{
		_width = width;
		_height = height;
		_nMaxMmuNum = nMaxMmuNum;
		Boxes = new List<BoxOnGlb>();
		_space = new int[height, width];
		_items = new List<MmuItem>();
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				_space[i, j] = -1;
			}
		}
	}

	public void Add(BoxOnGlb box)
	{
		Boxes.Add(box);
	}

	public MmuItem Allocate_item(int width, int height)
	{
		TileUtilities.Assert(width != 0, "width != 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 1045);
		if (height == 0)
		{
			height = 1;
		}
		if (height > GNNEEnv.GlbDepth || width > GNNEEnv.GlbWidth)
		{
			return new MmuItem(0, 0, 0, 0, 0);
		}
		int[] array = Find_space_(width, height);
		int id = GetId();
		if (id >= 0 && array[0] >= 0 && array[1] >= 0)
		{
			MmuItem mmuItem = new MmuItem(id, array[0], width, array[1], height);
			FillSpace(mmuItem);
			_items.Add(mmuItem);
			return mmuItem;
		}
		return new MmuItem(0, 0, 0, 0, 0);
	}

	public void DisplayFinalAllocation()
	{
		_logger.LogTrace("--------------------------");
		_logger.LogTrace("GLB allocation:");
		for (int i = 0; i < _nMaxMmuNum; i++)
		{
			int num = -1;
			int num2 = -1;
			int num3 = _width;
			int num4 = _height;
			for (int j = 0; j < _height; j++)
			{
				for (int k = 0; k < _width; k++)
				{
					if (_space[j, k] == i)
					{
						num = k;
						num2 = j;
						break;
					}
				}
				if (num >= 0)
				{
					break;
				}
			}
			if (num < 0)
			{
				continue;
			}
			for (int l = num2 + 1; l < _height; l++)
			{
				if (_space[l, num] != i)
				{
					num4 = l;
					break;
				}
			}
			for (int m = num + 1; m < _width; m++)
			{
				if (_space[num2, m] != i)
				{
					num3 = m;
					break;
				}
			}
			_logger.LogTrace($"{_space[num2, num]} : ({num}, {num3}, {num3 - num}) ({num2}, {num4}, {num4 - num2} )");
		}
		_logger.LogTrace("--------------------------");
	}

	private int[] Find_space_(int width, int height)
	{
		for (int i = 0; i < _height - height + 1; i++)
		{
			for (int j = 0; j < _width - width + 1; j++)
			{
				if (_space[i, j] != -1)
				{
					continue;
				}
				bool flag = true;
				for (int k = 0; k < height; k++)
				{
					for (int l = 0; l < width; l++)
					{
						if (_space[i + k, j + l] != -1)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					return new int[2] { j, i };
				}
			}
		}
		return new int[2] { -1, -1 };
	}

	private int GetId()
	{
		if (_items.Count == 0)
		{
			return 0;
		}
		int i;
		for (i = 0; i < _nMaxMmuNum; i++)
		{
			if (_items.All((MmuItem t) => i != t.Id))
			{
				return i;
			}
		}
		return -1;
	}

	private void FillSpace(MmuItem item)
	{
		for (int i = item.StartDepth; i < item.StartDepth + item.Depth; i++)
		{
			for (int j = item.StartBank; j < item.StartBank + item.Width; j++)
			{
				_space[i, j] = item.Id;
			}
		}
	}
}
