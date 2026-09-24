using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.Tensors;
using Nncase.Passes.Rules.K230;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

internal sealed class DDrMacCalcVisitor : ExprVisitor<Unit, Unit>
{
	public Fusion EntryFusion => (Fusion)base.VisitRoot;

	public string FusionName { get; }

	public IDictionary<string, RoofLineInfo> FusionMacsMap { get; }

	public DDrMacCalcVisitor(Dictionary<string, RoofLineInfo> fusionMacsMap, string name)
		: base(visitOtherFunctions: false)
	{
		FusionMacsMap = fusionMacsMap;
		FusionName = name;
	}

	protected override Unit DefaultVisitLeaf(Expr expr)
	{
		return default(Unit);
	}

	protected override Unit VisitLeafFusion(Fusion fusion)
	{
		return default(Unit);
	}

	protected override Unit VisitLeafCall(Call call)
	{
		if (!FusionMacsMap.TryGetValue(FusionName, out RoofLineInfo value))
		{
			value = new RoofLineInfo();
			FusionMacsMap.Add(FusionName, value);
		}
		ulong num = 0uL;
		ulong num2 = 0uL;
		long num3 = 0L;
		long num4 = 0L;
		ulong num5 = 0uL;
		ulong num6 = 0uL;
		long num7 = 0L;
		long num8 = 3840L;
		long num9 = 538L;
		long num10 = 1638400L;
		Expr target = call.Target;
		if (!(target is GNNELoad))
		{
			if (!(target is GNNELoadW))
			{
				if (!(target is GNNEStore))
				{
					if (!(target is GNNEConv2D))
					{
						if (!(target is GNNEConv2DTranspose))
						{
							if (!(target is GNNEPdp0DW))
							{
								if (!(target is GNNEMatMul))
								{
									if (!(target is GNNEPdp1))
									{
										if (!(target is GNNEActivation))
										{
											if (!(target is GNNETranspose))
											{
												if (!(target is Reshape))
												{
													if (!(target is Ai2dResize))
													{
														if (!(target is GNNELSTM))
														{
															if (!(target is GNNEPad))
															{
																if (!(target is GNNEPdp0Reduce))
																{
																	if (!(target is GetItem))
																	{
																		throw new ArgumentException(call.Target.GetType().Name + " is not supported.");
																	}
																}
																else
																{
																	num3 = (long)CostUtility.GetMemoryAccess(call[GNNEPdp0Reduce.Input].CheckedType, call.CheckedType);
																	value.AddOperator("GNNEPdp0Reduce", call[GNNEPdp0Reduce.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
																}
															}
															else
															{
																num3 = (long)CostUtility.GetMemoryAccess(call[GNNEPad.Input].CheckedType, call.CheckedType);
																value.AddOperator("GNNEPad", call[GNNEPad.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
															}
														}
														else
														{
															int[] array = call[GNNELSTM.Input].CheckedShape.ToValueArray();
															int[] array2 = call[GNNELSTM.WXc].CheckedShape.ToValueArray();
															call[GNNELSTM.InitialH].CheckedShape.ToValueArray();
															int[] array3 = call[GNNELSTM.WRc].CheckedShape.ToValueArray();
															long num11 = (long)CostUtility.GetMemoryAccess(call[GNNELSTM.Input].CheckedType) * (long)Math.Ceiling((double)array2[2] / (double)GNNEEnv.PuWidth) * array2[1];
															long num12 = (long)CostUtility.GetMemoryAccess(call[GNNELSTM.WXc].CheckedType) * array[1] * array[2];
															long num13 = (long)CostUtility.GetMemoryAccess(call[GNNELSTM.InitialH].CheckedType) * (long)Math.Ceiling((double)array3[2] / (double)GNNEEnv.PuWidth) * array[1];
															long num14 = (long)CostUtility.GetMemoryAccess(call[GNNELSTM.WRc].CheckedType) * array[1] * array[2];
															Shape shape = new int[4]
															{
																array2[1],
																array2[2],
																array[1],
																array[2]
															};
															long num15 = (long)CostUtility.GetMemoryAccess(new TensorType(DataTypes.Float16, shape));
															Shape shape2 = new int[4]
															{
																array3[1],
																array3[2],
																array[1],
																array[2]
															};
															long num16 = (long)CostUtility.GetMemoryAccess(new TensorType(DataTypes.Float16, shape2));
															num3 = (long)CostUtility.GetMemoryAccess(call[GNNELSTM.ActXc].CheckedType, call[GNNELSTM.ActRc0].CheckedType, call[GNNELSTM.ActRc1].CheckedType, call[GNNELSTM.InitialC].CheckedType, call.CheckedType);
															num3 += num11 + num12 + num13 + num14 + num15 + num16;
															num3 += (long)array[1] * (long)array[2] * array2[1] * (long)Math.Ceiling((double)array2[2] / (double)GNNEEnv.PuWidth) * num8;
															num3 += (long)array[1] * (long)array[2] * array2[1] * (long)Math.Ceiling((double)array2[2] / (double)GNNEEnv.PuWidth) * num8;
															num3 += (long)array[1] * (long)array[2] * array3[1] * (long)Math.Ceiling((double)array3[2] / (double)GNNEEnv.PuWidth) * num8;
															num3 += (long)array[1] * (long)array[2] * array3[1] * (long)Math.Ceiling((double)array3[2] / (double)GNNEEnv.PuWidth) * num8;
															UInt128 uInt = (UInt128)array[1];
															UInt128 uInt2 = (UInt128)array2[1];
															UInt128 uInt3 = (UInt128)array[2];
															UInt128 uInt4 = (UInt128)array2[3] / (byte)4;
															UInt128 uInt5 = (UInt128)array[^1];
															UInt128 uInt6 = uInt4;
															UInt128 uInt7 = uInt3;
															UInt128 uInt8 = uInt;
															num2 = (ulong)(uInt2 * uInt7 * uInt8 * ((byte)4 * uInt6 * uInt5 + (byte)4 * uInt6 * uInt6));
															value.AddOperator("GNNELSTM", call[GNNELSTM.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
														}
													}
													else
													{
														num3 = (long)CostUtility.GetMemoryAccess(call[Ai2dResize.Input].CheckedType, call.CheckedType);
														value.AddOperator("Ai2dResize", call[Ai2dResize.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
													}
												}
											}
											else
											{
												num3 = (long)CostUtility.GetMemoryAccess(call[GNNETranspose.Input].CheckedType, call.CheckedType);
												value.AddOperator("GNNETranspose", call[GNNETranspose.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
											}
										}
										else
										{
											bool flag = call[GNNEActivation.InputB] == None.Default;
											bool flag2 = (call[GNNEActivation.InputA] is Call call2 && call2.Target is GNNELoad) || (call[GNNEActivation.InputB] is Call call3 && call3.Target is GNNELoad) || (call[GNNEActivation.InputA] is Call call4 && call4.Target is Reshape) || (call[GNNEActivation.InputB] is Call call5 && call5.Target is Reshape);
											num3 = (long)CostUtility.GetMemoryAccess(call[GNNEActivation.InputA].CheckedType, call[GNNEActivation.InputB].CheckedType, call.CheckedType);
											num3 += (flag2 ? num10 : 0) + (flag ? (num8 * 2) : (num8 * 3));
											value.AddOperator("GNNEActivation", call[GNNEActivation.InputA].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
										}
									}
									else
									{
										num3 = (long)CostUtility.GetMemoryAccess(call[GNNEPdp1.Input].CheckedType, call.CheckedType);
										value.AddOperator("GNNEPdp1", call[GNNEPdp1.Input].CheckedShape.Select((Dimension x) => (" ", (ulong)x.FixedValue)).ToArray());
									}
								}
								else
								{
									ulong num17 = call.CheckedShape.Aggregate(0uL, (ulong sum, Dimension x) => sum * (ulong)x.FixedValue);
									num = (ulong)call[GNNEMatMul.InputA].CheckedShape[3].FixedValue * num17;
									ulong[] array4 = call[GNNEMatMul.InputA].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
									ulong[] array5 = call[GNNEMatMul.InputB].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
									ulong[] array6 = call.CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
									num2 = (ulong)((long)(array4.Aggregate(1uL, (ulong acc, ulong i) => acc * i) * array6[^1]) * (long)call[GNNEMatMul.InputA].CheckedDataType.SizeInBytes * call[GNNEMatMul.InputB].CheckedDataType.SizeInBytes);
									num3 = (long)CostUtility.GetMemoryAccess(call[GNNEMatMul.InputA].CheckedType, call[GNNEMatMul.InputB].CheckedType, call.CheckedType);
									num3 += (long)Math.Max(array4[0] * array4[1], array5[0] * array5[1]) * (long)Math.Ceiling((double)array4[3] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array4[2] / (double)GNNEEnv.PuWidth) * num8;
									num3 += (long)Math.Ceiling((double)array6[1] * (double)array6[2] / (double)GNNEEnv.PuWidth) * num8;
									value.AddOperator("GNNEMatMul", (from x in array4.SkipLast(2)
										select (" ", x)).Concat(new(string, ulong)[2]
									{
										("N", array4[^2]),
										("K", array4[^1])
									}).Concat((from x in array5.SkipLast(2)
										select (" ", x)).Concat(new(string, ulong)[2]
									{
										("K", array5[^2]),
										("M", array5[^1])
									})).ToArray());
								}
							}
							else
							{
								ulong[] array7 = call.CheckedShape.Select((Dimension d) => (ulong)d.FixedValue).ToArray();
								num = (ulong)((long)call[GNNEPdp0DW.Input].CheckedShape[1].FixedValue * (long)call[GNNEPdp0DW.Weights].CheckedShape[2].FixedValue * call[GNNEPdp0DW.Weights].CheckedShape[3].FixedValue * (long)array7[2]) * array7[3];
								ulong[] array8 = call[GNNEPdp0DW.Input].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
								ulong[] array9 = call[GNNEPdp0DW.Weights].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
								num2 = (ulong)((long)(array8[0] * array9[0] * array9[1] * array7[2] * array7[3] * array9[2] * array9[3]) * (long)call[GNNEPdp0DW.Input].CheckedDataType.SizeInBytes * call[GNNEPdp0DW.Weights].CheckedDataType.SizeInBytes);
								num3 = (long)CostUtility.GetMemoryAccess(call[GNNEPdp0DW.Input].CheckedType, call[GNNEPdp0DW.Weights].CheckedType, call[GNNEPdp0DW.Act].CheckedType, call.CheckedType);
								num3 += (long)Math.Ceiling((double)array8[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array8[2] * (double)array8[3] / (double)(GNNEEnv.PsumL1ElePerChan / 2)) * num8;
								num3 += (long)Math.Ceiling((double)array7[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array7[2] * (double)array7[3] / (double)(GNNEEnv.PsumL1ElePerChan / 2)) * num8;
								num7 = -(long)CostUtility.GetMemoryAccess(call[GNNEPdp0DW.WeightsQInt8].CheckedType, call[GNNEPdp0DW.WeightsBiasQint8].CheckedType, call[GNNEPdp0DW.ActQint8].CheckedType);
								value.AddOperator("GNNEPdp0DW", ("B", array8[0]), ("OC", 1uL), ("IC", array9[0]), ("OH", array7[2]), ("OW", array7[3]), ("KH", array9[2]), ("KW", array9[3]));
							}
						}
						else
						{
							ulong[] array10 = call.CheckedShape.Select((Dimension d) => (ulong)d.FixedValue).ToArray();
							num = (ulong)((long)call[GNNEConv2DTranspose.Input].CheckedShape[1].FixedValue * (long)call[GNNEConv2DTranspose.Weights].CheckedShape[2].FixedValue * call[GNNEConv2DTranspose.Weights].CheckedShape[3].FixedValue * call.CheckedShape[1].FixedValue * call[GNNEConv2DTranspose.Input].CheckedShape[2].FixedValue * call[GNNEConv2DTranspose.Input].CheckedShape[3].FixedValue);
							ulong[] array11 = call[GNNEConv2DTranspose.Input].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
							ulong[] array12 = call[GNNEConv2DTranspose.Weights].CheckedShape.Select((Dimension x) => (ulong)x.FixedValue).ToArray();
							num2 = (ulong)((long)(array11[0] * array12[0] * array12[1] * array11[2] * array11[3] * array12[2] * array12[3]) * (long)call[GNNEConv2DTranspose.Input].CheckedDataType.SizeInBytes * call[GNNEConv2DTranspose.Weights].CheckedDataType.SizeInBytes);
							num3 = (long)CostUtility.GetMemoryAccess(call[GNNEConv2DTranspose.Input].CheckedType, call[GNNEConv2DTranspose.Weights].CheckedType, call[GNNEConv2DTranspose.Act].CheckedType, call.CheckedType);
							num3 += (long)array11[0] * (long)Math.Ceiling((double)array11[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array12[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array11[2] * (double)array11[3] / (double)GNNEEnv.PsumL1ElePerChan) * num8;
							num3 += (long)array11[0] * (long)Math.Ceiling((double)array10[1] / (double)GNNEEnv.PuWidth) * num8;
							num7 = -(long)CostUtility.GetMemoryAccess(call[GNNEConv2DTranspose.WeightsQInt8].CheckedType, call[GNNEConv2DTranspose.WeightsBiasQint8].CheckedType, call[GNNEConv2DTranspose.ActQint8].CheckedType);
							num7 += (long)array11[0] * (long)Math.Ceiling((double)array11[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array12[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array11[2] * (double)array11[3] / (double)GNNEEnv.PsumL1ElePerChan) * (long)array12[2] * (long)array12[3] * num9;
							value.AddOperator("GNNEConv2DTranspose", ("B", array11[0]), ("OC", array12[0]), ("IC", array12[1]), ("OH", array10[2]), ("OW", array10[3]), ("KH", array12[2]), ("KW", array12[3]));
						}
					}
					else
					{
						ulong[] array13 = call[GNNEConv2D.Input].CheckedShape.Select((Dimension d) => (ulong)d.FixedValue).ToArray();
						ulong[] array14 = call[GNNEConv2D.Weights].CheckedShape.Select((Dimension d) => (ulong)d.FixedValue).ToArray();
						ulong[] array15 = call.CheckedShape.Select((Dimension d) => (ulong)d.FixedValue).ToArray();
						int num18 = ((TensorConst)call[GNNEConv2D.Groups]).Value.ToScalar<int>();
						ulong num19 = array13[1];
						ulong num20 = array15[1];
						bool flag3 = num19 == num20 && (int)num20 == num18 && num18 != 1;
						bool flag4 = call.Users.Count == 1 && call.Users.ElementAt(0) is Call call6 && call6.Target is GNNEPdp0DW;
						bool flag5 = call[GNNEConv2D.Input] is Call call7 && call7.Target is GNNELoad && call.Users.ElementAt(0) is Call call8 && call8.Target is GNNEStore;
						ulong num21 = (flag3 ? (array13[1] * array14[2] * array14[3] * array15[2] * array15[3] + array13[1] * array15[1] * array15[2] * array15[3]) : (array13[1] * array14[2] * array14[3] * array15[1] * array15[2] * array15[3]));
						int num22 = ((TensorConst)call[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
						int num23 = ((TensorConst)call[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
						int num24 = ((TensorConst)call[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
						int num25 = ((TensorConst)call[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
						int[] array16 = ((TensorConst)call[GNNEConv2D.Padding]).Value.ToArray<int>();
						Padding padding = new Padding(array16[0], array16[1]);
						Padding padding2 = new Padding(array16[2], array16[3]);
						bool flag6 = false;
						if (!flag3 && num18 == 1 && num22 == 1 && num23 == 1 && array14[2] == 1 && array14[3] == 1 && num24 == 1 && num25 == 1 && padding.Sum() == 0 && padding2.Sum() == 0 && (call[GNNEConv2D.Input].CheckedDataType == DataTypes.Int8 || call[GNNEConv2D.Input].CheckedDataType == DataTypes.UInt8) && array15[2] * array15[3] < 512)
						{
							flag6 = true;
						}
						num2 = (ulong)((long)(array13[0] * array14[0] * array14[1] * array15[2] * array15[3] * array14[2] * array14[3]) * (long)call[GNNEConv2D.Input].CheckedDataType.SizeInBytes * call[GNNEConv2D.Weights].CheckedDataType.SizeInBytes);
						num = num21;
						num3 = (long)CostUtility.GetMemoryAccess(call[GNNEConv2D.Weights].CheckedType, call[GNNEConv2D.Act].CheckedType, call.CheckedType);
						num3 += (long)CostUtility.GetMemoryAccess(call[GNNEConv2D.Input].CheckedType) * (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth);
						if (flag6)
						{
							num3 = ((array13[0] > 1 && array13[2] == 1 && array13[3] == 1) ? (num3 + (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array14[1] / (double)GNNEEnv.PuHeight) * num8) : ((!flag4) ? (num3 + (long)array13[0] * (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan) * num8) : (num3 + (long)array13[0] * (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan * 2.0) * num8)));
						}
						else
						{
							if (flag3)
							{
								num3 = ((!flag4) ? (num3 + (long)array13[0] * (long)Math.Ceiling((double)array13[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan) * num8) : (num3 + (long)array13[0] * (long)Math.Ceiling((double)array13[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan * 2.0) * num8));
							}
							else if (flag4)
							{
								num3 += (long)array13[0] * (long)Math.Ceiling((double)array13[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth) * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan * 2.0) * num8;
							}
							else
							{
								long num26 = ((array14[3] > 31) ? ((long)Math.Ceiling((double)TileUtilities.GetAlignedNum((int)array14[3], 31) / (double)GNNEEnv.PuKernelSpad * 2.0)) : 1);
								num3 += (long)array13[0] * (long)Math.Ceiling((double)array13[1] / (double)GNNEEnv.PuHeight) * (long)Math.Ceiling((double)array14[0] / (double)GNNEEnv.PuWidth) * num26 * (long)Math.Ceiling((double)array13[2] * (double)array13[3] / (double)GNNEEnv.PsumL1ElePerChan) * num8;
							}
							num3 *= ((!flag5) ? 1 : 12);
						}
						num3 += (long)Math.Ceiling((double)array15[1] / (double)GNNEEnv.PuWidth) * num8;
						num7 = -(long)CostUtility.GetMemoryAccess(call[GNNEConv2D.WeightsQInt8].CheckedType, call[GNNEConv2D.WeightsBiasQint8].CheckedType, call[GNNEConv2D.ActQint8].CheckedType);
						value.AddOperator("GNNEConv2D", ("B", array13[0]), ("OC", array14[0]), ("IC", array14[1]), ("OH", array15[2]), ("OW", array15[3]), ("KH", array14[2]), ("KW", array14[3]));
					}
				}
				else
				{
					num6++;
					num3 = (long)CostUtility.GetMemoryAccess(call[GNNEStore.Input].CheckedType);
					num4 = (long)CostUtility.GetMemoryAccess(call.CheckedType);
				}
			}
			else
			{
				num3 = (long)CostUtility.GetMemoryAccess(call.CheckedType);
				num7 = (long)CostUtility.GetMemoryAccess(call[GNNELoadW.Input].CheckedType);
			}
		}
		else
		{
			num5++;
			num3 = (long)CostUtility.GetMemoryAccess(call.CheckedType);
			num4 = (long)CostUtility.GetMemoryAccess(call[GNNELoad.Input].CheckedType);
		}
		value["Mac"] += num;
		value["FLOPs"] += num2;
		value["OnChipMemTraffic"] += (ulong)num3;
		value["OffChipMemTraffic"] += (ulong)num4;
		value["OffChipMemLWTraffic"] += (ulong)num7;
		value["OffChipLoadStoreCnt"] += num5 + num6;
		return default(Unit);
	}
}
