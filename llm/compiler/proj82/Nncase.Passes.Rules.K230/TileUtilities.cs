using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public static class TileUtilities
{
	public record MfuRepeat
	{
		public int LeftRepeats
		{
			get
			{
				return _leftRepeats;
			}
			set
			{
				_leftRepeats = value;
			}
		}

		public int ScalarRepeat
		{
			get
			{
				return _scalarRepeat;
			}
			set
			{
				_scalarRepeat = value;
			}
		}

		public int SliceLen
		{
			get
			{
				return _sliceLen;
			}
			set
			{
				_sliceLen = value;
			}
		}

		public int SliceRepeat
		{
			get
			{
				return _sliceRepeat;
			}
			set
			{
				_sliceRepeat = value;
			}
		}

		private int _leftRepeats;

		private int _scalarRepeat;

		private int _sliceLen;

		private int _sliceRepeat;

		public MfuRepeat(int LeftRepeats, int ScalarRepeat, int SliceLen, int SliceRepeat)
		{
			_leftRepeats = LeftRepeats;
			_scalarRepeat = ScalarRepeat;
			_sliceLen = SliceLen;
			_sliceRepeat = SliceRepeat;
		}

		public MfuRepeat()
			: this(0, 0, 0, 0)
		{
		}

		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("LeftRepeats = ");
			builder.Append(LeftRepeats.ToString());
			builder.Append(", ScalarRepeat = ");
			builder.Append(ScalarRepeat.ToString());
			builder.Append(", SliceLen = ");
			builder.Append(SliceLen.ToString());
			builder.Append(", SliceRepeat = ");
			builder.Append(SliceRepeat.ToString());
			return true;
		}

		[CompilerGenerated]
		public void Deconstruct(out int LeftRepeats, out int ScalarRepeat, out int SliceLen, out int SliceRepeat)
		{
			LeftRepeats = this.LeftRepeats;
			ScalarRepeat = this.ScalarRepeat;
			SliceLen = this.SliceLen;
			SliceRepeat = this.SliceRepeat;
		}
	}

	public static void Assert(bool v, [CallerArgumentExpression("v")] string vStr = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
	{
		if (!v)
		{
			throw new InvalidOperationException($"assert({vStr}) error!\n File \"{path}\", line {line} .");
		}
	}

	public static int GetNTcuNeeded(int nOutputRow, int totalNTcu)
	{
		int num = (int)System.Math.Ceiling(1.0 * (double)nOutputRow / (double)totalNTcu);
		return (int)System.Math.Ceiling(1.0 * (double)nOutputRow / (double)num);
	}

	public static List<Segment1D> GetSegmentStartEndLength(int start, int chunkSize, int upperBound)
	{
		List<Segment1D> list = new List<Segment1D>();
		for (int i = start; i < upperBound; i += chunkSize)
		{
			Segment1D item = new Segment1D(i..((i + chunkSize > upperBound) ? upperBound : (i + chunkSize)), new Padding(0, 0));
			list.Add(item);
		}
		return list;
	}

	public static IEnumerable<int> StrideRangeBy(int chunck, int upperBound)
	{
		int acc;
		for (acc = chunck; acc < upperBound; acc = System.Math.Min(acc + chunck, upperBound))
		{
			yield return acc;
		}
		yield return acc;
	}

	public static IEnumerable<Segment1D> SegmentBy(int start, int chunkSize, int upperBound)
	{
		int count = (int)System.Math.Ceiling((float)(upperBound - start) / (float)chunkSize);
		for (int i = 0; i < count; i++)
		{
			yield return new Segment1D((start + i * chunkSize)..(start + System.Math.Min((i + 1) * chunkSize, upperBound)), Padding.Zero(), i);
		}
	}

	public static IEnumerable<Segment1D> SegmentBy(Segment1D seg, int nPart)
	{
		return SegmentBy(seg.Start, (int)System.Math.Ceiling(1.0 * (double)seg.Length / (double)nPart), seg.End);
	}

	public static Segment1D GetInputRowSegment(int outputRowStart, int outputRowLength, int h, int r, int u, int d, in Padding p)
	{
		int num = (r - 1) * d + 1;
		int num2 = outputRowStart * u;
		int num3 = outputRowLength * u + (num - u);
		int num4 = num2 + num3;
		num2 -= p.Before;
		num4 -= p.Before;
		int before = 0;
		int after = 0;
		if (num2 <= 0 && num4 <= 0)
		{
			before = num4 - num2;
			after = 0;
			num2 = 0;
			num4 = 0;
			num3 = 0;
		}
		else if (num2 <= 0 && num4 >= 0 && num4 <= h)
		{
			before = -num2;
			after = 0;
			num2 = 0;
			num3 = num4 - num2;
		}
		else if (num2 <= 0 && h <= num4)
		{
			before = -num2;
			after = num4 - h;
			num2 = 0;
			num4 = h;
			num3 = num4 - num2;
		}
		else if (num2 >= 0 && num4 <= h)
		{
			before = 0;
			after = 0;
			num3 = num4 - num2;
		}
		else if (num2 >= 0 && num2 <= h && h <= num4)
		{
			before = 0;
			after = num4 - h;
			num4 = h;
			num3 = num4 - num2;
		}
		else if (h <= num2)
		{
			before = 0;
			after = num4 - num2;
			num2 = 0;
			num4 = 0;
			num3 = 0;
		}
		num2 = System.Math.Max(System.Math.Min(num2, h - 1), 0);
		num4 = num2 + num3;
		return new Segment1D(num2..num4, new Padding(before, after));
	}

	public static Segment1D GetInputColumnSegment(int outputColumnStart, int outputColumnLength, int w, int s, int u, int d, in Padding p)
	{
		return GetInputRowSegment(outputColumnStart, outputColumnLength, w, s, u, d, in p);
	}

	public static int GetAlignedNum(int value, int alignmentFactor)
	{
		return (int)System.Math.Ceiling(1.0 * (double)value / (double)alignmentFactor) * alignmentFactor;
	}

	public static Segment1D GetShiftStartEndLength(in Segment1D ori, int referStart)
	{
		Segment1D segment1D = new Segment1D(..0, Padding.Zero());
		if (ori.Length > 0)
		{
			segment1D = segment1D with
			{
				Range = (ori.Start - referStart)..(ori.End - referStart)
			};
		}
		return segment1D;
	}

	public static SegmentND GlbTensorIndexShift(in SegmentND ori, in SegmentND refer)
	{
		return new SegmentND(ori.Zip(refer).Select(it =>
			{
				Nncase.TIR.Segment1D first = it.First;
				return GetShiftStartEndLength(in first, it.Second.Start);
			}));
	}

	public static int GetSliceOffsetInTensor(in SegmentND tensor, in SegmentND slice)
	{
		int length = tensor[1].Length;
		int length2 = tensor[2].Length;
		int length3 = tensor[3].Length;
		return (slice[0].Start - tensor[0].Start) * length * length2 * length3 + (slice[1].Start - tensor[1].Start) * length2 * length3 + (slice[2].Start - tensor[2].Start) * length3 + (slice[3].Start - tensor[3].Start);
	}

	public static SegmentND ShiftInputTensor(in SegmentND x, in SegmentND w, int r, int s, int dilationH, int dilationW)
	{
		List<int> list = Shift1DSegment(x[3].Start, x[3].End, x.PadW.Before, x.PadW.After, w[3].Start, w[3].End, s, dilationW);
		List<int> list2 = Shift1DSegment(x[2].Start, x[2].End, x.PadH.Before, x.PadH.After, w[2].Start, w[2].End, r, dilationH);
		Padding padding = new Padding(list2[3], list2[4]);
		Padding padding2 = new Padding(list[3], list[4]);
		return new SegmentND(x[0], x[1], new Segment1D(list2[0]..list2[1], padding), new Segment1D(list[0]..list[1], padding2));
	}

	public static SegmentND ShiftInputTensor(in SegmentND x, in SegmentND w, int r, int s, int strideH, int strideW, int dilationH, int dilationW)
	{
		List<int> list = Shift1DSegment(x[3].Start, x[3].End, x.PadW.Before, x.PadW.After, w[3].Start, w[3].End, s, strideW, dilationW);
		List<int> list2 = Shift1DSegment(x[2].Start, x[2].End, x.PadH.Before, x.PadH.After, w[2].Start, w[2].End, r, strideH, dilationH);
		Padding padding = new Padding(list2[3], list2[4]);
		Padding padding2 = new Padding(list[3], list[4]);
		return new SegmentND(x[0], x[1], new Segment1D(list2[0]..list2[1], padding), new Segment1D(list[0]..list[1], padding2));
	}

	public static List<SegmentND> ShiftDeconvInoutTensor(SegmentND g2LIf, SegmentND l2GOf, SegmentND l2RW, int strideH, int strideW, int dilationH, int dilationW)
	{
		int num = dilationH * l2RW[2].Start - g2LIf.PadH.Before;
		int num2 = l2GOf[2].Start - num;
		int num3 = g2LIf[2].Start;
		if (num > 0)
		{
			num3 = g2LIf[2].Start + (int)System.Math.Ceiling(1.0 * (double)num / (double)strideH);
			num2 = l2GOf[2].Start + (int)(System.Math.Ceiling(1.0 * (double)num / (double)strideH) * (double)strideH - (double)num);
		}
		int num4 = dilationW * l2RW[3].Start - g2LIf.PadW.Before;
		int num5 = l2GOf[3].Start - num4;
		int num6 = g2LIf[3].Start;
		if (num4 > 0)
		{
			num6 = g2LIf[3].Start + (int)System.Math.Ceiling(1.0 * (double)num4 / (double)strideW);
			num5 = l2GOf[3].Start + (int)(System.Math.Ceiling(1.0 * (double)num4 / (double)strideW) * (double)strideW - (double)num4);
		}
		int num7 = l2GOf[2].End - num2;
		int l2 = g2LIf[2].End - num3;
		int num8 = FillZero(l2, strideH) + num2;
		int num9 = g2LIf[2].End;
		if (FillZero(l2, strideH) > num7)
		{
			num9 = num3 + (int)System.Math.Ceiling(1.0 * (double)num7 / (double)strideH);
			num8 = FillZero((int)System.Math.Ceiling(1.0 * (double)num7 / (double)strideH), strideH) + num2;
		}
		int num10 = l2GOf[3].End - num5;
		int l3 = g2LIf[3].End - num6;
		int num11 = num5 + FillZero(l3, strideW);
		int num12 = g2LIf[3].End;
		if (FillZero(l3, strideW) > num10)
		{
			num12 = num6 + (int)System.Math.Ceiling(1.0 * (double)num10 / (double)strideW);
			num11 = num5 + FillZero((int)System.Math.Ceiling(1.0 * (double)num10 / (double)strideW), strideW);
		}
		SegmentND segmentND = new SegmentND(g2LIf[0], g2LIf[1], new Segment1D(num3..num9, Padding.Zero()), new Segment1D(num6..num12, Padding.Zero()));
		SegmentND segmentND2 = new SegmentND(l2GOf[0], l2GOf[1], new Segment1D(num2..num8, Padding.Zero()), new Segment1D(num5..num11, Padding.Zero()));
		if (segmentND[2].Length <= 0)
		{
			segmentND[2] = new Segment1D(g2LIf[2].Start..g2LIf[2].Start, new Padding(1, 0));
			segmentND2[2] = new Segment1D(l2GOf[2].Start..(l2GOf[2].Start + 1), Padding.Zero());
		}
		if (segmentND[3].Length <= 0)
		{
			segmentND[3] = new Segment1D(g2LIf[3].Start..g2LIf[3].Start, new Padding(1, 0));
			segmentND2[3] = new Segment1D(l2GOf[3].Start..(l2GOf[3].Start + 1), Padding.Zero());
		}
		return new List<SegmentND> { segmentND, segmentND2 };
		static int FillZero(int l, int interval)
		{
			return (l - 1) * interval + 1;
		}
	}

	public static int GetTcuIndexBits(int tcuIndex)
	{
		Assert(tcuIndex < 4, "tcuIndex < 4", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 272);
		return 1 << tcuIndex;
	}

	public static Expr GetTcuIndexBits(Expr tcuIndex)
	{
		return Tensors.Cast(Nncase.IR.F.Math.LeftShift(1u, Tensors.Cast(tcuIndex, DataTypes.UInt32)), DataTypes.Int32);
	}

	public static int GetNTcuIndexBits(int nTcu)
	{
		Assert(nTcu >= 1 && nTcu <= 4, "nTcu >= 1 && nTcu <= 4", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 283);
		return (1 << nTcu) - 1;
	}

	public static Expr GetNTcuIndexBits(Expr nTcu)
	{
		return Tensors.Cast(Nncase.IR.F.Math.LeftShift(1u, Tensors.Cast(nTcu, DataTypes.UInt32)), DataTypes.Int32) - 1;
	}

	public static bool IsFirstSlice(SegmentND slice, SegmentND tensor)
	{
		if (slice[0].Start == tensor[0].Start && slice[1].Start == tensor[1].Start && slice[2].Start == tensor[2].Start && slice[3].Start == tensor[3].Start)
		{
			return true;
		}
		return false;
	}

	public static bool IsLastSlice(SegmentND slice, SegmentND tensor)
	{
		if (slice[0].End == tensor[0].End && slice[1].End == tensor[1].End && slice[2].End == tensor[2].End && slice[3].End == tensor[3].End)
		{
			return true;
		}
		return false;
	}

	public static void CheckConvTHeight(int h, int r, int e, int strideH, Padding pH)
	{
		if (pH.Sum() > 0)
		{
			Assert(System.Math.Ceiling(1f * (float)e / (float)strideH).Equals(h), "System.Math.Ceiling(1.0f * e / strideH).Equals(h)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 322);
		}
		else
		{
			Assert(System.Math.Ceiling(1f * (float)(e - (r - 1)) / (float)strideH).Equals(h), "System.Math.Ceiling(1.0f * (e - (r - 1)) / strideH).Equals(h)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 326);
		}
	}

	public static void CheckConvTWidth(int w, int s, int f, int strideW, Padding pW)
	{
		CheckConvTHeight(w, s, f, strideW, pW);
	}

	public static void CheckConvTShape(int h, int r, int e, int strideH, Padding pH, int w, int s, int f, int strideW, Padding pW)
	{
		CheckConvTHeight(h, r, e, strideH, pH);
		CheckConvTWidth(w, s, f, strideW, pW);
	}

	public static int GetConvTImmediateHeight(int h, int strideH, int r)
	{
		return (h - 1) * strideH + r;
	}

	public static int GetConvTImmediateWidth(int w, int strideW, int s)
	{
		return GetConvTImmediateHeight(w, strideW, s);
	}

	public static int GetConvTPaddedH(int e, int r, int strideH)
	{
		return (int)System.Math.Ceiling(1f * (float)(e - r) / (float)strideH) + 1;
	}

	public static int GetConvTPaddedW(int f, int s, int strideW)
	{
		return GetConvTPaddedH(f, s, strideW);
	}

	public static (int I, int J) GetConvTPadHImmE(int h, int strideH, int r, int e)
	{
		int convTImmediateHeight = GetConvTImmediateHeight(h, strideH, r);
		int num;
		if (convTImmediateHeight < e)
		{
			num = GetConvTPaddedH(e, r, strideH);
			convTImmediateHeight = GetConvTImmediateHeight(num, strideH, r);
		}
		else
		{
			num = h;
		}
		return (I: num, J: convTImmediateHeight);
	}

	public static (int I, int J) GetConvTPadWImmF(int w, int strideW, int s, int f)
	{
		return GetConvTPadHImmE(w, strideW, s, f);
	}

	public static Padding CalPadTopBottom(int paddedH, int h)
	{
		Assert(paddedH >= h, "paddedH >= h", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 387);
		int before = (paddedH - h) / 2;
		int after = (int)System.Math.Ceiling((double)(paddedH - h) / 2.0);
		return new Padding(before, after);
	}

	public static Padding CalPadLeftRight(int paddedW, int w)
	{
		return CalPadTopBottom(paddedW, w);
	}

	public static (int I, int J) GetStartEndOfMidCrop(int immLen, int destLen)
	{
		Assert(immLen >= destLen, "immLen >= destLen", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 400);
		int item = (immLen - destLen) / 2;
		int item2 = immLen - (int)System.Math.Ceiling((float)(immLen - destLen) / 2f);
		return (I: item, J: item2);
	}

	public static (int I, int J) GetConvTOutputStartEnd(int oriInputLen, int paddedInputLen, int immLen, int destLen, Padding pad = null, int outputPadding = 0)
	{
		Assert(paddedInputLen >= oriInputLen, "paddedInputLen >= oriInputLen", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 408);
		if (outputPadding == 0)
		{
			if (oriInputLen == paddedInputLen)
			{
				return GetStartEndOfMidCrop(immLen, destLen);
			}
			return (I: 0, J: destLen);
		}
		Assert(immLen + outputPadding == destLen + pad.Sum(), "immLen + outputPadding == destLen + pad.Sum()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 419);
		return (I: pad.Before, J: pad.Before + destLen);
	}

	public static Segment1D GetVnocOutputRowSegment(int inputRowStart, int inputRowLength, int r, int strideH, int padTop)
	{
		int num = inputRowStart + padTop;
		int num2 = num * strideH;
		int num3 = (num + (inputRowLength - 1)) * strideH + (r - 1) + 1;
		return new Segment1D(num2..num3, Padding.Zero());
	}

	public static Segment1D GetVnocOutputRowSegmentGivenRi(int inputRowStart, int inputRowLength, int ri, int strideH, int padTop, int padBottom = 0, int h = 0)
	{
		int num = ((inputRowLength == 0) ? (h + padTop) : (inputRowStart + padTop));
		int num2 = num * strideH + ri;
		int num3 = (num + (inputRowLength - 1) + padBottom) * strideH + ri + 1;
		System.Math.Max(num3 - num2, 0);
		return new Segment1D(num2..num3, new Padding(0, 0));
	}

	public static Segment1D GetVnocOutputColumnSegment(int inputColumnStart, int inputColumnLength, int s, int strideW, int padLeft)
	{
		return GetVnocOutputRowSegment(inputColumnStart, inputColumnLength, s, strideW, padLeft);
	}

	public static Segment1D GetVnocInputRowSegment(int outputRowStart, int outputRowLength, int ddrPadTop, int ddrPadBot, int h, int r, int strideH)
	{
		int num = h - 1;
		int num2 = 0;
		for (int i = 0; i < r; i++)
		{
			Segment1D vnocInputRowSegmentGivenRi = GetVnocInputRowSegmentGivenRi(outputRowStart, outputRowLength, ddrPadTop, ddrPadBot, h, i, strideH);
			if (vnocInputRowSegmentGivenRi.Length > 0)
			{
				num = System.Math.Min(num, vnocInputRowSegmentGivenRi.Start);
				num2 = System.Math.Max(num2, vnocInputRowSegmentGivenRi.End);
			}
		}
		return new Segment1D(num..num2, Padding.Zero());
	}

	public static Segment1D GetVnocInputColSegment(int outputColStart, int outputColLength, int ddrPadLeft, int ddrPadRight, int w, int s, int strideW)
	{
		return GetVnocInputRowSegment(outputColStart, outputColLength, ddrPadLeft, ddrPadRight, w, s, strideW);
	}

	public static Segment1D GetVnocInputRowSegmentGivenRi(int outputRowStart, int outputRowLength, int ddrPadTop, int ddrPadBot, int h, int ri, int strideH)
	{
		int num = System.Math.Max(0, (int)System.Math.Ceiling(1f * (float)(outputRowStart - ri) / (float)strideH));
		int num2 = System.Math.Min(h - 1 + ddrPadTop + ddrPadBot, (outputRowStart + outputRowLength - 1 - ri) / strideH) + 1;
		num -= ddrPadTop;
		num2 -= ddrPadTop;
		int num3 = 0;
		int num4 = 0;
		if (num < 0)
		{
			num3 = System.Math.Abs(num);
			num = 0;
		}
		else if (num > h)
		{
			num3 = 0;
			num = h;
		}
		else
		{
			num3 = 0;
		}
		if (num2 > h)
		{
			num4 = num2 - h;
			num2 = h;
		}
		else if (num2 < 0)
		{
			num4 = 0;
			num2 = 0;
		}
		else
		{
			num4 = 0;
		}
		Assert(num2 - num >= 0, "inputRowLength >= 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 514);
		num = System.Math.Min(h - 1, num);
		return new Segment1D(num..num2, new Padding(num3, num4));
	}

	public static Segment1D GetVnocInputColumnSegmentGivenSi(int outputColumnStart, int outputColumnLength, int padLeft, int padRight, int w, int si, int strideW)
	{
		return GetVnocInputRowSegmentGivenRi(outputColumnStart, outputColumnLength, padLeft, padRight, w, si, strideW);
	}

	public static Segment1D GetDeconvInputRowSegment(int es, int el, int pt, int h, int uh, int r, int dh)
	{
		int num = es - pt;
		int num2 = num + dh * (r - 1) + el;
		pt = 0;
		int num3 = 0;
		if (num < 0)
		{
			pt = -num;
			num = 0;
		}
		else
		{
			pt = (int)System.Math.Ceiling(1f * (float)num / (float)uh) * uh - num;
			num = (int)System.Math.Ceiling(1f * (float)num / (float)uh);
		}
		num2 = (int)System.Math.Ceiling(1f * (float)num2 / (float)uh);
		if (num2 > h)
		{
			num2 = h;
		}
		int num4 = num2 - num;
		num3 = el - (uh * (num4 - 1) + 1) + dh * (r - 1) - pt;
		return new Segment1D(num..num2, new Padding(pt, num3));
	}

	public static Segment1D GetDeconvInputColSegment(int fs, int fl, int pl, int w, int uw, int s, int dw)
	{
		return GetDeconvInputRowSegment(fs, fl, pl, w, uw, s, dw);
	}

	public static int GetBytesPerElement(DataType type)
	{
		if (type == DataTypes.Int8 || type == DataTypes.UInt8)
		{
			return 1;
		}
		if (type == DataTypes.BFloat16 || type == DataTypes.Float16 || type == DataTypes.Int16 || type == DataTypes.UInt16)
		{
			return 2;
		}
		if (type == DataTypes.Float32 || type == DataTypes.UInt32 || type == DataTypes.Int32)
		{
			return 4;
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	public static MfuRepeat GetMfuRepeat(int[] srcShape, int[] destShape)
	{
		if (srcShape.Sum() == 0)
		{
			return new MfuRepeat(1, 1, 1, 1);
		}
		if ((srcShape[2] != 1 && srcShape[2] != destShape[2]) || (srcShape[3] != 1 && srcShape[3] != destShape[3]))
		{
			return new MfuRepeat(1, System.Math.Max(destShape[3] / srcShape[3], 1), srcShape[3], System.Math.Max(destShape[2] / srcShape[2], 1));
		}
		int[] array = AlignBroadcastShape(srcShape, destShape);
		int i = 0;
		List<int> list = new List<int>();
		for (; i < array.Length && (array[i] != destShape[i] || array[i] == 1); i++)
		{
			if (array[i] == 1)
			{
				list.Add(destShape[i]);
				continue;
			}
			throw new NotSupportedException("Unexpected broadcast");
		}
		for (; i < array.Length && array[i] == destShape[i]; i++)
		{
		}
		List<int> list2 = new List<int>();
		for (; i < array.Length && array[i] != destShape[i]; i++)
		{
			if (array[i] == 1)
			{
				list2.Add(destShape[i]);
				continue;
			}
			throw new NotSupportedException("Unexpected broadcast");
		}
		int num = 1;
		for (; i < array.Length; i++)
		{
			if (array[i] == destShape[i])
			{
				num *= array[i];
				continue;
			}
			if (array[i] == 1)
			{
				break;
			}
			throw new NotSupportedException("Unexpected broadcast");
		}
		List<int> list3 = new List<int>();
		for (; i < array.Length; i++)
		{
			if (array[i] == 1)
			{
				list3.Add(destShape[i]);
				continue;
			}
			throw new NotSupportedException("Unexpected broadcast");
		}
		MfuRepeat mfuRepeat = new MfuRepeat();
		mfuRepeat.LeftRepeats = (int)((list.Count == 0) ? 1 : TensorUtilities.GetProduct(list.ToArray()));
		mfuRepeat.ScalarRepeat = (int)TensorUtilities.GetProduct(list3.ToArray());
		mfuRepeat.SliceLen = num;
		mfuRepeat.SliceRepeat = (int)TensorUtilities.GetProduct(list2.ToArray());
		if (mfuRepeat.SliceLen == 1)
		{
			if (srcShape[^1] == 1 && srcShape[^2] == 1)
			{
				if (mfuRepeat.LeftRepeats == 1 && (double)mfuRepeat.SliceRepeat >= System.Math.Pow(2.0, 16.0))
				{
					mfuRepeat.ScalarRepeat = destShape[^1];
					mfuRepeat.SliceRepeat /= destShape[^1];
					return mfuRepeat;
				}
				if ((double)mfuRepeat.LeftRepeats >= System.Math.Pow(2.0, 16.0))
				{
					mfuRepeat.ScalarRepeat = destShape[^1];
					mfuRepeat.SliceRepeat = mfuRepeat.LeftRepeats / destShape[^1];
					mfuRepeat.LeftRepeats = 1;
					return mfuRepeat;
				}
			}
			if ((double)(mfuRepeat.ScalarRepeat * mfuRepeat.SliceRepeat) < System.Math.Pow(2.0, 14.0))
			{
				mfuRepeat.ScalarRepeat *= mfuRepeat.SliceRepeat;
				mfuRepeat.SliceRepeat = 1;
			}
		}
		if ((object)mfuRepeat != null && mfuRepeat.SliceRepeat == 1 && mfuRepeat.SliceLen == 1)
		{
			int num2 = array.Length - 1;
			while (num2 >= 0 && (double)(mfuRepeat.SliceLen * array[num2]) < System.Math.Pow(2.0, 16.0))
			{
				mfuRepeat.SliceLen *= array[num2];
				num2--;
			}
		}
		return mfuRepeat;
	}

	public static Expr Split(Expr length, Expr factor)
	{
		return Tensors.Cast(Nncase.IR.F.Math.Ceil(Tensors.Cast(length, DataTypes.Float32) / Tensors.Cast(factor, DataTypes.Float32)), DataTypes.Int32);
	}

	public static Expr SplitTimes(Expr length, Expr chunck)
	{
		return Tensors.Cast(Nncase.IR.F.Math.Ceil(Tensors.Cast(length, DataTypes.Float32) / Tensors.Cast(chunck, DataTypes.Float32)), DataTypes.Int32);
	}

	public static void Ai2dUtilKpuUpdateStaticParam(Call ld, Call st, Ai2dResize resize, ref Ai2dConfig config)
	{
		int[] array = ld.CheckedShape.ToValueArray();
		int[] array2 = st.CheckedShape.ToValueArray();
		DataType checkedDataType = ld.CheckedDataType;
		config.CscEn = 0u;
		config.CmdId = 1u;
		config.SrcFormat = ((checkedDataType == DataTypes.Int16) ? 5u : 3u);
		config.DstFormat = config.SrcFormat;
		config.CrcInd = 0u;
		config.DstInd = config.CrcInd;
		config.Shift = 0u;
		config.Sign = ((!(ld.CheckedDataType == DataTypes.UInt8)) ? 1u : 0u);
		config.SrcCh0WidthLayout = (uint)((checkedDataType == DataTypes.Int16) ? (array[3] * 2) : array[3]);
		config.SrcCh1WidthLayout = config.SrcCh0WidthLayout;
		config.SrcCh2WidthLayout = config.SrcCh0WidthLayout;
		config.SrcCh3WidthLayout = config.SrcCh0WidthLayout;
		config.DstCh0WidthLayout = (uint)((checkedDataType == DataTypes.Int16) ? (array2[3] * 2) : array2[3]);
		config.DstCh1WidthLayout = config.DstCh0WidthLayout;
		config.DstCh2WidthLayout = config.DstCh0WidthLayout;
		config.DstCh3WidthLayout = config.DstCh0WidthLayout;
	}

	public static void Ai2dUtilKpuUpdateResizeParam(Call ld, Call st, Ai2dResize resize, ref Ai2dConfig config)
	{
		config.Interpolation = ((resize.ResizeMethod != MFU_CROP_RESIZE.NEAREST) ? 1u : 0u);
		if (config.Interpolation == 0 && !resize.AlignCorners)
		{
			config.CordRound = 2u;
		}
		int[] array = ld.CheckedShape.ToValueArray();
		int[] array2 = st.CheckedShape.ToValueArray();
		float num = 1f * (float)array[3] / (float)array2[3];
		float num2 = 1f * (float)array[2] / (float)array2[2];
		float num3 = 0f;
		float num4 = 0f;
		if (config.Interpolation == 0)
		{
			if (resize.HalfPixelCenters)
			{
				num3 = 0.5f * num;
				num4 = 0.5f * num2;
			}
			else if (resize.AlignCorners)
			{
				if (array2[3] > 1)
				{
					num = 1f * (float)(array[3] - 1) / (float)(array2[3] - 1);
				}
				if (array2[2] > 1)
				{
					num2 = 1f * (float)(array[2] - 1) / (float)(array2[2] - 1);
				}
			}
		}
		else if (resize.HalfPixelCenters)
		{
			num3 = 0.5f * num - 0.5f;
			num4 = 0.5f * num2 - 0.5f;
		}
		else if (resize.AlignCorners)
		{
			if (array2[3] > 1)
			{
				num = 1f * (float)(array[3] - 1) / (float)(array2[3] - 1);
			}
			if (array2[2] > 1)
			{
				num2 = 1f * (float)(array[2] - 1) / (float)(array2[2] - 1);
			}
		}
		num *= 1024f;
		num2 *= 1024f;
		num3 *= 1024f;
		num4 *= 1024f;
		FP32 fP = default(FP32);
		fP.U = 0u;
		config.M0 = new FP32
		{
			F = num
		}.U;
		config.M2 = new FP32
		{
			F = num3
		}.U;
		config.M4 = new FP32
		{
			F = num2
		}.U;
		config.M5 = new FP32
		{
			F = num4
		}.U;
		config.BoundInd = 1u;
	}

	public static void Ai2dUtilKpuUpdateDynamicParam(TiledGlb glb, ref Ai2dConfig config, SegmentND ifmap_sram, SegmentND ofmap_sram, int i_pp, SegmentND ifmap, SegmentND ofmap, float offset_M2, float offset_M5, DataType input_type, DataType output_type, int offset_if = 0, int offset_of = 0)
	{
		config.Channel = (uint)ifmap_sram[1].Length;
		config.DstChannel = config.Channel;
		config.SrcHeightShape = (uint)ifmap_sram[2].Length;
		config.SrcWidthShape = (uint)ifmap_sram[3].Length;
		config.DstHeightShape = (uint)ofmap_sram[2].Length;
		config.DstWidthShape = (uint)ofmap_sram[3].Length;
		config.SrcX = (uint)ifmap_sram[3].Start;
		config.SrcY = (uint)ifmap_sram[2].Start;
		config.DstX = (uint)ofmap_sram[3].Start;
		config.DstY = (uint)ofmap_sram[2].Start;
		config.PadT = (uint)ifmap_sram.PadH.Before;
		config.PadB = (uint)ifmap_sram.PadH.After;
		config.PadL = (uint)ifmap_sram.PadW.Before;
		config.PadR = (uint)ifmap_sram.PadW.After;
		int num = ifmap[2].Length * ifmap[3].Length * GetBytesPerElement(input_type);
		config.SrcCh0Ptr = (uint)(i_pp * glb.GlbMap[ItemName.Ifmap].AllocatedBytes + (ifmap_sram[0].Start - ifmap[0].Start) * num * ifmap[1].Length + (ifmap_sram[1].Start - ifmap[1].Start) * num + offset_if);
		config.SrcCh0Ptr += (uint)(glb.GlbMap[ItemName.Ifmap].Mmu.Id << 28);
		config.SrcCh1Ptr = (uint)(config.SrcCh0Ptr + num);
		config.SrcCh2Ptr = (uint)(config.SrcCh1Ptr + num);
		config.SrcCh3Ptr = (uint)(config.SrcCh2Ptr + num);
		int num2 = ofmap[2].Length * ofmap[3].Length * GetBytesPerElement(output_type);
		config.DstCh0Ptr = (uint)(i_pp * glb.GlbMap[ItemName.Ofmap].AllocatedBytes + (ofmap_sram[0].Start - ofmap[0].Start) * num2 * ofmap[1].Length + (ofmap_sram[1].Start - ofmap[1].Start) * num2 + offset_of);
		config.DstCh0Ptr += (uint)(glb.GlbMap[ItemName.Ofmap].Mmu.Id << 28);
		config.DstCh1Ptr = (uint)(config.DstCh0Ptr + num2);
		config.DstCh2Ptr = (uint)(config.DstCh1Ptr + num2);
		config.DstCh3Ptr = (uint)(config.DstCh2Ptr + num2);
		FP32 fP = default(FP32);
		fP.U = config.M2;
		float f = fP.F;
		f += offset_M2 * 1024f;
		fP = new FP32
		{
			F = f
		};
		config.M2 = fP.U;
		fP = default(FP32);
		fP.U = config.M5;
		float f2 = fP.F;
		f2 += offset_M5 * 1024f;
		fP = new FP32
		{
			F = f2
		};
		config.M5 = fP.U;
	}

	public static List<float> Ai2dUtilMMulAdd(List<float> mScale, List<float> mBias, List<float> v_i)
	{
		List<float> list = new List<float>();
		list.Add(0f);
		list.Add(0f);
		list[0] = mScale[0] * v_i[0] + mScale[1] * v_i[1] + mBias[0];
		list[1] = mScale[2] * v_i[0] + mScale[3] * v_i[1] + mBias[1];
		return list;
	}

	public static void Ai2dUtilInvM(List<float> mOriScale, List<float> mOriBias, List<float> mInvScale, List<float> mInvBias)
	{
		float num = mOriScale[0] * mOriScale[3] - mOriScale[1] * mOriScale[2];
		Assert(num != 0f, "det != 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 970);
		mInvScale[0] = mOriScale[3] / num;
		mInvScale[3] = mOriScale[0] / num;
		mInvScale[1] = (0f - mOriScale[1]) / num;
		mInvScale[2] = (0f - mOriScale[2]) / num;
		mInvBias[0] = (0f - mInvScale[0]) * mOriBias[0] - mInvScale[1] * mOriBias[1];
		mInvBias[1] = (0f - mInvScale[2]) * mOriBias[0] - mInvScale[3] * mOriBias[1];
	}

	public static bool Ai2dUtilsTryAllocateResizeSram(List<float> mOriScale, List<float> mOriBias, int dst_max_h, int dst_max_w, SegmentND ifmap, SegmentND ofmap, Ai2dFormat srcFormat)
	{
		List<Segment1D> segmentStartEndLength = GetSegmentStartEndLength(0, dst_max_h, ofmap[2].Length);
		List<Segment1D> segmentStartEndLength2 = GetSegmentStartEndLength(0, dst_max_w, ofmap[3].Length);
		bool flag = true;
		foreach (Segment1D item in segmentStartEndLength)
		{
			foreach (Segment1D item2 in segmentStartEndLength2)
			{
				List<float> v_i = new List<float> { item2.Start, item.Start };
				List<float> v_i2 = new List<float>
				{
					item2.End - 1,
					item.End - 1
				};
				List<float> list = Ai2dUtilMMulAdd(mOriScale, mOriBias, v_i);
				List<float> list2 = Ai2dUtilMMulAdd(mOriScale, mOriBias, v_i2);
				int num = System.Math.Max((int)System.Math.Floor(list[0]), 0);
				int num2 = System.Math.Max((int)System.Math.Floor(list[1]), 0);
				if (srcFormat <= Ai2dFormat.YUV420_I420)
				{
					if (num % 2 == 1)
					{
						num--;
					}
					if (num2 % 2 == 1)
					{
						num2--;
					}
				}
				int num3 = System.Math.Min((int)System.Math.Ceiling(list2[0]), ifmap[3].Length - 1);
				int num4 = System.Math.Min((int)System.Math.Ceiling(list2[1]), ifmap[2].Length - 1);
				int num5 = num3 - num + 1;
				int num6 = num4 - num2 + 1;
				if (srcFormat <= Ai2dFormat.YUV420_I420)
				{
					if (num5 % 2 == 1)
					{
						num3 = System.Math.Min((int)System.Math.Ceiling(list2[0] + 1f), ofmap[3].Length - 1);
						num5 = num3 - num + 1;
						if (num5 % 2 == 1)
						{
							throw new NotSupportedException("YUV420 width error.");
						}
					}
					if (num6 % 2 == 1)
					{
						num6 = System.Math.Min((int)System.Math.Ceiling(list2[1] + 1f), ofmap[2].Length - 1) - num2 + 1;
						if (num6 % 2 == 1)
						{
							throw new NotSupportedException("YUV420 height error.");
						}
					}
				}
				flag = num6 * num5 <= Ai2dSram.SramSize;
				if (!flag)
				{
					return flag;
				}
			}
		}
		return flag;
	}

	public static void Ai2dUtilResizeSramSearch(Ai2dConfig config, SegmentND ofmap, SegmentND ifmap, List<int> ret)
	{
		List<float> mOriScale = new List<float>
		{
			config.OriginM(config.M0),
			config.OriginM(config.M1),
			config.OriginM(config.M3),
			config.OriginM(config.M4)
		};
		List<float> mOriBias = new List<float>
		{
			config.OriginM(config.M2),
			config.OriginM(config.M5)
		};
		List<float> list = new List<float> { 0f, 0f, 0f, 0f };
		List<float> list2 = new List<float> { 0f, 0f };
		Ai2dUtilInvM(mOriScale, mOriBias, list, list2);
		int num = Ai2dSram.SramLen;
		int num2 = Ai2dSram.SramLen;
		if (ifmap[3].Length <= Ai2dSram.SramLen)
		{
			num = ifmap[3].Length;
			num2 = System.Math.Min(Ai2dSram.SramSize / num, ifmap[2].Length);
		}
		else if (ifmap[2].Length <= Ai2dSram.SramLen)
		{
			num2 = ifmap[2].Length;
			num = System.Math.Min(Ai2dSram.SramSize / num2, ifmap[3].Length);
		}
		if (num2 == ifmap[2].Length && num == ifmap[3].Length)
		{
			ret[0] = ofmap[2].Length;
			ret[1] = ofmap[3].Length;
		}
		List<float> v_i = new List<float> { 0f, 0f };
		List<float> v_i2 = new List<float>
		{
			(float)num - 1f,
			(float)num2 - 1f
		};
		List<float> list3 = Ai2dUtilMMulAdd(list, list2, v_i);
		List<float> list4 = Ai2dUtilMMulAdd(list, list2, v_i2);
		int num3 = (int)System.Math.Floor(list4[0]) - (int)System.Math.Ceiling(list3[0]) + 1;
		int num4 = (int)System.Math.Floor(list4[1]) - (int)System.Math.Ceiling(list3[1]) + 1;
		if (config.SrcFormat <= 2)
		{
			if (num4 % 2 == 1)
			{
				num4--;
			}
			if (num3 % 2 == 1)
			{
				num3--;
			}
		}
		bool flag = false;
		int num5 = ((config.SrcFormat > 2) ? 1 : 2);
		while (num4 > 0 && num3 > 0)
		{
			Ai2dFormat srcFormat = (Ai2dFormat)config.SrcFormat;
			if (Ai2dUtilsTryAllocateResizeSram(mOriScale, mOriBias, num4, num3, ifmap, ofmap, srcFormat))
			{
				break;
			}
			if (ifmap[3].Length <= Ai2dSram.SramLen)
			{
				num4 -= num5;
				continue;
			}
			if (ifmap[2].Length <= Ai2dSram.SramLen)
			{
				num3 -= num5;
				continue;
			}
			if (flag)
			{
				if (num3 - 1 <= 0)
				{
					num4 -= num5;
				}
				else
				{
					num3 -= num5;
				}
			}
			else if (num4 - 1 <= 0)
			{
				num3 -= num5;
			}
			else
			{
				num4 -= num5;
			}
			flag = !flag;
		}
		ret[0] = num4;
		ret[1] = num3;
	}

	internal static int SearchAxis(GNNEShape shape, int begin, int limit, Func<int, AllocateResult> handleAlloc)
	{
		while (begin < limit)
		{
			if (handleAlloc(begin + 1).IsOk)
			{
				begin++;
				continue;
			}
			return begin;
		}
		return begin;
	}

	internal static int SearchAxis(GNNEShape shapeA, GNNEShape shapeB, int axis, Func<int, AllocateResult> handleAlloc)
	{
		return SearchAxis(shapeA, shapeA[axis], shapeB[axis], handleAlloc);
	}

	internal static Padding[] GetPadsFromConst(Const paddings)
	{
		if (paddings.CheckedShape.Rank != 2)
		{
			throw new InvalidOperationException($"paddings rank must be 2, but get {paddings.CheckedShape.Rank}");
		}
		Tensor pads = ((TensorConst)paddings).Value;
		return (from i in Enumerable.Range(0, pads.Shape[0].FixedValue)
			select new Padding((int)pads[new int[2] { i, 0 }], (int)pads[new int[2] { i, 1 }])).ToArray();
	}

	private static List<int> Shift1DSegment(int inputStart, int inputEnd, int padBefore, int padAfter, int weightStart, int weightEnd, int weightLength, int dilation)
	{
		int num = inputStart - padBefore + weightStart * dilation;
		int num2 = inputEnd + padAfter - (weightLength - weightEnd) * dilation;
		int num3 = 0;
		int item = 0;
		int item2 = 0;
		if (num2 <= inputStart)
		{
			item = num2 - num;
			item2 = 0;
			num = 0;
			num2 = 0;
			num3 = 0;
		}
		else if (num <= inputStart && inputStart <= num2 && num2 <= inputEnd)
		{
			item = inputStart - num;
			item2 = 0;
			num = inputStart;
			num3 = num2 - num;
		}
		else if (num <= inputStart && inputEnd <= num2)
		{
			item = inputStart - num;
			item2 = num2 - inputEnd;
			num = inputStart;
			num2 = inputEnd;
			num3 = num2 - num;
		}
		else if (inputStart <= num && num2 <= inputEnd)
		{
			item = 0;
			item2 = 0;
			num3 = num2 - num;
		}
		else if (inputStart <= num && num <= inputEnd && inputEnd <= num2)
		{
			item = 0;
			item2 = num2 - inputEnd;
			num2 = inputEnd;
			num3 = num2 - num;
		}
		else if (inputEnd <= num)
		{
			item = 0;
			item2 = num2 - num;
			num = 0;
			num2 = 0;
			num3 = 0;
		}
		else
		{
			Assert(v: false, "false", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 1277);
		}
		num = System.Math.Max(System.Math.Min(num, inputEnd - 1), 0);
		num2 = num + num3;
		return new List<int> { num, num2, num3, item, item2 };
	}

	private static List<int> Shift1DSegment(int inputStart, int inputEnd, int padBefore, int padAfter, int weightStart, int weightEnd, int kernel, int stride, int dilation)
	{
		int num = inputStart - padBefore + weightStart * dilation;
		int num2 = inputEnd + padAfter - (kernel - weightEnd) * dilation;
		int num3 = num2 - (weightEnd - weightStart - 1) * dilation - 1;
		int num4 = 0;
		while (num3 > inputEnd)
		{
			num4++;
			num3 -= stride;
		}
		int item = 0;
		int item2 = 0;
		if (num <= num2 && num2 <= inputStart)
		{
			item = num2 - num;
			item2 = 0;
			num = 0;
			num2 = 0;
		}
		else if (num <= inputStart && inputStart <= num2 && num2 <= inputEnd)
		{
			item = inputStart - num;
			item2 = 0;
			num = inputStart;
		}
		else if (num <= inputStart && inputEnd <= num2)
		{
			int num5 = System.Math.Max(System.Math.Min(num2 - stride * num4, inputEnd), inputStart);
			item = inputStart - num;
			item2 = num2 - num5;
			num = inputStart;
			num2 = num5;
		}
		else if (inputStart <= num && num <= num2 && num2 <= inputEnd)
		{
			item = 0;
			item2 = 0;
		}
		else if (inputStart <= num && num <= inputEnd && inputEnd <= num2)
		{
			int num6 = System.Math.Max(System.Math.Min(num2 - stride * num4, inputEnd), inputStart);
			item = 0;
			item2 = num2 - num6;
			num2 = num6;
		}
		else if (inputEnd <= num && num <= num2)
		{
			item = 0;
			item2 = num2 - num;
			num = 0;
			num2 = 0;
		}
		else
		{
			Assert(v: false, "false", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 1351);
		}
		return new List<int>
		{
			num,
			num2,
			num2 - num,
			item,
			item2
		};
	}

	private static int[] AlignBroadcastShape(int[] srcShape, int[] destShape)
	{
		Assert(srcShape.Length == destShape.Length, "srcShape.Length == destShape.Length", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/TileUtilities.cs", 1366);
		int[] array = srcShape.Where((int dim) => dim != 1).ToArray();
		if (array.Length == 0)
		{
			return Enumerable.Repeat(1, destShape.Length).ToArray();
		}
		int num = array[0];
		int num2 = 0;
		for (int i = 0; i < srcShape.Length; i++)
		{
			if (num == srcShape[i])
			{
				num2 = i;
				break;
			}
		}
		int num3 = srcShape.Length - num2;
		for (int num4 = destShape.Length - num3; num4 >= 0; num4--)
		{
			bool flag = true;
			for (int j = 0; j < num3; j++)
			{
				int num5 = srcShape[num2 + j];
				if (num5 != 1 && num5 != destShape[num4 + j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				List<int> list = Enumerable.Repeat(1, num4).ToList();
				for (int k = 0; k < num3; k++)
				{
					list.Add(srcShape[num2 + k]);
				}
				int num6 = destShape.Length - list.Count;
				for (int l = 0; l < num6; l++)
				{
					list.Add(1);
				}
				return list.ToArray();
			}
		}
		throw new NotSupportedException($"Unexpected broadcast:  [{string.Join(",", srcShape)}] with [{string.Join(",", destShape)}]");
	}
}
