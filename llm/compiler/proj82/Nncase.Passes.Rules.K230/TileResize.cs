using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileResize : RewriteRule<Pattern>
{
	private static int _count = -1;

	private readonly List<Tuple<SegmentND, TensorStat>>[] _r2GOfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _ofmapRec = new List<Tuple<SegmentND, TensorStat>>[2];

	public override Pattern Pattern { get; } = FusionPattern.IsAi2DResizeFusion();


	private PrimFunction GetReplace(Call call, Call ld, Call st)
	{
		Ai2dResize resize = (Ai2dResize)call.Target;
		_count++;
		ItemRecStatusInit();
		TileAi2DResizeGlb glb = SearchGlbParameters(ld, st);
		int[] array = ld[GNNELoad.Input].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		BuildSchedule(glb, resize, ld, st, buffer, buffer2, recStat: true);
		List<GnneAction> actions = BuildSchedule(glb, resize, ld, st, buffer, buffer2, recStat: false);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileResize_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, Ai2dResize resize, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf, bool recStat)
	{
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		TileUtilities.Assert(!resize.AlignCorners || !resize.HalfPixelCenters, "!(resize.AlignCorners && resize.HalfPixelCenters)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAi2DResize.cs", 51);
		if (!recStat)
		{
			for (int i = 0; i < _r2GOfRec.Length; i++)
			{
				for (int j = 0; j < _r2GOfRec[i].Count; j++)
				{
					if (j == 0)
					{
						_r2GOfRec[i][j].Item2.IsFirstSlice = true;
					}
					if (j == _r2GOfRec[i].Count - 1)
					{
						_r2GOfRec[i][j].Item2.IsLastSlice = true;
					}
					if (j < _r2GOfRec[i].Count - 1 && _r2GOfRec[i][j].Item1 != _r2GOfRec[i][j + 1].Item1)
					{
						_r2GOfRec[i][j].Item2.IsLastSlice = true;
						_r2GOfRec[i][j + 1].Item2.IsFirstSlice = true;
					}
				}
			}
			for (int k = 0; k < _ofmapRec.Length; k++)
			{
				if (_ofmapRec[k].Count > 0)
				{
					_ofmapRec[k][0].Item2.IsFirstSlice = true;
					_ofmapRec[k][_ofmapRec[k].Count - 1].Item2.IsLastSlice = true;
				}
			}
			if (!recStat)
			{
				gnneActionUpdater.UpdateMmuConf();
			}
		}
		GNNEShape gNNEShape = new GNNEShape(ld.CheckedShape.ToValueArray());
		GNNEShape gNNEShape2 = new GNNEShape(st.CheckedShape.ToValueArray());
		DataType checkedDataType = ld.CheckedDataType;
		DataType checkedDataType2 = st[GNNEStore.Input].CheckedDataType;
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], gNNEShape2[0]);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], gNNEShape2[1]);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], gNNEShape2[2]);
		List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], gNNEShape2[3]);
		int num = 0;
		foreach (Segment1D item in segmentStartEndLength)
		{
			Segment1D segment1D = item;
			foreach (Segment1D item2 in segmentStartEndLength2)
			{
				Segment1D segment1D2 = item2;
				foreach (Segment1D item3 in segmentStartEndLength3)
				{
					Segment1D segment1D3 = new Segment1D(0..gNNEShape[2], Padding.Zero());
					foreach (Segment1D item4 in segmentStartEndLength4)
					{
						Segment1D segment1D4 = new Segment1D(0..gNNEShape[3], Padding.Zero());
						SegmentND segmentND = new SegmentND(item, item2, item3, item4);
						SegmentND segmentND2 = new SegmentND(segment1D, segment1D2, segment1D3, segment1D4);
						Ai2dConfig config = new Ai2dConfig();
						TileUtilities.Ai2dUtilKpuUpdateStaticParam(ld, st, resize, ref config);
						TileUtilities.Ai2dUtilKpuUpdateResizeParam(ld, st, resize, ref config);
						List<int> list2 = new List<int> { 0, 0 };
						TileUtilities.Ai2dUtilResizeSramSearch(config, segmentND, segmentND2, list2);
						int chunkSize = ((checkedDataType == DataTypes.Int16) ? 2 : 4);
						List<Segment1D> segmentStartEndLength5 = TileUtilities.GetSegmentStartEndLength(item2.Start, chunkSize, item2.End);
						List<Segment1D> segmentStartEndLength6 = TileUtilities.GetSegmentStartEndLength(item3.Start, list2[0], item3.End);
						List<Segment1D> segmentStartEndLength7 = TileUtilities.GetSegmentStartEndLength(item4.Start, list2[1], item4.End);
						if (!recStat)
						{
							List<CcrSet> ccrsToSet = new List<CcrSet>
							{
								new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num)), (segmentStartEndLength5.Count * segmentStartEndLength6.Count * segmentStartEndLength7.Count <= 1) ? 1 : 2)
							};
							int offsetD = 0;
							List<int> stridesD = new List<int>
							{
								segmentND2[1].Length,
								segmentND2[2].Length,
								segmentND2[3].Length
							};
							gnneActionUpdater.UpdateLoadIf(segmentND2, ld, num, ddrIf, offsetD, stridesD, ItemName.Ifmap, ccrsToSet);
						}
						List<CcrClr> list3 = new List<CcrClr>();
						int num2 = 0;
						foreach (Segment1D item5 in segmentStartEndLength5)
						{
							bool flag = true;
							foreach (Segment1D item6 in segmentStartEndLength6)
							{
								foreach (Segment1D item7 in segmentStartEndLength7)
								{
									List<CcrSet> list7;
									bool write_all;
									if (!recStat)
									{
										TileUtilities.Ai2dUtilKpuUpdateResizeParam(ld, st, resize, ref config);
										List<float> v_i = new List<float> { item7.Start, item6.Start };
										List<float> v_i2 = new List<float>
										{
											item7.End - 1,
											item6.End - 1
										};
										List<float> mScale = new List<float>
										{
											config.OriginM(config.M0),
											config.OriginM(config.M1),
											config.OriginM(config.M3),
											config.OriginM(config.M4)
										};
										List<float> mBias = new List<float>
										{
											config.OriginM(config.M2),
											config.OriginM(config.M5)
										};
										List<float> list4 = TileUtilities.Ai2dUtilMMulAdd(mScale, mBias, v_i);
										List<float> list5 = TileUtilities.Ai2dUtilMMulAdd(mScale, mBias, v_i2);
										int num3 = Math.Max((int)Math.Floor(list4[0]), 0);
										int num4 = Math.Max((int)Math.Floor(list4[1]), 0);
										int num5 = Math.Min((int)Math.Ceiling(list5[0]), gNNEShape[3] - 1);
										int num6 = Math.Min((int)Math.Ceiling(list5[1]), gNNEShape[2] - 1);
										List<float> v_i3 = new List<float> { 0f, 0f };
										List<float> list6 = TileUtilities.Ai2dUtilMMulAdd(mScale, mBias, v_i3);
										float offset_M = 0f;
										if (num3 != 0)
										{
											offset_M = list4[0] - (float)num3 - list6[0];
										}
										float offset_M2 = 0f;
										if (num4 != 0)
										{
											offset_M2 = list4[1] - (float)num4 - list6[1];
										}
										int num7 = num5 - num3 + 1;
										int num8 = num6 - num4 + 1;
										Segment1D segment1D5 = new Segment1D(num4..(num4 + num8), Padding.Zero());
										Segment1D segment1D6 = new Segment1D(num3..(num3 + num7), Padding.Zero());
										SegmentND ifmap_sram = new SegmentND(segmentND2[0], item5, segment1D5, segment1D6);
										SegmentND segmentND3 = new SegmentND(segmentND[0], item5, item6, item7);
										TileUtilities.Ai2dUtilKpuUpdateDynamicParam(glb, ref config, ifmap_sram, segmentND3, num, segmentND2, segmentND, offset_M, offset_M2, checkedDataType, checkedDataType2);
										int num9 = 0;
										if (TileUtilities.IsFirstSlice(segmentND3, segmentND) || TileUtilities.IsLastSlice(segmentND3, segmentND))
										{
											num9 = 1;
										}
										if (num9 == 1)
										{
											list3.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num))));
										}
										int num10 = 0;
										int num11 = 0;
										Tuple<SegmentND, TensorStat> tuple = _r2GOfRec[num][0];
										_r2GOfRec[num].RemoveAt(0);
										if (tuple.Item2.IsFirstSlice && !_ofmapRec[num][0].Item2.IsFirstSlice)
										{
											num11 = 1;
										}
										if (tuple.Item2.IsLastSlice)
										{
											num10 = 1;
										}
										if (num11 > 0)
										{
											list3.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.OfmapFake, num))));
										}
										list7 = new List<CcrSet>();
										if (num10 > 0)
										{
											list7.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)), 1));
										}
										write_all = false;
										if (flag)
										{
											num2 += 144;
											flag = false;
											write_all = true;
										}
										else
										{
											num2 += 32;
										}
										int num12;
										if (item6 == segmentStartEndLength6[segmentStartEndLength6.Count - 1])
										{
											num12 = ((item7 == segmentStartEndLength7[segmentStartEndLength7.Count - 1]) ? 1 : 0);
										}
										else
										{
											num12 = 0;
										}
										int num13 = num2;
										if (num12 == 0)
										{
											num13 += 32;
										}
										else if (item5 != segmentStartEndLength5[segmentStartEndLength5.Count - 1])
										{
											num13 += 144;
										}
										if (num13 > 1024)
										{
											num2 = 0;
											config.IntrMask = 0u;
										}
										else
										{
											if (item5 == segmentStartEndLength5[segmentStartEndLength5.Count - 1])
											{
												if (item6 == segmentStartEndLength6[segmentStartEndLength6.Count - 1])
												{
													if (item7 == segmentStartEndLength7[segmentStartEndLength7.Count - 1])
													{
														config.IntrMask = 0u;
														goto IL_096a;
													}
												}
											}
											config.IntrMask = 1u;
										}
										goto IL_096a;
									}
									_r2GOfRec[num].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
									continue;
									IL_096a:
									gnneActionUpdater.UpdateAi2dResize(config, list7, list3, write_all);
									if (config.IntrMask == 0)
									{
										list3.Clear();
									}
								}
							}
						}
						if (!recStat)
						{
							bool num14 = !_ofmapRec[num][0].Item2.IsLastSlice;
							_ofmapRec[num].RemoveAt(0);
							List<CcrSet> list8 = new List<CcrSet>();
							if ((num14 ? 1 : 0) > (false ? 1 : 0))
							{
								list8.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.OfmapFake, num)), 1));
							}
							List<CcrClr> ccrsToClr = new List<CcrClr>
							{
								new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)))
							};
							gnneActionUpdater.UpdateStoreT(segmentND, st, num, ddrOf, 0, null, list8, ccrsToClr);
						}
						else
						{
							_ofmapRec[num].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
						}
						num = (num + 1) % 2;
					}
				}
			}
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAi2DResize.cs", 303);
		return list;
	}

	private TileAi2DResizeGlb SearchGlbParameters(Call ld, Call st)
	{
		GNNEShape gNNEShape = new GNNEShape(ld[GNNELoad.Input].CheckedShape.ToValueArray());
		GNNEShape gNNEShape2 = new GNNEShape(st.CheckedShape.ToValueArray());
		int n = 1;
		int num = Math.Min(4, gNNEShape2[1]);
		int num2 = gNNEShape2[2];
		int num3 = gNNEShape2[3];
		int h = gNNEShape[2];
		int w = gNNEShape[3];
		AllocateResult allocateResult = HandleAllocate(ld, st, n, num, h, w, num2, num3);
		if (!allocateResult.IsOk)
		{
			num = 1;
		}
		while (num < gNNEShape2[1])
		{
			int num4 = Math.Min(num + 4, gNNEShape2[1]);
			allocateResult = HandleAllocate(ld, st, n, num4, h, w, num2, num3);
			if (!allocateResult.IsOk)
			{
				break;
			}
			num = num4;
		}
		while (num < gNNEShape2[1])
		{
			int num5 = Math.Min(num + 1, gNNEShape2[1]);
			allocateResult = HandleAllocate(ld, st, n, num5, h, w, num2, num3);
			if (!allocateResult.IsOk)
			{
				break;
			}
			num = num5;
		}
		allocateResult = HandleAllocate(ld, st, n, num, h, w, num2, num3, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAi2DResize.cs", 359);
		GNNEShape gNNEShape3 = new GNNEShape(n, num, num2, num3);
		return new TileAi2DResizeGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape3.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private AllocateResult HandleAllocate(Call ld, Call st, int n, int c, int h, int w, int e, int f, bool isFinal = false)
	{
		BoxPacker boxPacker = new BoxPacker(16);
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		TileUtilities.GetBytesPerElement(ld.CheckedDataType);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, c, h, w }, ld.CheckedDataType, 0);
		int glbNByte = tensorOnGlb.GlbNByte;
		glbNByte = (tensorOnGlb.AllocatedBytes = TileUtilities.GetAlignedNum(glbNByte, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth));
		glbNByte *= GNNEEnv.NPingPongSplit;
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { n, c, e, f }, st.CheckedDataType, 0);
		int glbNByte2 = tensorOnGlb2.GlbNByte;
		glbNByte2 = (tensorOnGlb2.AllocatedBytes = TileUtilities.GetAlignedNum(glbNByte2, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth));
		glbNByte2 *= GNNEEnv.NPingPongSplit;
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			glbNByte / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte2 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(boxPacker);
		if (allocateResult.IsOk)
		{
			tensorOnGlb.Mmu = allocateResult.Items[ItemName.Ifmap];
			tensorOnGlb2.Mmu = allocateResult.Items[ItemName.Ofmap];
		}
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb2);
		allocateResult.GlbMap = dictionary;
		return allocateResult;
	}

	private void ItemRecStatusInit()
	{
		for (int i = 0; i < _r2GOfRec.Length; i++)
		{
			if (_r2GOfRec[i] != null)
			{
				_r2GOfRec[i].Clear();
			}
			else
			{
				_r2GOfRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
		}
		for (int j = 0; j < _ofmapRec.Length; j++)
		{
			if (_ofmapRec[j] != null)
			{
				_ofmapRec[j].Clear();
			}
			else
			{
				_ofmapRec[j] = new List<Tuple<SegmentND, TensorStat>>();
			}
		}
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, ld, st);
	}
}
