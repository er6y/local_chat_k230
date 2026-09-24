using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.Buffers;
using Nncase.IR.K230;
using Nncase.IR.Tensors;
using Nncase.Passes.Rules.K230;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.Tile;

internal sealed class FusionConvertVisitor
{
	public record GlbItemSize
	{
		public int BasementSize { get; set; }

		public int MaxOfmapSize { get; set; }

		public int MaxWeightSize { get; set; }

		public int MaxActSize { get; set; }

		public int MaxWeightQargSize { get; set; }

		public int MaxDwWeightSize { get; set; }

		public int MaxDwActSize { get; set; }

		public int MaxDwWeightQargSize { get; set; }

		public int MaxIfmap2Size { get; set; }

		public int MaxAct1Size { get; set; }

		public int MaxPdp0ActSize { get; set; }

		public int MaxWeightPreloadSize { get; set; }

		public int SumSize => BasementSize + MaxOfmapSize + MaxWeightSize + MaxActSize + MaxWeightQargSize + MaxDwWeightSize + MaxDwActSize + MaxDwWeightQargSize + MaxIfmap2Size + MaxAct1Size + MaxPdp0ActSize + MaxWeightPreloadSize;
	}

	private readonly TileOptions _tileOptions;

	private Fusion? _currentFusion;

	private int _ofBufNum = 2;

	private bool _containNotSupportedOp;

	private RunPassContext _passOptions;

	public RunPassContext PassOptions
	{
		get
		{
			return _passOptions;
		}
		set
		{
			_passOptions = value;
		}
	}

	public Fusion CurrentFusion => _currentFusion;

	public FusionConvertVisitor(RunPassContext passOptions, TileOptions tileOptions)
	{
		PassOptions = passOptions;
		_tileOptions = tileOptions;
	}

	public bool TryL1Fuse(Fusion fusion)
	{
		List<NodeInfo> list;
		if ((object)_currentFusion == null)
		{
			_currentFusion = fusion;
			list = new List<NodeInfo>();
			Visit(fusion.Body, list);
			if (_containNotSupportedOp)
			{
				return false;
			}
			if (list.Count == 4)
			{
				Call op = list[1].Op;
				if ((object)op == null || !(op.Target is GNNEPdp0DW))
				{
					op = list[1].Op;
					if ((object)op == null || !(op.Target is GNNEPdp0Reduce))
					{
						goto IL_009b;
					}
				}
				op = list[2].Op;
				if ((object)op != null && op.Target is GNNEConv2D)
				{
					return true;
				}
			}
			goto IL_009b;
		}
		throw new InvalidOperationException("Can't Visit More Than One Fusion!");
		IL_009b:
		if (list.Count == 4)
		{
			Call op = list[1].Op;
			if ((object)op != null && op.Target is GNNEActivation)
			{
				op = list[2].Op;
				if ((object)op != null && op.Target is GNNEConv2D)
				{
					if (!((GNNEActivation)list[1].Op.Target).InputFromL1[0] || (object)list[1].Op[GNNEActivation.InputA] != list[2].Op)
					{
						if (((GNNEActivation)list[1].Op.Target).InputFromL1[1])
						{
							return (object)list[1].Op[GNNEActivation.InputB] == list[2].Op;
						}
						return false;
					}
					return true;
				}
			}
		}
		return false;
	}

