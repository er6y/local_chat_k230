using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

internal static class SpaceSearcher
{
	public static int GetBasementSize()
	{
		return 512;
	}

	public static int GetTensorSize(int d0, int d1, int d2, int d3, int bytesPerElement)
	{
		return d0 * d1 * d2 * d3 * bytesPerElement;
	}

	public static int GetWeightSize(int r, int s, int c, int m, int bytesPerElement)
	{
		return r * s * (int)Math.Ceiling(1f * (float)c / (float)GNNEEnv.WAlignNum) * GNNEEnv.WAlignNum * m * bytesPerElement;
	}

	public static int GetActSize(int c, int nActParam, int bytesPerElement)
	{
		return c * nActParam * bytesPerElement;
	}

	public static int GetQargSize(int c, int bytesPerElement)
	{
		return c * bytesPerElement;
	}

	public static int GetWQargSize(int m, int bytesPerElement)
	{
		return (int)Math.Ceiling(1f * (float)m / (float)GNNEEnv.PuWidth) * GNNEEnv.PuWidth * bytesPerElement;
	}

	public static int GetInputHeight(int e, int inH, int r, int outH, int uh, int dh, in Padding p)
	{
		List<Segment1D> list = TileUtilities.SegmentBy(0, e, outH).ToList();
		int num = 0;
		foreach (Segment1D item in list)
		{
			int length = TileUtilities.GetInputRowSegment(item.Start, e, inH, r, uh, dh, in p).Length;
			if (length > num)
			{
				num = length;
			}
		}
		return num;
	}

	public static int GetInputWidth(int f, int inW, int s, int outW, int uw, int dw, in Padding p)
	{
		return GetInputHeight(f, inW, s, outW, uw, dw, in p);
	}

	public static int GetDeconvInputHeight(int e, int inH, int r, int outH, int uh, int dh, Padding p)
	{
		List<Segment1D> list = TileUtilities.SegmentBy(0, e, outH).ToList();
		int num = 0;
		foreach (Segment1D item in list)
		{
			int length = TileUtilities.GetDeconvInputRowSegment(item.Start, item.Length, p.Before, inH, uh, r, dh).Length;
			if (length > num)
			{
				num = length;
			}
		}
		return num;
	}

	public static int GetDeconvInputWidth(int f, int inW, int s, int outW, int uw, int dw, Padding p)
	{
		return GetDeconvInputHeight(f, inW, s, outW, uw, dw, p);
	}

	public static AllocateResult TryAllocate(BoxPacker bp)
	{
		ReadOnlyCollection<BoxOnGlb> boxes = bp.Boxes.AsReadOnly();
		int[] v = boxes.Select((BoxOnGlb b) => b.Box[1]).ToArray();
		int[] boxOrderByDepth = SortIndexes(v);
		boxOrderByDepth.Select((int i) => boxes[i]).ToArray();
		int[] array = (from i in SortIndexes(boxes.Select((BoxOnGlb b) => b.Box[0]).ToArray())
			select boxOrderByDepth[i]).ToArray();
		List<int> list = array.ToList();
		int[] array2 = array;
		foreach (int num in array2)
		{
			if (bp.Boxes[num].OwnerName == ItemName.Basement)
			{
				list.Remove(num);
				list.Insert(0, num);
				break;
			}
		}
		array = list.ToArray();
		Dictionary<ItemName, MmuItem> dictionary = new Dictionary<ItemName, MmuItem>();
		bool isOk = true;
		for (int k = 0; k < boxes.Count; k++)
		{
			MmuItem mmuItem = bp.Allocate_item(boxes[array[k]].Box[0], boxes[array[k]].Box[1]);
			if (mmuItem != null && mmuItem.Depth == 0 && mmuItem.Width == 0)
			{
				isOk = false;
				break;
			}
			dictionary[boxes[array[k]].OwnerName] = mmuItem;
		}
		return new AllocateResult
		{
			IsOk = isOk,
			Items = dictionary,
			Boxes = bp.Boxes
		};
	}

	private static int[] SortIndexes<T>(T[] v) where T : unmanaged, IEquatable<T>
	{
		T[] v2 = v;
		List<int> list = Enumerable.Range(0, v2.Length).ToList();
		list.Sort((int i1, int i2) => Comparer<T>.Default.Compare(v2[i2], v2[i1]));
		return list.ToArray();
	}
}
