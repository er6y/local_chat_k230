using System;
using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

public class TiledGlb
{
	private Dictionary<ItemName, TensorOnGlb> _glbMap;

	private Dictionary<ItemName, MmuItem> _items;

	private int[] _lastOutShape;

	private int _nPingPongSplit;

	public Dictionary<ItemName, TensorOnGlb> GlbMap
	{
		get
		{
			return _glbMap;
		}
		set
		{
			_glbMap = value ?? throw new ArgumentNullException("value");
		}
	}

	public Dictionary<ItemName, MmuItem> Items
	{
		get
		{
			return _items;
		}
		set
		{
			_items = value ?? throw new ArgumentNullException("value");
		}
	}

	public int[] LastOutShape
	{
		get
		{
			return _lastOutShape;
		}
		set
		{
			_lastOutShape = value ?? throw new ArgumentNullException("value");
		}
	}

	public int NPingPongSplit
	{
		get
		{
			return _nPingPongSplit;
		}
		set
		{
			_nPingPongSplit = value;
		}
	}

	public TiledGlb(Dictionary<ItemName, TensorOnGlb> glbMap, Dictionary<ItemName, MmuItem> items, int[] lastOutShape, int nPingPongSplit)
	{
		GlbMap = glbMap;
		Items = items;
		LastOutShape = lastOutShape;
		NPingPongSplit = nPingPongSplit;
	}

	public TiledGlb()
	{
		GlbMap = new Dictionary<ItemName, TensorOnGlb>();
		Items = new Dictionary<ItemName, MmuItem>();
		LastOutShape = Array.Empty<int>();
		NPingPongSplit = 1;
	}
}