	public AllocateResult TryL2Fuse(Fusion fusion, FusionType fusionType)
	{
		if ((object)_currentFusion == null)
		{
			_currentFusion = fusion;
			List<NodeInfo> list = new List<NodeInfo>();
			_containNotSupportedOp = false;
			Visit(fusion.Body, list);
			if (_containNotSupportedOp)
			{
				return new AllocateResult
				{
					IsOk = false
				};
			}
			List<NodeInfo> list2 = new List<NodeInfo>();
			for (int i = 0; i < list.Count; i++)
			{
				Call op = list[i].Op;
				if ((object)op != null && op.Target is Reshape && (!(op[Reshape.Input] is Call call) || !(call.Target is GNNEStore) || list[i].Children.Count > 0) && !(op[Reshape.Input] is Var))
				{
					return new AllocateResult
					{
						IsOk = false
					};
				}
				if ((object)op == null || !(op.Target is Reshape))
				{
					list2.Add(list[i]);
				}
			}
			list = list2;
			foreach (NodeInfo item in list.Where(delegate(NodeInfo ni)
			{
				Call op3 = ni.Op;
				return (object)op3 != null && op3.Target is GNNEActivation;
			}))
			{
				Expr expr = item.Op[GNNEActivation.InputA];
				List<NodeInfo> list3 = list;
				if (expr == list3[list3.Count - 1].Op)
				{
					List<NodeInfo> list4 = list;
					if (!list4[list4.Count - 1].Children.Contains(item))
					{
						List<NodeInfo> list5 = list;
						list5[list5.Count - 1].Children.Add(item);
					}
				}
				if (!(item.Op[GNNEActivation.InputB] != None.Default))
				{
					continue;
				}
				Expr expr2 = item.Op[GNNEActivation.InputB];
				List<NodeInfo> list6 = list;
				if (expr2 == list6[list6.Count - 1].Op)
				{
					List<NodeInfo> list7 = list;
					if (!list7[list7.Count - 1].Children.Contains(item))
					{
						List<NodeInfo> list8 = list;
						list8[list8.Count - 1].Children.Add(item);
					}
				}
			}
			UpdateAlignType(list);
			_ofBufNum = (list.Exists((NodeInfo ni) => ni.Children.Count > 1) ? 3 : ((list.Count(delegate(NodeInfo ni)
			{
				Call op2 = ni.Op;
				return (object)op2 != null && op2.Target is GNNELoad;
			}) > 1) ? 3 : 2));
			List<int[]> list9 = new List<int[]>();
			int[] array = fusion.Body.CheckedShape.ToValueArray();
			if (fusion.Body is Call call2 && call2.Target is Reshape)
			{
				array = call2[Reshape.Input].CheckedShape.ToValueArray();
			}
			int num = Math.Min(GNNEEnv.MultiLayerTilingH, array[2]);
			int num2 = Math.Min(GNNEEnv.MultiLayerTilingW, array[3]);
			if (fusionType == FusionType.L2IfFullWPp)
			{
				num = array[2];
				num2 = array[3];
			}
			AllocateResult allocateResult = new AllocateResult
			{
				IsOk = false
			};
			if ((num == 0 || num2 == 0) && (fusionType == FusionType.L2IfSplitWPp || fusionType == FusionType.L2IfSplitWFull))
			{
				double num3 = Math.Floor(0.9 * (double)GNNEEnv.GlbSize);
				if ((double)(array[0] * array[1] * array[2] * array[3] * _ofBufNum * TileUtilities.GetBytesPerElement(list[0].Op[GNNEStore.Input].CheckedDataType)) < num3)
				{
					return allocateResult;
				}
				int num4 = 1;
				int num5 = 1;
				while (num4 < array[2] || num5 < array[3])
				{
					if ((num4 <= num5 && num4 < array[2]) || num5 == array[3])
					{
						num4++;
					}
					else
					{
						num5++;
					}
					num = (int)Math.Ceiling(1f * (float)array[2] / (float)num4);
					num2 = (int)Math.Ceiling(1f * (float)array[3] / (float)num5);
					list9.Add(new int[4]
					{
						array[0],
						array[1],
						num,
						num2
					});
				}
			}
			else
			{
				list9.Add(new int[4]
				{
					array[0],
					array[1],
					num,
					num2
				});
			}
			for (int j = 0; j < list9.Count && j < 3; j++)
			{
				int[] tileOutShape = list9[j];
				List<BoxOnGlb> list10 = new List<BoxOnGlb>();
				GlbItemSize glbItemSize = new GlbItemSize();
				glbItemSize.BasementSize = TileUtilities.GetAlignedNum(SpaceSearcher.GetBasementSize(), GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
				List<NodeInfo> nodeInfo = list;
				Visit(fusionType, tileOutShape, glbItemSize, list);
				list10.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.GlbWidth,
					glbItemSize.BasementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
				}, ItemName.Basement));
				list10.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.OfmapBankWidth,
					glbItemSize.MaxOfmapSize / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
				}, ItemName.Ofmap));
				list10.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.WBankWidth,
					glbItemSize.MaxWeightSize / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
				}, ItemName.Weight));
				list10.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.ActBankWidth,
					glbItemSize.MaxActSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
				}, ItemName.Act));
				if (glbItemSize.MaxDwWeightSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.WBankWidth,
						glbItemSize.MaxDwWeightSize / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.DwWeight));
				}
				if (glbItemSize.MaxDwActSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.ActBankWidth,
						glbItemSize.MaxDwActSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.DwAct1));
				}
				if (glbItemSize.MaxWeightQargSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.WQargBankWidth,
						glbItemSize.MaxWeightQargSize / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.WQarg));
				}
				if (glbItemSize.MaxDwWeightQargSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.WQargBankWidth,
						glbItemSize.MaxDwWeightQargSize / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.DwQarg));
				}
				if (glbItemSize.MaxPdp0ActSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.ActBankWidth,
						glbItemSize.MaxPdp0ActSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.PdpAct1));
				}
				if (glbItemSize.MaxIfmap2Size > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.IfmapBankWidth,
						glbItemSize.MaxIfmap2Size / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.Ifmap2));
				}
				if (glbItemSize.MaxAct1Size > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.ActBankWidth,
						glbItemSize.MaxAct1Size / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.MfuAct1));
				}
				if (glbItemSize.MaxWeightPreloadSize > 0)
				{
					list10.Add(new BoxOnGlb(new int[2]
					{
						GNNEEnv.WBankWidth,
						glbItemSize.MaxWeightPreloadSize / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
					}, ItemName.WeightPreload));
				}
				allocateResult = SpaceSearcher.TryAllocate(new BoxPacker(16)
				{
					Boxes = list10
				});
				allocateResult.LastOutShape = list9[j];
				int value = ((fusionType == FusionType.L2IfFullWPp || fusionType == FusionType.L2IfSplitWPp) ? glbItemSize.MaxWeightSize : 0);
				allocateResult.BufferSize = new Dictionary<ItemName, int>
				{
					{
						ItemName.Ofmap,
						glbItemSize.MaxOfmapSize / _ofBufNum
					},
					{
						ItemName.Weight,
						value
					}
				};
				allocateResult.NodeInfo = nodeInfo;
				if (fusionType == FusionType.L2IfSplitWPp || fusionType == FusionType.L2IfSplitWFull)
				{
					if (glbItemSize.MaxWeightSize > GNNEEnv.GlbSize)
					{
						allocateResult.IsOk = false;
						break;
					}
					if (allocateResult.IsOk)
					{
						break;
					}
				}
			}
			if (allocateResult.IsOk)
			{
				GetFusionInfo(fusion, allocateResult);
			}
			return allocateResult;
		}
		throw new InvalidOperationException("Can't Visit More Than One Fusion!");
	}

	public void Visit(Expr expr, List<NodeInfo> nodeInfos)
	{
		if (expr is Call)
		{
			Call call = expr as Call;
			Expr target = call.Target;
			if (!(target is GNNELoad))
			{
				if (!(target is GNNEStore))
				{
					if (!(target is GNNEConv2D))
					{
						if (!(target is GNNEPdp0DW))
						{
							if (!(target is GNNEPdp0Reduce))
							{
								if (!(target is Uninitialized))
								{
									if (!(target is GNNEPdp1))
									{
										if (!(target is GNNETranspose))
										{
											if (!(target is GNNEActivation))
											{
												if (target is Reshape)
												{
													LowerReshape(call, nodeInfos);
												}
												else
												{
													_containNotSupportedOp = true;
												}
											}
											else if (call[GNNEActivation.InputB] is Call call2 && call2.Target is GNNELoad && call2[GNNELoad.Input] is Call call3 && call3.Target is Reshape)
											{
												_containNotSupportedOp = true;
											}
											else if (call[GNNEActivation.InputB] != None.Default && (!(call[GNNEActivation.InputB] is Call call4) || !(call4.Target is GNNELoad)) && call[GNNEActivation.InputA] is Call call5 && call5.Target is GNNELoad && call5[GNNELoad.Input] is Call call6 && call6.Target is Reshape)
											{
												_containNotSupportedOp = true;
											}
											else
											{
												LowerGnneActivation(call, nodeInfos);
											}
										}
										else
										{
											LowerGnneTranspose(call, nodeInfos);
										}
									}
									else
									{
										LowerGnnePdp1(call, nodeInfos);
									}
								}
								else
								{
									LowerUninitialized(call, nodeInfos);
								}
							}
							else
							{
								LowerGnnePdp0Reduce(call, nodeInfos);
							}
						}
						else
						{
							LowerGNNEPdp0Dw(call, nodeInfos);
						}
					}
					else
					{
						LowerGNNEConv2D(call, nodeInfos);
					}
				}
				else
				{
					LowerGnneStore(call, nodeInfos);
				}
			}
			else
			{
				LowerGNNELoad(call, nodeInfos);
			}
		}
		else
		{
			_containNotSupportedOp = true;
		}
	}

	public void Visit(FusionType fusionType, int[] tileOutShape, GlbItemSize maxSizes, List<NodeInfo> nodesInfo)
	{
		nodesInfo[0].OutShape = tileOutShape;
		foreach (NodeInfo item in nodesInfo)
		{
			Call op = item.Op;
			Expr target = op.Target;
			if (!(target is GNNELoad op2))
			{
				if (!(target is GNNEStore op3))
				{
					if (!(target is GNNEConv2D op4))
					{
						if (!(target is GNNEPdp0DW op5))
						{
							if (!(target is GNNEPdp0Reduce op6))
							{
								if (!(target is Uninitialized op7))
								{
									if (!(target is GNNEPdp1 op8))
									{
										if (!(target is GNNETranspose op9))
										{
											if (!(target is GNNEActivation op10))
											{
												throw new NotImplementedException("Not implemented Op in L2 fuse");
											}
											LowerGnneActivation(op, op10, fusionType, nodesInfo, maxSizes, item);
										}
										else
										{
											LowerGnneTranspose(op, op9, fusionType, nodesInfo, maxSizes, item);
										}
									}
									else
									{
										LowerGnnePdp1(op, op8, fusionType, nodesInfo, maxSizes, item);
									}
								}
								else
								{
									LowerUninitialized(op, op7, fusionType, nodesInfo, maxSizes, item);
								}
							}
							else
							{
								LowerGnnePdp0Reduce(op, op6, fusionType, nodesInfo, maxSizes, item);
							}
						}
						else
						{
							LowerGnnePdp0Dw(op, op5, fusionType, nodesInfo, maxSizes, item);
						}
					}
					else
					{
						LowerGnneConv2D(op, op4, fusionType, nodesInfo, maxSizes, item);
					}
				}
				else
				{
					LowerGnneStore(op, op3, fusionType, nodesInfo, maxSizes, item);
				}
			}
			else
			{
				LowerGnneLoad(op, op2, fusionType, nodesInfo, maxSizes, item);
			}
		}
	}

	public List<NodeInfo> GetFusionInfo(Fusion fusion, AllocateResult allocation)
	{
		List<NodeInfo> list = new List<NodeInfo>();
		List<int> list2 = ((_ofBufNum == 2) ? new int[2] { 0, 1 }.ToList() : new int[3] { 0, 1, 2 }.ToList());
		Dictionary<Call, int> dictionary = new Dictionary<Call, int>();
		List<NodeInfo> list3 = new List<NodeInfo>();
		list3.AddRange(allocation.NodeInfo);
		list3.Reverse();
		using List<NodeInfo>.Enumerator enumerator = list3.GetEnumerator();
		NodeInfo ni;
		for (; enumerator.MoveNext(); list.Add(ni))
		{
			ni = enumerator.Current;
			Call op = ni.Op;
			if ((object)op == null || !(op.Target is GNNEPdp0DW))
			{
				op = ni.Op;
				if ((object)op == null || !(op.Target is GNNEPdp0Reduce))
				{
					op = ni.Op;
					if ((object)op == null || !(op.Target is GNNEActivation) || !(ni.Op[GNNEActivation.InputB] == None.Default) || !((GNNEActivation)ni.Op.Target).InputFromL1[0])
					{
						op = ni.Op;
						if ((object)op != null && op.Target is GNNEActivation && ni.Op[GNNEActivation.InputB] != None.Default && ((GNNEActivation)ni.Op.Target).InputFromL1.Any((bool x) => x))
						{
							op = ni.Op;
							int num = (((object)op != null && op.Target is GNNEActivation && ((GNNEActivation)ni.Op.Target).InputFromL1[1]) ? 1 : 0);
							int index = num;
							NodeInfo preNi2 = list3.Find((NodeInfo nI) => nI.Op == ni.Op.Arguments[index]);
							ni.Nb.OfBufferIndex = preNi2.Nb.OfBufferIndex;
							ni.Nb.OutputsSize = ni.Children.Count;
							ni.Nb.OfmapOffset = preNi2.Nb.OfmapOffset;
							list.Find((NodeInfo nI) => nI.Op == preNi2.Op).Nb.AlignType = ni.Nb.AlignType;
							dictionary.Add(ni.Op, ni.Nb.OutputsSize);
							if (!dictionary.ContainsKey((Call)ni.Op.Arguments[1 - index]))
							{
								continue;
							}
							dictionary[(Call)ni.Op.Arguments[1 - index]]--;
							if (dictionary[(Call)ni.Op.Arguments[1 - index]] == 0)
							{
								list2.Add(list.Find((NodeInfo nI) => nI.Op == (Call)ni.Op.Arguments[1 - index]).Nb.OfBufferIndex);
							}
							continue;
						}
						if (list2.Count == 0)
						{
							allocation.IsOk = false;
							return new List<NodeInfo>();
						}
						int num2 = list2[0];
						list2.RemoveAt(0);
						ni.Nb.OfBufferIndex = num2;
						ni.Nb.OutputsSize = ni.Children.Count;
						ni.Nb.OfmapOffset = num2 * allocation.BufferSize[ItemName.Ofmap];
						dictionary.Add(ni.Op, ni.Nb.OutputsSize);
						op = ni.Op;
						if (((object)op == null || !(op.Target is GNNELoad)) && dictionary.ContainsKey((Call)ni.Op.Arguments[0]))
						{
							dictionary[(Call)ni.Op.Arguments[0]]--;
							if (dictionary[(Call)ni.Op.Arguments[0]] == 0)
							{
								list2.Add(list.Find((NodeInfo nI) => nI.Op == (Call)ni.Op.Arguments[0]).Nb.OfBufferIndex);
							}
						}
						op = ni.Op;
						if ((object)op == null || !(op.Target is GNNEActivation) || !(ni.Op[GNNEActivation.InputB] != None.Default) || !dictionary.ContainsKey((Call)ni.Op.Arguments[1]))
						{
							continue;
						}
						dictionary[(Call)ni.Op.Arguments[1]]--;
						if (dictionary[(Call)ni.Op.Arguments[1]] == 0)
						{
							list2.Add(list.Find((NodeInfo nI) => nI.Op == (Call)ni.Op.Arguments[1]).Nb.OfBufferIndex);
						}
						continue;
					}
				}
			}
			NodeInfo preNi = list3.Find((NodeInfo nI) => nI.Op == ni.Op.Arguments[0]);
			ni.Nb.OfBufferIndex = preNi.Nb.OfBufferIndex;
			ni.Nb.OutputsSize = ni.Children.Count;
			ni.Nb.OfmapOffset = preNi.Nb.OfmapOffset;
			list.Find((NodeInfo nI) => nI.Op == preNi.Op).Nb.AlignType = ni.Nb.AlignType;
			dictionary.Add(ni.Op, ni.Nb.OutputsSize);
		}
		return list;
	}

	public PrimFunction BuildSchedule(FusionType fusionType, FusionInfo fusionInfo)
	{
		return new TileLayerGroup().BuildSchedule(fusionInfo);
	}

	public PrimFunction VisitToPrimFunc(Fusion fusion)
	{
		return T.PrimFunc("nop", K230RtModule.Kind).Body().Build();
	}

	private void UpdateAlignType(List<NodeInfo> nodeInfos)
	{
		foreach (NodeInfo ni in nodeInfos)
		{
			if (ni.Children.Exists(delegate(NodeInfo subNi)
			{
				Call op4 = subNi.Op;
				return (object)op4 != null && op4.Target is GNNEActivation;
			}))
			{
				foreach (NodeInfo item in from child in ni.Children.Where(delegate(NodeInfo child)
					{
						Call op3 = child.Op;
						return (object)op3 != null && op3.Target is GNNEActivation && child.Op[GNNEActivation.InputB] != None.Default && ((GNNEActivation)child.Op.Target).InputFromL1.Any((bool x) => x);
					})
					let index = ((GNNEActivation)child.Op.Target).InputFromL1[0] ? 1 : 0
					where (object)(Call)child.Op.Arguments[index] == ni.Op
					select child)
				{
					_ = item;
					if (ni.Op.CheckedShape[2] == 1)
					{
						ni.Nb.AlignType = AlignedType.FAligned;
					}
					else
					{
						ni.Nb.AlignType = AlignedType.EAligned;
					}
				}
			}
			Call op = ni.Op;
			if ((object)op == null || !(op.Target is GNNEPdp1))
			{
				op = ni.Op;
				if ((object)op == null || !(op.Target is GNNETranspose))
				{
					goto IL_01a7;
				}
			}
			ni.Nb.AlignType = AlignedType.FAligned;
			goto IL_01a7;
			IL_01a7:
			if (ni.Children.Exists(delegate(NodeInfo subNi)
			{
				Call op2 = subNi.Op;
				if ((object)op2 == null || !(op2.Target is GNNEPdp1))
				{
					op2 = subNi.Op;
					if ((object)op2 != null)
					{
						return op2.Target is GNNETranspose;
					}
					return false;
				}
				return true;
			}))
			{
				ni.Nb.AlignType = AlignedType.FAligned;
			}
		}
	}

	private void UpdateNodeInfo(List<NodeInfo> nodeInfos, Call call, NodeInfo currNode = null)
	{
		Call call2 = call;
		int num = nodeInfos.FindIndex((NodeInfo ni) => ni.Op == call2);
		if (num >= 0)
		{
			NodeInfo nodeInfo = nodeInfos[num];
			nodeInfos.RemoveAt(num);
			if (currNode != null && !nodeInfo.Children.Contains(currNode))
			{
				nodeInfo.Children.Add(currNode);
			}
			nodeInfos.Add(nodeInfo);
		}
		else
		{
			nodeInfos.Add(new NodeInfo(call2, (currNode != null) ? new List<NodeInfo> { currNode } : new List<NodeInfo>()));
		}
	}

	private void LowerGNNELoad(Call call, List<NodeInfo> nodeInfos)
	{
		if (!(call[GNNELoad.Input] is TensorConst))
		{
			UpdateNodeInfo(nodeInfos, call);
			if (!(call[GNNELoad.Input] is Var))
			{
				UpdateNodeInfo(nodeInfos, call[GNNELoad.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
				Visit(call[GNNELoad.Input], nodeInfos);
			}
		}
	}

	private void LowerGnneStore(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNEStore.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNEStore.Input], nodeInfos);
	}

	private void LowerGNNEConv2D(Call call, List<NodeInfo> nodeInfos)
	{
		int[] array = call[GNNEConv2D.Input].CheckedShape.ToValueArray();
		int[] array2 = call.CheckedShape.ToValueArray();
		int[] array3 = call[GNNEConv2D.Weights].CheckedShape.ToValueArray();
		int[] source = ((TensorConst)call[GNNEConv2D.Padding]).Value.ToArray<int>();
		int num = ((TensorConst)call[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)call[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
		int num3 = ((TensorConst)call[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
		int num4 = ((TensorConst)call[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
		int num5 = ((TensorConst)call[GNNEConv2D.Groups]).Value.ToScalar<int>();
		if (array[0] > 1 && array2[2] == 1 && array2[3] == 1 && array[2] == 1 && array[3] == 1 && array3[2] == 1 && array3[3] == 1 && num == 1 && num2 == 1 && num3 == 1 && num4 == 1 && source.Sum() == 0 && num5 == 1)
		{
			_containNotSupportedOp = true;
		}
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNEConv2D.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNEConv2D.Input], nodeInfos);
	}

	private void LowerGNNEPdp0Dw(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNEPdp0DW.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNEPdp0DW.Input], nodeInfos);
	}

	private void LowerUninitialized(Call call, List<NodeInfo> nodeInfos)
	{
	}

	private void LowerGnnePdp1(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNEPdp1.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNEPdp1.Input], nodeInfos);
	}

	private void LowerGnnePdp0Reduce(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNEPdp0Reduce.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNEPdp0Reduce.Input], nodeInfos);
	}

	private void LowerGnneTranspose(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		UpdateNodeInfo(nodeInfos, call[GNNETranspose.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
		Visit(call[GNNETranspose.Input], nodeInfos);
	}

	private void LowerGnneActivation(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		NodeInfo currNode = nodeInfos[nodeInfos.Count - 1];
		bool flag = false;
		if (call[GNNEActivation.InputB] != None.Default && call[GNNEActivation.InputB] is Call call2 && call2.Target is GNNEConv2D && ((GNNEActivation)call.Target).InputFromL1[1])
		{
			flag = true;
			UpdateNodeInfo(nodeInfos, call[GNNEActivation.InputB] as Call, currNode);
			Visit(call[GNNEActivation.InputB], nodeInfos);
		}
		if (call[GNNEActivation.InputB] == None.Default || !(call[GNNEActivation.InputA] is Call call3) || !(call3.Target is GNNELoad) || ((((Call)call[GNNEActivation.InputA])[GNNELoad.Input] is Var || (((Call)call[GNNEActivation.InputA])[GNNELoad.Input] is Call call4 && call4.Target is Reshape)) && call[GNNEActivation.InputB] is Call call5 && call5.Target is GNNELoad))
		{
			UpdateNodeInfo(nodeInfos, call[GNNEActivation.InputA] as Call, currNode);
			Visit(call[GNNEActivation.InputA], nodeInfos);
		}
		if (!flag && call[GNNEActivation.InputB] != None.Default && (!(call[GNNEActivation.InputB] is Call call6) || !(call6.Target is GNNELoad) || (((Call)call[GNNEActivation.InputB])[GNNELoad.Input] is Var && call[GNNEActivation.InputA] is Call call7 && call7.Target is GNNELoad && ((Call)call[GNNEActivation.InputA])[GNNELoad.Input] is TensorConst)))
		{
			UpdateNodeInfo(nodeInfos, call[GNNEActivation.InputB] as Call, currNode);
			Visit(call[GNNEActivation.InputB], nodeInfos);
		}
	}

	private void LowerReshape(Call call, List<NodeInfo> nodeInfos)
	{
		UpdateNodeInfo(nodeInfos, call);
		if (!(call[Reshape.Input] is Var))
		{
			UpdateNodeInfo(nodeInfos, call[Reshape.Input] as Call, nodeInfos[nodeInfos.Count - 1]);
			Visit(call[Reshape.Input], nodeInfos);
		}
	}

	private void LowerGnneLoad(Call call, GNNELoad op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] tileOutShape = currNode.OutShape;
		if (!(call2[GNNELoad.Input] is TensorConst))
		{
			int[] array = tileOutShape;
			int num = array[2];
			int num2 = array[3];
			int alignmentFactor = ((TileUtilities.GetBytesPerElement(call2.CheckedDataType) == 1) ? 32 : 16);
			if (currNode.Nb.AlignType == AlignedType.EAligned)
			{
				num = TileUtilities.GetAlignedNum(num, alignmentFactor);
			}
			if (currNode.Nb.AlignType == AlignedType.FAligned)
			{
				num2 = TileUtilities.GetAlignedNum(num2, alignmentFactor);
			}
			int allocatedBytes = new TensorOnGlb(new int[4]
			{
				array[0],
				array[1],
				num,
				num2
			}, call2.CheckedDataType, 0).AllocatedBytes;
			allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
			allocatedBytes *= _ofBufNum;
			if (allocatedBytes > maxSizes.MaxOfmapSize)
			{
				maxSizes.MaxOfmapSize = allocatedBytes;
			}
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNELoad.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = tileOutShape;
		});
	}

	private void LowerGnneStore(Call call, GNNEStore op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] tileOutShape = currNode.OutShape;
		int[] array = tileOutShape;
		int num = array[2];
		int num2 = array[3];
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array[0],
			array[1],
			num,
			num2
		}, call2[GNNEStore.Input].CheckedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEStore.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = tileOutShape;
		});
	}

	private void LowerGnnePdp1(Call call, GNNEPdp1 op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		int[] array = call2[GNNEPdp1.Input].CheckedShape.ToValueArray();
		int[] array2 = call2.CheckedShape.ToValueArray();
		int[] array3 = ((TensorConst)call2[GNNEPdp1.Padding]).Value.ToArray<int>();
		int[] array4 = new int[2]
		{
			array3[0],
			array3[1]
		};
		int[] array5 = new int[2]
		{
			array3[2],
			array3[3]
		};
		int uh = ((TensorConst)call2[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int uh2 = ((TensorConst)call2[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int dh = 1;
		int dh2 = 1;
		int num = ((TensorConst)call2[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)call2[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		_ = call2[GNNEPdp1.Input].CheckedDataType;
		DataType checkedDataType = call2.CheckedDataType;
		int r = num;
		int r2 = num2;
		bool flag = array[2] == num && array[3] == num2 && (array[2] > 16 || array[3] > 64 || array[2] * array[3] > 256);
		if (fusionType != FusionType.L2IfFullWPp && flag)
		{
			maxSizes.MaxOfmapSize = GNNEEnv.GlbSize;
		}
		int[] array6 = outShape;
		int num3 = array6[2];
		if (flag)
		{
			int num4 = ((num > 16) ? 16 : num);
			num3 = (int)Math.Ceiling(1f * (float)array[2] / (float)num4);
		}
		int num5 = array6[3];
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(flag ? DataTypes.Float16 : checkedDataType) == 1) ? 32 : 16);
		int alignedNum = TileUtilities.GetAlignedNum(num5, alignmentFactor);
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array6[0],
			array6[1],
			num3,
			alignedNum
		}, flag ? DataTypes.Float16 : checkedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		int e = num3;
		int inH = array[2];
		int outH = array2[2];
		Padding p = new Padding(array4[0], array4[1]);
		int num6 = SpaceSearcher.GetInputHeight(e, inH, r, outH, uh, dh, in p);
		int inH2 = array[3];
		int outH2 = array2[3];
		p = new Padding(array5[0], array5[1]);
		int num7 = SpaceSearcher.GetInputHeight(num5, inH2, r2, outH2, uh2, dh2, in p);
		if (fusionType == FusionType.L2IfFullWPp)
		{
			int num8 = Math.Min(num6, array[2]);
			num7 = Math.Min(num7, array[3]);
			num6 = num8;
		}
		int[] currentInputShape = new int[4]
		{
			array[0],
			array[1],
			num6,
			num7
		};
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEPdp1.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
	}

	private void LowerGnnePdp0Reduce(Call call, GNNEPdp0Reduce op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		int[] array = call2[GNNEPdp0Reduce.Input].CheckedShape.ToValueArray();
		int[] array2 = call2.CheckedShape.ToValueArray();
		int[] array3 = ((TensorConst)call2[GNNEPdp0Reduce.Padding]).Value.ToArray<int>();
		int[] array4 = new int[2]
		{
			array3[0],
			array3[1]
		};
		int[] array5 = new int[2]
		{
			array3[2],
			array3[3]
		};
		int uh = ((TensorConst)call2[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[0];
		int uh2 = ((TensorConst)call2[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[1];
		int dh = 1;
		int dh2 = 1;
		int num = ((TensorConst)call2[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)call2[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[1];
		_ = call2[GNNEPdp0Reduce.Input].CheckedDataType;
		DataType checkedDataType = call2.CheckedDataType;
		int num3 = array2[1];
		int r = num;
		int r2 = num2;
		int c = num3;
		int[] array6 = outShape;
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(checkedDataType) == 1) ? 32 : 16);
		int num4 = array6[2];
		if (currNode.Nb.AlignType == AlignedType.EAligned)
		{
			num4 = TileUtilities.GetAlignedNum(num4, alignmentFactor);
		}
		int num5 = array6[3];
		if (currNode.Nb.AlignType == AlignedType.FAligned)
		{
			num5 = TileUtilities.GetAlignedNum(num5, alignmentFactor);
		}
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array6[0],
			array6[1],
			num4,
			num5
		}, checkedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		int e = array6[2];
		int inH = array[2];
		int outH = array2[2];
		Padding p = new Padding(array4[0], array4[1]);
		int num6 = SpaceSearcher.GetInputHeight(e, inH, r, outH, uh, dh, in p);
		int e2 = array6[3];
		int inH2 = array[3];
		int outH2 = array2[3];
		p = new Padding(array5[0], array5[1]);
		int num7 = SpaceSearcher.GetInputHeight(e2, inH2, r2, outH2, uh2, dh2, in p);
		if (fusionType == FusionType.L2IfFullWPp)
		{
			int num8 = Math.Min(num6, array[2]);
			num7 = Math.Min(num7, array[3]);
			num6 = num8;
		}
		int[] currentInputShape = new int[4]
		{
			array[0],
			array[1],
			num6,
			num7
		};
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		NodeBuffer nb = currNode.Nb;
		nb.Pdp0ActOffset = maxSizes.MaxPdp0ActSize;
		int alignedNum = TileUtilities.GetAlignedNum(SpaceSearcher.GetActSize(c, GNNEEnv.ActNumPerChan, 2), GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		maxSizes.MaxPdp0ActSize += alignedNum;
		currNode.Nb = nb;
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEPdp0Reduce.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
	}

	private void LowerGnneConv2D(Call call, GNNEConv2D op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		int[] array = call2[GNNEConv2D.Input].CheckedShape.ToValueArray();
		int[] array2 = call2.CheckedShape.ToValueArray();
		int[] array3 = call2[GNNEConv2D.Weights].CheckedShape.ToValueArray();
		int[] array4 = ((TensorConst)call2[GNNEConv2D.Padding]).Value.ToArray<int>();
		int[] source = new int[2]
		{
			array4[0],
			array4[1]
		};
		_ = new int[2]
		{
			array4[2],
			array4[3]
		};
		int uh = ((TensorConst)call2[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
		int uh2 = ((TensorConst)call2[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
		int dh = ((TensorConst)call2[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
		int dh2 = ((TensorConst)call2[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
		int num = ((TensorConst)call2[GNNEConv2D.Groups]).Value.ToScalar<int>();
		DataType checkedDataType = call2.CheckedDataType;
		DataType checkedDataType2 = call2[GNNEConv2D.Weights].CheckedDataType;
		int num2 = array3[0];
		int num3 = array3[1];
		int num4 = array3[2];
		int num5 = array3[3];
		int num6 = num2;
		int[] array5 = outShape;
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(checkedDataType) == 1) ? 32 : 16);
		int num7 = array5[2];
		if (currNode.Nb.AlignType == AlignedType.EAligned)
		{
			num7 = TileUtilities.GetAlignedNum(num7, alignmentFactor);
		}
		int num8 = array5[3];
		if (currNode.Nb.AlignType == AlignedType.FAligned)
		{
			num8 = TileUtilities.GetAlignedNum(num8, alignmentFactor);
		}
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array5[0],
			array5[1],
			num7,
			num8
		}, checkedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		int e = array5[2];
		int inH = array[2];
		int outH = array2[2];
		Padding p = new Padding(0, 0);
		int num9 = SpaceSearcher.GetInputHeight(e, inH, num4, outH, uh, dh, in p);
		if (array3[1] * array3[2] <= GNNEEnv.PuHeight && array3[2] != 1)
		{
			num9 += source.Sum();
		}
		int e2 = array5[3];
		int inH2 = array[3];
		int outH2 = array2[3];
		p = new Padding(0, 0);
		int num10 = SpaceSearcher.GetInputHeight(e2, inH2, num5, outH2, uh2, dh2, in p);
		if (fusionType == FusionType.L2IfFullWPp)
		{
			int num11 = Math.Min(num9, array[2]);
			num10 = Math.Min(num10, array[3]);
			num9 = num11;
		}
		int[] currentInputShape = new int[4]
		{
			array[0],
			(num == 1) ? array3[1] : array[1],
			num9,
			num10
		};
		NodeBuffer nb = currNode.Nb;
		Call op2;
		if (currNode.Children.Count == 1)
		{
			op2 = currNode.Children[0].Op;
			if ((object)op2 != null && op2.Target is GNNEActivation && ((((GNNEActivation)currNode.Children[0].Op.Target).InputFromL1[0] && currNode.Children[0].Op[GNNEActivation.InputA] == call2) || (((GNNEActivation)currNode.Children[0].Op.Target).InputFromL1[1] && currNode.Children[0].Op[GNNEActivation.InputB] == call2)))
			{
				goto IL_044e;
			}
		}
		op2 = currNode.Children[0].Op;
		if ((object)op2 == null || !(op2.Target is GNNEPdp0DW))
		{
			op2 = currNode.Children[0].Op;
			if (((object)op2 == null || !(op2.Target is GNNEPdp0Reduce)) && allocatedBytes > maxSizes.MaxOfmapSize)
			{
				maxSizes.MaxOfmapSize = allocatedBytes;
			}
		}
		goto IL_044e;
		IL_044e:
		new TensorOnGlb(new int[4] { num4, num5, num3, num2 }, checkedDataType2, 0);
		if (fusionType == FusionType.L2IfFullWPp || fusionType == FusionType.L2IfSplitWPp)
		{
			int alignedNum = TileUtilities.GetAlignedNum(SpaceSearcher.GetWeightSize(num4, num5, num3, num2, TileUtilities.GetBytesPerElement(checkedDataType2)), GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
			if (alignedNum > maxSizes.MaxWeightSize)
			{
				maxSizes.MaxWeightSize = alignedNum;
			}
		}
		else
		{
			nb.WeightOffset = maxSizes.MaxWeightSize;
			int alignedNum2 = TileUtilities.GetAlignedNum(SpaceSearcher.GetWeightSize(num4, num5, num3, num2, TileUtilities.GetBytesPerElement(checkedDataType2)), GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
			maxSizes.MaxWeightSize += alignedNum2;
		}
		nb.ActOffset = maxSizes.MaxActSize;
		int alignedNum3 = TileUtilities.GetAlignedNum(SpaceSearcher.GetActSize(num6, GNNEEnv.ActNumPerChan, 2), GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		maxSizes.MaxActSize += alignedNum3;
		if (checkedDataType2 == DataTypes.UInt8 || checkedDataType2 == DataTypes.Int16)
		{
			nb.WeightQargOffset = maxSizes.MaxWeightQargSize;
			int alignedNum4 = TileUtilities.GetAlignedNum(SpaceSearcher.GetWQargSize((int)Math.Ceiling((double)num6 * 1.0 / (double)GNNEEnv.PuHeight) * GNNEEnv.PuWidth, 1), GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			maxSizes.MaxWeightQargSize += alignedNum4;
		}
		currNode.Nb = nb;
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEConv2D.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
	}

	private void LowerGnnePdp0Dw(Call call, GNNEPdp0DW op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		int[] array = call2[GNNEPdp0DW.Input].CheckedShape.ToValueArray();
		int[] array2 = call2.CheckedShape.ToValueArray();
		int[] array3 = call2[GNNEPdp0DW.Weights].CheckedShape.ToValueArray();
		int[] array4 = ((TensorConst)call2[GNNEPdp0DW.Padding]).Value.ToArray<int>();
		int[] source = new int[2]
		{
			array4[0],
			array4[1]
		};
		_ = new int[2]
		{
			array4[2],
			array4[3]
		};
		int uh = ((TensorConst)call2[GNNEPdp0DW.Stride]).Value.ToArray<int>()[0];
		int uh2 = ((TensorConst)call2[GNNEPdp0DW.Stride]).Value.ToArray<int>()[1];
		int dh = ((TensorConst)call2[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[0];
		int dh2 = ((TensorConst)call2[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[1];
		int num = ((TensorConst)call2[GNNEPdp0DW.Groups]).Value.ToScalar<int>();
		DataType checkedDataType = call2.CheckedDataType;
		DataType checkedDataType2 = call2[GNNEPdp0DW.Weights].CheckedDataType;
		array3[0] /= array[1];
		array3[1] = TileUtilities.GetAlignedNum(array[1], GNNEEnv.PuWidth);
		int num2 = array3[0];
		int num3 = array3[1];
		int num4 = array3[2];
		int num5 = array3[3];
		int num6 = num3 * num2;
		int[] array5 = outShape;
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(checkedDataType) == 1) ? 32 : 16);
		int num7 = array5[2];
		if (currNode.Nb.AlignType == AlignedType.EAligned)
		{
			num7 = TileUtilities.GetAlignedNum(num7, alignmentFactor);
		}
		int num8 = array5[3];
		if (currNode.Nb.AlignType == AlignedType.FAligned)
		{
			num8 = TileUtilities.GetAlignedNum(num8, alignmentFactor);
		}
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array5[0],
			array5[1],
			num7,
			num8
		}, checkedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		int e = array5[2];
		int inH = array[2];
		int outH = array2[2];
		Padding p = new Padding(0, 0);
		int num9 = SpaceSearcher.GetInputHeight(e, inH, num4, outH, uh, dh, in p);
		if (array3[1] * array3[2] <= GNNEEnv.PuHeight && array3[2] != 1)
		{
			num9 += source.Sum();
		}
		int e2 = array5[3];
		int inH2 = array[3];
		int outH2 = array2[3];
		p = new Padding(0, 0);
		int num10 = SpaceSearcher.GetInputHeight(e2, inH2, num5, outH2, uh2, dh2, in p);
		if (fusionType == FusionType.L2IfFullWPp)
		{
			int num11 = Math.Min(num9, array[2]);
			num10 = Math.Min(num10, array[3]);
			num9 = num11;
		}
		int[] currentInputShape = new int[4]
		{
			array[0],
			(num == 1) ? array3[1] : array[1],
			num9,
			num10
		};
		NodeBuffer nb = currNode.Nb;
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		new TensorOnGlb(new int[4] { num4, num5, num3, num2 }, checkedDataType2, 0);
		nb.DwWeightOffset = maxSizes.MaxDwWeightSize;
		int alignedNum = TileUtilities.GetAlignedNum(SpaceSearcher.GetWeightSize(num4, num5, TileUtilities.GetAlignedNum(num3, GNNEEnv.PuWidth), num2, TileUtilities.GetBytesPerElement(checkedDataType2)), GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
		maxSizes.MaxDwWeightSize += alignedNum;
		nb.DwActOffset = maxSizes.MaxDwActSize;
		int alignedNum2 = TileUtilities.GetAlignedNum(SpaceSearcher.GetActSize(num6, GNNEEnv.ActNumPerChan, 2), GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		maxSizes.MaxDwActSize += alignedNum2;
		if (checkedDataType2 == DataTypes.UInt8 || checkedDataType2 == DataTypes.Int16)
		{
			nb.DwWeightQargOffset = maxSizes.MaxDwWeightQargSize;
			int alignedNum3 = TileUtilities.GetAlignedNum(SpaceSearcher.GetWQargSize(num6, 1), GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			maxSizes.MaxDwWeightQargSize += alignedNum3;
		}
		currNode.Nb = nb;
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEPdp0DW.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
	}

	private void LowerGnneTranspose(Call call, GNNETranspose op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		if (fusionType != FusionType.L2IfFullWPp)
		{
			maxSizes.MaxOfmapSize = GNNEEnv.GlbSize;
		}
		_ = call2[GNNETranspose.Input].CheckedDataType;
		DataType checkedDataType = call2.CheckedDataType;
		int[] array = outShape;
		int value = array[3];
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(checkedDataType) == 1) ? 32 : 16);
		int alignedNum = TileUtilities.GetAlignedNum(value, alignmentFactor);
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array[0],
			array[1],
			array[2],
			alignedNum
		}, checkedDataType, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		MFU_TRANS_PERMUTE perm = op.Perm;
		int[] array2 = GetInputShapes(array[0], array[1], array[2], array[3], perm);
		int[] currentInputShape = new int[4]
		{
			array2[0],
			array2[1],
			array2[2],
			array2[3]
		};
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNETranspose.Input]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
		static int[] GetInputShapes(int d0, int d1, int d2, int d3, MFU_TRANS_PERMUTE permute)
		{
			return permute switch
			{
				MFU_TRANS_PERMUTE.NCHW => new int[4] { d0, d1, d2, d3 }, 
				MFU_TRANS_PERMUTE.NCWH => new int[4] { d0, d1, d3, d2 }, 
				MFU_TRANS_PERMUTE.NHCW => new int[4] { d0, d2, d1, d3 }, 
				MFU_TRANS_PERMUTE.NHWC => new int[4] { d0, d3, d1, d2 }, 
				MFU_TRANS_PERMUTE.NWCH => new int[4] { d0, d2, d3, d1 }, 
				MFU_TRANS_PERMUTE.NWHC => new int[4] { d0, d3, d2, d1 }, 
				MFU_TRANS_PERMUTE.CNHW => new int[4] { d1, d0, d2, d3 }, 
				MFU_TRANS_PERMUTE.CNWH => new int[4] { d1, d0, d3, d2 }, 
				MFU_TRANS_PERMUTE.CHNW => new int[4] { d2, d0, d1, d3 }, 
				MFU_TRANS_PERMUTE.CHWN => new int[4] { d3, d0, d1, d2 }, 
				MFU_TRANS_PERMUTE.CWNH => new int[4] { d2, d0, d3, d1 }, 
				MFU_TRANS_PERMUTE.CWHN => new int[4] { d3, d0, d2, d1 }, 
				MFU_TRANS_PERMUTE.HNCW => new int[4] { d1, d2, d0, d3 }, 
				MFU_TRANS_PERMUTE.HNWC => new int[4] { d1, d3, d0, d2 }, 
				MFU_TRANS_PERMUTE.HCNW => new int[4] { d2, d1, d0, d3 }, 
				MFU_TRANS_PERMUTE.HCWN => new int[4] { d3, d1, d0, d2 }, 
				MFU_TRANS_PERMUTE.HWNC => new int[4] { d2, d3, d0, d1 }, 
				MFU_TRANS_PERMUTE.HWCN => new int[4] { d3, d2, d0, d1 }, 
				MFU_TRANS_PERMUTE.WNCH => new int[4] { d1, d2, d3, d0 }, 
				MFU_TRANS_PERMUTE.WNHC => new int[4] { d1, d3, d2, d0 }, 
				MFU_TRANS_PERMUTE.WCNH => new int[4] { d2, d1, d3, d0 }, 
				MFU_TRANS_PERMUTE.WCHN => new int[4] { d3, d1, d2, d0 }, 
				MFU_TRANS_PERMUTE.WHNC => new int[4] { d2, d3, d1, d0 }, 
				MFU_TRANS_PERMUTE.WHCN => new int[4] { d3, d2, d1, d0 }, 
				_ => new int[4] { d0, d1, d2, d3 }, 
			};
		}
	}

	private void LowerUninitialized(Call call, Uninitialized op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize max_sizes, NodeInfo currNode)
	{
		_ = currNode.OutShape;
	}

	private void LowerGnneActivation(Call call, GNNEActivation op, FusionType fusionType, List<NodeInfo> nodesInfo, GlbItemSize maxSizes, NodeInfo currNode)
	{
		Call call2 = call;
		int[] outShape = currNode.OutShape;
		bool flag = call2[GNNEActivation.InputB] == None.Default;
		int[] array = call2[GNNEActivation.InputA].CheckedShape.ToValueArray();
		int[] array2 = array;
		if (!flag)
		{
			array2 = call2[GNNEActivation.InputB].CheckedShape.ToValueArray();
		}
		int[] array3 = call2.CheckedShape.ToValueArray();
		if ((fusionType == FusionType.L2IfSplitWPp || fusionType == FusionType.L2IfSplitWFull) && (!array.SequenceEqual(array3) || !array2.SequenceEqual(array3)))
		{
			maxSizes.MaxOfmapSize = GNNEEnv.GlbSize;
		}
		DataType checkedDataType = call2[GNNEActivation.InputA].CheckedDataType;
		DataType dataType = checkedDataType;
		if (!flag)
		{
			dataType = call2[GNNEActivation.InputB].CheckedDataType;
		}
		DataType checkedDataType2 = call2.CheckedDataType;
		int num = array3[1];
		int[] array4 = outShape;
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(checkedDataType2) == 1) ? 32 : 16);
		int num2 = array4[2];
		if (currNode.Nb.AlignType == AlignedType.EAligned)
		{
			num2 = TileUtilities.GetAlignedNum(num2, alignmentFactor);
		}
		int num3 = array4[3];
		if (currNode.Nb.AlignType == AlignedType.FAligned)
		{
			num3 = TileUtilities.GetAlignedNum(num3, alignmentFactor);
		}
		int allocatedBytes = new TensorOnGlb(new int[4]
		{
			array4[0],
			array4[1],
			num2,
			num3
		}, checkedDataType2, 0).AllocatedBytes;
		allocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		allocatedBytes *= _ofBufNum;
		int num4 = array4[2];
		int num5 = array4[3];
		int[] currentInputShape = new int[4]
		{
			array3[0],
			array3[1],
			num4,
			num5
		};
		NodeBuffer nb = currNode.Nb;
		if (allocatedBytes > maxSizes.MaxOfmapSize)
		{
			maxSizes.MaxOfmapSize = allocatedBytes;
		}
		nb.Ifmap2Offset = maxSizes.MaxIfmap2Size;
		int num6 = 0;
		if (!flag && call2[GNNEActivation.InputA] is Call call3 && call3.Target is GNNELoad && nodesInfo.Find((NodeInfo ni) => ni.Op == call2[GNNEActivation.InputA]) == null)
		{
			num4 = array[2];
			num5 = array[3];
			num6 = new TensorOnGlb(new int[4]
			{
				array[0],
				array[1],
				TileUtilities.GetAlignedNum(num4, 32),
				num5
			}, checkedDataType, 0).AllocatedBytes;
			num6 = TileUtilities.GetAlignedNum(num6, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		}
		if (!flag && call2[GNNEActivation.InputB] is Call call4 && call4.Target is GNNELoad && nodesInfo.Find((NodeInfo ni) => ni.Op == call2[GNNEActivation.InputB]) == null)
		{
			num4 = array2[2];
			num5 = array2[3];
			num6 = new TensorOnGlb(new int[4]
			{
				array2[0],
				array2[1],
				TileUtilities.GetAlignedNum(num4, 32),
				num5
			}, dataType, 0).AllocatedBytes;
			num6 = TileUtilities.GetAlignedNum(num6, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		}
		maxSizes.MaxIfmap2Size += num6;
		nb.Act1Offset = maxSizes.MaxAct1Size;
		bool num7 = ((TensorConst)call2[GNNEActivation.Is16Segments]).Value.ToScalar<bool>();
		int c = (num7 ? 1 : num);
		int nActParam = (num7 ? 49 : GNNEEnv.ActNumPerChan);
		int alignedNum = TileUtilities.GetAlignedNum(SpaceSearcher.GetActSize(c, nActParam, 2), GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		maxSizes.MaxAct1Size += alignedNum;
		currNode.Nb = nb;
		if (fusionType != FusionType.L2IfFullWPp && !array.SequenceEqual(array2))
		{
			maxSizes.MaxOfmapSize = GNNEEnv.GlbSize;
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEActivation.InputA]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
		if (flag)
		{
			return;
		}
		nodesInfo.Where((NodeInfo node) => node.Op == call2[GNNEActivation.InputB]).ToList().ForEach(delegate(NodeInfo node)
		{
			node.OutShape = ((node.OutShape.Length == 0 || currentInputShape.Aggregate((int a, int b) => a * b) > node.OutShape.Aggregate((int a, int b) => a * b)) ? currentInputShape : node.OutShape);
		});
	}
}
