using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

internal class TilePadGlb : TiledGlb
{
	private TensorOnGlb _ifGlb;

	private TensorOnGlb _ofGlb;

	public TilePadGlb(Dictionary<ItemName, TensorOnGlb> glbMap, Dictionary<ItemName, MmuItem> items, int[] lastOutShape, int nPingPongSplit, TensorOnGlb ifGlb, TensorOnGlb ofGlb)
		: base(glbMap, items, lastOutShape, nPingPongSplit)
	{
		_ifGlb = ifGlb;
		_ofGlb = ofGlb;
	}
}
