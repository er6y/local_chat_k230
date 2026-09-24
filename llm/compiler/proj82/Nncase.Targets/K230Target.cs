#define TRACE
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nncase.CodeGen;
using Nncase.CodeGen.K230;
using Nncase.CodeGen.StackVM;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.Mutators.K230;
using Nncase.Passes;
using Nncase.Passes.Analysis;
using Nncase.Passes.Rules.K230;
using Nncase.Passes.Rules.Lower;
using Nncase.Passes.Rules.Neutral;
using Nncase.Passes.Rules.Tile;
using Nncase.PatternMatch;
using Nncase.Quantization;
using Nncase.Runtime.K230;
using Nncase.Utilities;

namespace Nncase.Targets;

public class K230Target : ITarget
{
	public class K230MixQuantInfo : MixQuantInfo
	{
		private bool _permitInt16Quant = true;

		private TensorConst? _i16FineTunedWeights;

		private TensorConst? _i16FineTunedWeightsRangesByChannel;

		public bool PermitInt16Quant
		{
			get
			{
				return _permitInt16Quant;
			}
			set
			{
				_permitInt16Quant = value;
			}
		}

		public TensorConst? I16FineTunedWeights
		{
			get
			{
				return _i16FineTunedWeights;
			}
			set
			{
				_i16FineTunedWeights = value;
			}
		}

		public TensorConst? I16FineTunedWeightsRangesByChannel
		{
			get
			{
				return _i16FineTunedWeightsRangesByChannel;
			}
			set
			{
				_i16FineTunedWeightsRangesByChannel = value;
			}
		}
	}

	public const string Kind = "k230";

	public static ModuleType ModuleType => ModuleType.Create("k230");

	string ITarget.Kind => "k230";

	public void ParseTargetDependentOptions(IConfigurationSection configure)
	{
	}

	public void RegisterTargetInDependentPass(IPassManager passManager, CompileOptions options)
	{
		passManager.AddWithName<DataflowPass>("TargetDependentNeutralOptimize", Array.Empty<object>()).Configure(delegate(DataflowPass p)
		{
			p.Add<BroadcastTransposeOutputNames>(Array.Empty<object>());
			p.Add<BroadcastReshapeOutputNames>(Array.Empty<object>());
			p.Add<BroadcastNopPadOutputNames>(Array.Empty<object>());
			p.Add<IntegralPromotion>(Array.Empty<object>());
			p.Add<Nncase.Passes.Rules.Neutral.FoldConstCall>(Array.Empty<object>());
			p.Add<FoldShapeOf>(Array.Empty<object>());
			p.Add<TransposeToReshape>(Array.Empty<object>());
			p.Add<ExpandToBroadcast>(Array.Empty<object>());
			p.Add<MatMulToConv2DWithMarker>(Array.Empty<object>());
			p.Add<BroadcastMatMulToConv2DWithMarker>(Array.Empty<object>());
			p.Add<MatMulToConv2D>(Array.Empty<object>());
			p.Add<BroadcastMatMulToConv2D>(Array.Empty<object>());
			p.Add<BroadcastMatMul>(Array.Empty<object>());
			p.Add<ReshapeBatchMatmul>(Array.Empty<object>());
			p.Add<SplitBatchMatMul>(Array.Empty<object>());
			p.Add<BatchNormToBinary>(Array.Empty<object>());
			p.Add<FoldTwoReshapes>(Array.Empty<object>());
			p.Add<FoldNopReshape>(Array.Empty<object>());
			p.Add<FoldTwoReduce>(Array.Empty<object>());
			p.Add<CombineBinaryReshape>(Array.Empty<object>());
			p.Add<CombineConstBinaryReshape>(Array.Empty<object>());
			p.Add<CombineUnaryReshape>(Array.Empty<object>());
			p.Add<CombineActivationsReshape>(Array.Empty<object>());
			p.Add<FoldNopBroadcast>(Array.Empty<object>());
		});
	}

	public void RegisterTargetDependentPass(IPassManager passManager, CompileOptions options)
	{
		passManager.AddWithName<DataflowPass>("pre_fake_opt", Array.Empty<object>()).Configure(delegate(DataflowPass p)
		{
			p.Add<LeakyReluTransposeSwap>(Array.Empty<object>());
			p.Add<LeakyReluReshape>(Array.Empty<object>());
			p.Add<TransposeCastMotion>(Array.Empty<object>());
			p.Add<ExpandShapeOfGlobalReduceWindow>(Array.Empty<object>());
			p.Add<ReduceToGlobalReduceWindow>(Array.Empty<object>());
			p.Add<ReduceWithDim1ToGlobalReduceWindow>(Array.Empty<object>());
			p.Add<ReduceWithDim12ToGlobalReduceWindow>(Array.Empty<object>());
			p.Add<Nncase.Passes.Rules.Neutral.FoldConstCall>(Array.Empty<object>());
			p.Add<FoldNopTranspose>(Array.Empty<object>());
			p.Add<FoldTwoTransposes>(Array.Empty<object>());
			p.Add<CombineTransposeUnary>(Array.Empty<object>());
			p.Add<CombineTransposePad>(Array.Empty<object>());
			p.Add<CombineBinaryTranspose>(Array.Empty<object>());
			p.Add<CombineTransposeConstBinary>(Array.Empty<object>());
			p.Add<CombineTransposeReduce>(Array.Empty<object>());
			p.Add<CombineTransposeActivations>(Array.Empty<object>());
			p.Add<CombinePadTranspose>(Array.Empty<object>());
			p.Add<FoldNopPad>(Array.Empty<object>());
			p.Add<FoldConv2DPads>(Array.Empty<object>());
			p.Add<FoldReduceWindow2DPads>(Array.Empty<object>());
			p.Add<SplitLargeConv2D>(Array.Empty<object>());
			p.Add<SplitLargeConv2DTranspose>(Array.Empty<object>());
			p.Add<FoldReluConcatWithConv2d>(Array.Empty<object>());
		});
		if (options.QuantizeOptions.ModelQuantMode != ModelQuantMode.UsePTQ)
		{
			return;
		}
		passManager.AddWithName<DataflowPass>("to_fake_preprocess_1", Array.Empty<object>()).Configure(delegate(DataflowPass p)
		{
			p.Add<ReplaceMarker>(Array.Empty<object>());
			p.Add<UniteConcatRange>(Array.Empty<object>());
		});
		passManager.AddWithName<DataflowPass>("to_fake_preprocess_2", Array.Empty<object>()).Configure(delegate(DataflowPass p)
		{
			p.AddAnalysis<IExprUserAnalysisResult>();
			p.Add<BroadcastConcatRange>(Array.Empty<object>());
		});
		passManager.AddWithName<EGraphRulesPass>("to_fake", Array.Empty<object>()).Configure(delegate(EGraphRulesPass p)
		{
			p.Add<ToFakeLSTM>(Array.Empty<object>());
			p.Add<ToFakeConv2D>(Array.Empty<object>());
			p.Add<ToFakeConv2DTranspose>(Array.Empty<object>());
			p.Add<ToFakePdpReduce>(Array.Empty<object>());
			p.Add<ToFakeMatmul>(Array.Empty<object>());
			p.Add<PReluToFakeActivation>(Array.Empty<object>());
			p.Add<GeluToFakeActivation>(Array.Empty<object>());
			p.Add<MulToFakeActivation>(Array.Empty<object>());
			p.Add<DivToFakeActivation>(Array.Empty<object>());
			p.Add<AddToFakeActivation>(Array.Empty<object>());
			p.Add<SubToFakeActivation>(Array.Empty<object>());
			p.Add<MaxToFakeActivation>(Array.Empty<object>());
			p.Add<MinToFakeActivation>(Array.Empty<object>());
			p.Add<NegToFakeActivation>(Array.Empty<object>());
			p.Add<AbsToFakeActivation>(Array.Empty<object>());
			p.Add<GeneralAddSubMulDivToFakeActivation>(Array.Empty<object>());
			p.Add<LeakyReluToFakeActivation>(Array.Empty<object>());
			p.Add<ReluToFakeActivation>(Array.Empty<object>());
			p.Add<BatchNormToFakeActivation>(Array.Empty<object>());
			p.Add<ResizeToFakeActivation>(Array.Empty<object>());
			p.Add<BroadcastToFakeActivation>(Array.Empty<object>());
			p.Add<ACosToFakeActivation>(Array.Empty<object>());
			p.Add<ASinToFakeActivation>(Array.Empty<object>());
			p.Add<ClampToFakeActivation>(Array.Empty<object>());
			p.Add<EluToFakeActivation>(Array.Empty<object>());
			p.Add<HardSigmoidToFakeActivation>(Array.Empty<object>());
			p.Add<HardSwishToFakeActivation>(Array.Empty<object>());
			p.Add<SigmoidToFakeActivation>(Array.Empty<object>());
			p.Add<SwishToFakeActivation>(Array.Empty<object>());
			p.Add<TanhToFakeActivation>(Array.Empty<object>());
			p.Add<ResizeToFakeActivation>(Array.Empty<object>());
			p.Add<TileToFakeActivation>(Array.Empty<object>());
			p.Add<ToFakeAi2dResize>(Array.Empty<object>());
			p.Add<FoldNopReshape>(Array.Empty<object>());
		});
		passManager.AddWithName<EGraphPassWithAdaRound>("AdaRoundWeights", Array.Empty<object>());
		passManager.AddWithName<EGraphPassWithBindQuantizeConfig>("BindQuantizeConfig", Array.Empty<object>());
		passManager.AddWithName<EGraphRulesPass>("fake_opt", Array.Empty<object>()).Configure(delegate(EGraphRulesPass p)
		{
			p.Add<FoldTwoFakeActivation>(Array.Empty<object>());
			p.Add<FoldFakeConv2DAndFakeActivation>(Array.Empty<object>());
			p.Add<FoldFakeMatMulAndFakeActivation>(Array.Empty<object>());
			p.Add<FoldFakeConv2DTransposeAndFakeActivation>(Array.Empty<object>());
			p.Add<FoldMaxAndFakeActivation>(Array.Empty<object>());
		});
		if ((options.QuantizeOptions.QuantScheme != string.Empty && !options.QuantizeOptions.QuantSchemeStrictMode) || (options.QuantizeOptions.QuantScheme == string.Empty && !options.QuantizeOptions.ExportQuantScheme) || (options.QuantizeOptions.ExportQuantScheme && options.QuantizeOptions.ExportWeightRangeByChannel))
		{
			passManager.AddWithName<DataflowPass>("fake_range_by_channel", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.Add<ReplaceFakeConv2DWeightsRangeToByChannel>(Array.Empty<object>());
				p.Add<ReplaceFakeConv2DTransposeWeightsRangeToByChannel>(Array.Empty<object>());
			});
		}
		passManager.AddWithName<DataflowPass>("fake_range_fix", Array.Empty<object>()).Configure(delegate(DataflowPass p)
		{
			p.Add<DisableConvDWPermitInt16Quant>(Array.Empty<object>());
			p.Add<DisableConvPdp0ReducePermitInt16Quant>(Array.Empty<object>());
			p.Add<SquantFineTuneFakeConv2DWeights>(Array.Empty<object>());
			p.Add<SplitLargeActivation>(Array.Empty<object>());
		});
	}

	public Task AdaRoundWeights(ICalibrationDatasetProvider calibrationDataset, List<ENode> rangeOfs, List<ENode> childrenOfRangeOfs, QuantizeOptions quantizeOptions)
	{
		return Task.CompletedTask;
	}

	public async Task<Dictionary<ENode, List<Tuple<List<DataType>, List<List<QuantParam>>, float>>>> BindQuantMethodCosine(ICalibrationDatasetProvider calibrationDataset, List<ENode> rangeOfs, List<ENode> childrenOfRangeOfs, QuantizeOptions quantizeOptions)
	{
		return BindQuantMethodCosineImpl(await calibrationDataset.Samples.ToListAsync(), rangeOfs, childrenOfRangeOfs, quantizeOptions);
	}

	public void RegisterQuantizePass(IPassManager passManager, CompileOptions options)
	{
		if (options.QuantizeOptions.ModelQuantMode == ModelQuantMode.UsePTQ)
		{
			passManager.AddWithName<DataflowPass>("LowerFakePdp", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<ToGNNEPdpReduce>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("LowerFakeMain", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.Add<ToGNNELSTM>(Array.Empty<object>());
				p.Add<ToDynamicGNNEMatMul>(Array.Empty<object>());
				p.Add<ToGNNEConv2D>(Array.Empty<object>());
				p.Add<ToGNNEConv2DTranspose>(Array.Empty<object>());
				p.Add<ToGNNEMatMul>(Array.Empty<object>());
				p.Add<ToGNNEActivation>(Array.Empty<object>());
				p.Add<ToAi2dPad>(Array.Empty<object>());
				p.Add<ToAi2dResize>(Array.Empty<object>());
				p.Add<FoldNopReshape>(Array.Empty<object>());
				p.Add<RestoreFakeConv2D>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("LowerClearMarker", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<RemoveMarker>(Array.Empty<object>());
				p.Add<FoldNopReshape>(Array.Empty<object>());
				p.Add<FoldTwoReshapes>(Array.Empty<object>());
			});
			passManager.AddWithName<EGraphRulesPass>("LowerEGraph", Array.Empty<object>());
			passManager.AddWithName<DataflowPass>("LowerdOptimize", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<CombineQuantizeConcat>(Array.Empty<object>());
				p.Add<CombineQuantizeReshape>(new object[1] { true });
				p.Add<CombineQuantizeTranspose>(Array.Empty<object>());
				p.Add<FoldQuantDeQuant>(Array.Empty<object>());
				p.Add<FoldDeQuantQuant>(Array.Empty<object>());
				p.Add<ToGNNETranspose>(Array.Empty<object>());
				p.Add<ToGNNEPad>(Array.Empty<object>());
				p.Add<MarkActivationInputAL1Fuse>(Array.Empty<object>());
				p.Add<MarkActivationInputBL1Fuse>(Array.Empty<object>());
			});
			passManager.AddWithName<EGraphRulesPass>("LowerFake_6", Array.Empty<object>());
		}
	}

	public void RegisterTargetDependentAfterQuantPass(IPassManager passManager, CompileOptions options)
	{
		if (options.QuantizeOptions.ModelQuantMode == ModelQuantMode.UsePTQ)
		{
			passManager.AddWithName<DataflowPass>("ProcessShiftBitsPass_1", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.Add<ProcessShiftBitsGNNEMatmul>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("PostLoweringProcess_1", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<EliminateFloat32>(Array.Empty<object>());
				p.Add<FoldNopReshape>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("PostLoweringProcess_2", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<FuseQuantIntoConv>(Array.Empty<object>());
				p.Add<FuseQuantIntoConvTranspose>(Array.Empty<object>());
				p.Add<FuseQuantIntoPdp0Reduce>(Array.Empty<object>());
				p.Add<FuseQuantIntoPdp0Dw>(Array.Empty<object>());
				p.Add<FuseQuantIntoPdp1>(Array.Empty<object>());
				p.Add<FuseQuantIntoAct1>(Array.Empty<object>());
				p.Add<FoldNopAct1>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("PostLoweringProcess_3", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<DWToPdp>(Array.Empty<object>());
				p.Add<FoldGNNEPdp0ReduceAndGNNEActivation>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("PostLoweringProcess_5", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.AddAnalysis<IExprUserAnalysisResult>();
				p.Add<StackToConcat>(Array.Empty<object>());
				p.Add<Expand1x1Conv>(Array.Empty<object>());
				p.Add<QuantToCpu>(Array.Empty<object>());
				p.Add<QuantToAct1>(Array.Empty<object>());
				p.Add<DeqToAct1>(Array.Empty<object>());
				p.Add<FoldTwoReshapes>(Array.Empty<object>());
				p.Add<FoldNopReshape>(Array.Empty<object>());
				p.Add<EliminateFloat32>(Array.Empty<object>());
			});
			passManager.AddWithName<EGraphRulesPass>("PostLoweringProcess_6", Array.Empty<object>());
			passManager.AddWithName<DataflowPass>("PostLoweringProcess_7", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.Add<ProcessConvShiftBits>(Array.Empty<object>());
				p.Add<ProcessConvTransposeShiftBits>(Array.Empty<object>());
				p.Add<ProcessAct1ShiftBits>(Array.Empty<object>());
				p.Add<ProcessPdp0DWShiftBits>(Array.Empty<object>());
				p.Add<ProcessPdp0ReduceShiftBits>(Array.Empty<object>());
				p.Add<ProcessMatmulShiftBits>(Array.Empty<object>());
				p.Add<ProcessLstmShiftBits>(Array.Empty<object>());
			});
			passManager.AddWithName<DataflowPass>("Fusion", Array.Empty<object>()).Configure(delegate(DataflowPass p)
			{
				p.Add<ConvFusion>(Array.Empty<object>());
				p.Add<Pdp0DwFusion>(Array.Empty<object>());
				p.Add<Pdp0ReduceFusion>(Array.Empty<object>());
				p.Add<PadFusion>(Array.Empty<object>());
				p.Add<TransposeFusion>(Array.Empty<object>());
				p.Add<Pdp1Fusion>(Array.Empty<object>());
				p.Add<ActSIFFusion>(Array.Empty<object>());
				p.Add<ActDIFFusion>(Array.Empty<object>());
				p.Add<ConvTransposeFusion>(Array.Empty<object>());
				p.Add<MatMulFusion>(Array.Empty<object>());
				p.Add<ResizeFusion>(Array.Empty<object>());
				p.Add<GNNELSTMFusion>(Array.Empty<object>());
			});
			passManager.AddWithName<K230FusionToTirPass>("FusionToTirPass", Array.Empty<object>());
			passManager.AddWithName<PrimFuncPass>("BufferStage", Array.Empty<object>()).Configure(delegate(PrimFuncPass p)
			{
				p.Add<Nncase.Mutators.K230.FoldConstCall>(Array.Empty<object>());
			});
			passManager.AddWithName<DDrBufferSchdeulePass>("DDrBufferSchdeule", Array.Empty<object>());
			passManager.Add<DumpTraceInfoPass>(Array.Empty<object>());
			passManager.AddWithName<PrimFuncPass>("InstStage", Array.Empty<object>()).Configure(delegate(PrimFuncPass p)
			{
				p.Add<Nncase.Mutators.K230.FoldConstCall>(Array.Empty<object>());
				p.Add<FoldBufferSlot>(Array.Empty<object>());
			});
		}
	}

	public void RegisterTargetDependentBeforeCodeGen(IPassManager passManager, CompileOptions options)
	{
	}

	public IModuleBuilder CreateModuleBuilder(string moduleKind, CompileOptions options)
	{
		if (moduleKind == K230RtModule.Kind)
		{
			return new Nncase.CodeGen.K230.ModuleBuilder(options);
		}
		return new StackVMModuleBuilder();
	}

	public (Command Command, Func<InvocationContext, Command, ITargetCompileOptions> Parser) RegisterCommandAndParser()
	{
		return (Command: new Command("k230"), Parser: (InvocationContext _, Command _) => DefaultTargetCompileOptions.Instance);
	}

	private Dictionary<ENode, List<Tuple<List<DataType>, List<List<QuantParam>>, float>>> BindQuantMethodCosineImpl(List<IReadOnlyDictionary<Var, IValue>> samples, List<ENode> rangeOfs, List<ENode> childrenOfRangeOfs, QuantizeOptions quantizeOptions)
	{
		int count = Math.Min(samples.Count, 5);
		samples = samples.GetRange(0, count);
		Dictionary<ENode, List<Tuple<List<DataType>, List<List<QuantParam>>, float>>> dictionary = new Dictionary<ENode, List<Tuple<List<DataType>, List<List<QuantParam>>, float>>>();
		List<IReadOnlyDictionary<ENode, Tensor>> list = new List<IReadOnlyDictionary<ENode, Tensor>>();
		foreach (IReadOnlyDictionary<Var, IValue> sample in samples)
		{
			using (new DumpScope("eval_childof_rangeof"))
			{
				using CalibrationEvaluator calibrationEvaluator = new CalibrationEvaluator(sample, childrenOfRangeOfs);
				IReadOnlyDictionary<ENode, Tensor> item = calibrationEvaluator.Evaluate();
				list.Add(item);
			}
		}
		foreach (ENode childrenOfRangeOf in childrenOfRangeOfs)
		{
			if (!(childrenOfRangeOf.Expr is Call) || (!(((Call)childrenOfRangeOf.Expr).Target is FakeActivation) && !(((Call)childrenOfRangeOf.Expr).Target is FakeAi2dPad) && !(((Call)childrenOfRangeOf.Expr).Target is FakeAi2dResize) && !(((Call)childrenOfRangeOf.Expr).Target is FakeConv2D) && !(((Call)childrenOfRangeOf.Expr).Target is FakeConv2DTranspose) && !(((Call)childrenOfRangeOf.Expr).Target is FakeDynamicGNNEMatMul) && !(((Call)childrenOfRangeOf.Expr).Target is FakeLSTM) && !(((Call)childrenOfRangeOf.Expr).Target is FakeMatMul) && !(((Call)childrenOfRangeOf.Expr).Target is FakePdp)))
			{
				continue;
			}
			List<Tuple<List<DataType>, List<List<QuantParam>>, float>> list2 = new List<Tuple<List<DataType>, List<List<QuantParam>>, float>>();
			int num = 0;
			foreach (IReadOnlyDictionary<Var, IValue> sample2 in samples)
			{
				IReadOnlyDictionary<ENode, Tensor> readOnlyDictionary;
				using (new DumpScope("eval_rangeof"))
				{
					using CalibrationEvaluator calibrationEvaluator2 = new CalibrationEvaluator(sample2, rangeOfs);
					readOnlyDictionary = calibrationEvaluator2.Evaluate();
				}
				IReadOnlyDictionary<ENode, Tensor> readOnlyDictionary2 = list[num++];
				List<Tuple<List<DataType>, List<List<QuantParam>>, float>> list3 = new List<Tuple<List<DataType>, List<List<QuantParam>>, float>>();
				MarkerPattern markerPattern = Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard());
				List<Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>> list4 = new List<Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>>();
				for (int i = 1; i < childrenOfRangeOf.Children.Count; i++)
				{
					List<ENode> list5 = new List<ENode>();
					ENode item2 = childrenOfRangeOf.Children[i].Nodes[0];
					list5.Add(item2);
					using (new DumpScope("eval_input"))
					{
						IReadOnlyDictionary<ENode, Tensor> item3 = new CalibrationEvaluator(sample2, list5).Evaluate();
						bool flag = markerPattern.MatchLeaf(childrenOfRangeOf.Children[i].Nodes[0].Expr);
						if (flag)
						{
							Tensor item4 = readOnlyDictionary[childrenOfRangeOf.Children[i].Nodes[0].Children[1].Nodes[0]];
							list4.Add(new Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>(item3, item4, flag));
						}
						else
						{
							list4.Add(new Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>(item3, null, flag));
						}
					}
				}
				bool flag2 = false;
				int num2 = 0;
				for (int j = 0; j < list4.Count; j++)
				{
					if (list4[j].Item3)
					{
						num2++;
					}
				}
				Trace.Assert(num2 == 0 || num2 == 1 || num2 == 2);
				List<List<DataType>> list6 = new List<List<DataType>>();
				switch (num2)
				{
				case 1:
					list6.AddRange(new List<List<DataType>>
					{
						new List<DataType> { DataTypes.UInt8 },
						new List<DataType> { DataTypes.Int8 },
						new List<DataType> { DataTypes.Int16 },
						new List<DataType> { DataTypes.Float16 }
					});
					break;
				case 2:
					list6.AddRange(new List<List<DataType>>
					{
						new List<DataType>
						{
							DataTypes.UInt8,
							DataTypes.UInt8
						},
						new List<DataType>
						{
							DataTypes.Int8,
							DataTypes.Int8
						},
						new List<DataType>
						{
							DataTypes.Int16,
							DataTypes.UInt8
						},
						new List<DataType>
						{
							DataTypes.UInt8,
							DataTypes.Int16
						},
						new List<DataType>
						{
							DataTypes.Float16,
							DataTypes.Float16
						}
					});
					break;
				}
				for (int k = 0; k < list6.Count; k++)
				{
					Trace.Assert(Nncase.PatternMatch.Utility.IsCall(Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard()).MatchLeaf(childrenOfRangeOf.Expr));
					K230MixQuantInfo obj = (K230MixQuantInfo)((Marker)childrenOfRangeOf.Children[1].Nodes[0].Expr).MixQuantInfo;
					if ((obj != null && !obj.PermitInt16Quant && list6[k][0] == DataTypes.Int16) || (num2 == 1 && ((((Call)childrenOfRangeOf.Expr).Target is FakeActivation && list6[k][0] == DataTypes.Int16) || (!(((Call)childrenOfRangeOf.Expr).Target is FakeActivation) && list6[k][0] == DataTypes.Float16))))
					{
						continue;
					}
					if (num2 == 2)
					{
						K230MixQuantInfo obj2 = (K230MixQuantInfo)((Marker)childrenOfRangeOf.Children[2].Nodes[0].Expr).MixQuantInfo;
						if ((obj2 != null && !obj2.PermitInt16Quant && list6[k][1] == DataTypes.Int16) || (((Call)childrenOfRangeOf.Expr).Target is FakeActivation && list6[k][1] == DataTypes.Int16) || (!(((Call)childrenOfRangeOf.Expr).Target is FakeActivation) && list6[k][0] == DataTypes.Float16 && list6[k][1] == DataTypes.Float16))
						{
							continue;
						}
					}
					List<Expr> list7 = new List<Expr>();
					List<List<QuantParam>> list8 = new List<List<QuantParam>>();
					List<Memory<float>> list9 = new List<Memory<float>>();
					for (int l = 0; l < list4.Count; l++)
					{
						if (quantizeOptions.UseSquant)
						{
							Tensor item5 = list4[l].Item2;
							if (item5 != null && item5.Rank == 2)
							{
								Tensor item6 = list4[l].Item2;
								if (list6[k][1] == DataTypes.UInt8)
								{
									Dictionary<ENode, Tensor> dictionary2 = new Dictionary<ENode, Tensor>();
									dictionary2.Add(list4[l].Item1.Keys.First(), ((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo.U8FineTunedWeights.Value);
									if (((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo?.U8FineTunedWeightsRangesByChannel != null)
									{
										item6 = (((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo?.U8FineTunedWeightsRangesByChannel).Value;
									}
									list4[l] = new Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>(dictionary2, item6, list4[l].Item3);
								}
								if (list6[k][1] == DataTypes.Int8)
								{
									Dictionary<ENode, Tensor> dictionary3 = new Dictionary<ENode, Tensor>();
									dictionary3.Add(list4[l].Item1.Keys.First(), (((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo?.I8FineTunedWeights).Value);
									if (((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo?.I8FineTunedWeightsRangesByChannel != null)
									{
										item6 = ((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo.I8FineTunedWeightsRangesByChannel.Value;
									}
									list4[l] = new Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>(dictionary3, item6, list4[l].Item3);
								}
								if (list6[k][1] == DataTypes.Int16)
								{
									Dictionary<ENode, Tensor> dictionary4 = new Dictionary<ENode, Tensor>();
									dictionary4.Add(list4[l].Item1.Keys.First(), ((K230MixQuantInfo)((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo).I16FineTunedWeights.Value);
									if (((K230MixQuantInfo)((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo)?.I16FineTunedWeightsRangesByChannel != null)
									{
										item6 = ((K230MixQuantInfo)((Marker)childrenOfRangeOf.Children[l + 1].Nodes[0].Expr).MixQuantInfo).I16FineTunedWeightsRangesByChannel.Value;
									}
									list4[l] = new Tuple<IReadOnlyDictionary<ENode, Tensor>, Tensor, bool>(dictionary4, item6, list4[l].Item3);
								}
							}
						}
						Expr item7 = new TensorConst(list4[l].Item1.Values.ToArray()[0]);
						List<QuantParam> list10 = new List<QuantParam>();
						list10.Add(new QuantParam(0, 1f));
						if (list4[l].Item3)
						{
							DataType dataType = list6[k][l];
							flag2 = true;
							Span<float> span = MemoryMarshal.Cast<byte, float>(list4[l].Item1.Values.ToArray()[0].BytesBuffer);
							list9.Add(span.ToArray());
							QuantMode quantMode = ((!(dataType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
							int bits = ((dataType == DataTypes.Int16) ? 12 : 8);
							if (list4[l].Item2.Shape.Rank == 1)
							{
								if (!(((Call)childrenOfRangeOf.Expr).Target is FakeActivation) || !(dataType == DataTypes.Float16))
								{
									list10[0] = QuantUtility.GetQuantParam(new ValueRange<float>((float)list4[l].Item2[new int[1]], (float)list4[l].Item2[new int[1] { 1 }]), bits, quantMode);
									for (int m = 0; m < span.Length; m++)
									{
										double num3 = (double)((float)Math.Round((double)span[m] / (double)list10[0].Scale + (double)list10[0].ZeroPoint) - (float)list10[0].ZeroPoint) * (double)list10[0].Scale;
										span[m] = (float)num3;
									}
								}
							}
							else
							{
								if (list4[l].Item2.Shape.Rank != 2)
								{
									throw new AggregateException("Invalid range values");
								}
								list10.Clear();
								int fixedValue = list4[l].Item2.Shape[0].FixedValue;
								int num4 = span.Length / fixedValue;
								for (int n = 0; n < fixedValue; n++)
								{
									list10.Add(QuantUtility.GetQuantParam(new ValueRange<float>((float)list4[l].Item2[new int[2] { n, 0 }], (float)list4[l].Item2[new int[2] { n, 1 }]), bits, quantMode));
								}
								for (int num5 = 0; num5 < span.Length; num5++)
								{
									double num6 = (double)((float)Math.Round((double)span[num5] / (double)list10[num5 / num4].Scale + (double)list10[num5 / num4].ZeroPoint) - (float)list10[num5 / num4].ZeroPoint) * (double)list10[num5 / num4].Scale;
									span[num5] = (float)num6;
								}
							}
							list7.Add(item7);
						}
						else
						{
							list7.Add(item7);
						}
						list8.Add(list10);
					}
					if (!flag2)
					{
						continue;
					}
					EGraph eGraph = new EGraph();
					Call expr = new Call(childrenOfRangeOf.Children[0].Nodes[0].Expr, list7.ToArray());
					eGraph.Add(expr);
					List<ENode> list11 = new List<ENode>();
					for (int num7 = 0; num7 < eGraph.Nodes.ToList().Count; num7++)
					{
						ENode eNode = eGraph.Nodes.ToArray()[num7];
						if (eNode.Children.Count != 0)
						{
							list11.Add(eNode);
						}
					}
					DumpScope dumpScope4 = new DumpScope("eval_sim");
					try
					{
						using CalibrationEvaluator calibrationEvaluator3 = new CalibrationEvaluator(null, list11);
						Span<float> v = MemoryMarshal.Cast<byte, float>(calibrationEvaluator3.Evaluate().Values.ToArray()[0].BytesBuffer);
						Span<float> v2 = MemoryMarshal.Cast<byte, float>(readOnlyDictionary2[childrenOfRangeOf].BytesBuffer);
						float cosineSimilarity = Nncase.Quantization.Utility.GetCosineSimilarity(v, v2);
						list3.Add(new Tuple<List<DataType>, List<List<QuantParam>>, float>(list6[k], list8, cosineSimilarity));
						list2.Add(new Tuple<List<DataType>, List<List<QuantParam>>, float>(list6[k], list8, cosineSimilarity));
						for (int num8 = 0; num8 < list4.Count; num8++)
						{
							if (list4[num8].Item3)
							{
								Span<float> span2 = MemoryMarshal.Cast<byte, float>(list4[num8].Item1.Values.ToArray()[0].BytesBuffer);
								float[] array = list9[num8].ToArray();
								for (int num9 = 0; num9 < span2.Length; num9++)
								{
									span2[num9] = array[num9];
								}
							}
						}
					}
					finally
					{
						((IDisposable)dumpScope4).Dispose();
					}
				}
			}
			List<Tuple<List<DataType>, List<List<QuantParam>>, float>> list12 = new List<Tuple<List<DataType>, List<List<QuantParam>>, float>>();
			int num10 = list2.Count / samples.Count;
			List<float> list13 = new List<float>(new float[num10]);
			for (int num11 = 0; num11 < list2.Count; num11++)
			{
				list13[num11 % num10] += list2[num11].Item3;
			}
			for (int num12 = 0; num12 < num10; num12++)
			{
				list12.Add(new Tuple<List<DataType>, List<List<QuantParam>>, float>(list2[num12].Item1, list2[num12].Item2, list13[num12] / (float)samples.Count));
			}
			if (list12.Count == 0)
			{
				continue;
			}
			Tuple<List<DataType>, List<List<QuantParam>>, float> tuple2;
			Tuple<List<DataType>, List<List<QuantParam>>, float> tuple;
			Tuple<List<DataType>, List<List<QuantParam>>, float> tuple3 = (tuple2 = (tuple = new Tuple<List<DataType>, List<List<QuantParam>>, float>(new List<DataType>(), new List<List<QuantParam>>(), 0f)));
			for (int num13 = 0; num13 < list12.Count; num13++)
			{
				if ((list12[num13].Item1.Count != 1 || (!(list12[num13].Item1[0] == DataTypes.Int16) && !(list12[num13].Item1[0] == DataTypes.Float16))) && (list12[num13].Item1.Count != 2 || (!(list12[num13].Item1[0] == DataTypes.Int16) && !(list12[num13].Item1[1] == DataTypes.Int16) && (!(list12[num13].Item1[0] == DataTypes.Float16) || !(list12[num13].Item1[1] == DataTypes.Float16)))) && list12[num13].Item3 > tuple.Item3)
				{
					tuple = list12[num13];
				}
			}
			for (int num14 = 0; num14 < list12.Count; num14++)
			{
				if (list12[num14].Item3 > tuple2.Item3)
				{
					tuple2 = list12[num14];
				}
			}
			for (int num15 = 0; num15 < list12.Count; num15++)
			{
				if ((list12[num15].Item1.Count != 1 || !(list12[num15].Item1[0] != DataTypes.Int16)) && (list12[num15].Item1.Count != 2 || !(list12[num15].Item1[0] != DataTypes.Int16) || !(list12[num15].Item1[1] != DataTypes.Int16)) && list12[num15].Item3 > tuple3.Item3)
				{
					tuple3 = list12[num15];
				}
			}
			float num16 = 0.99f;
			Tuple<List<DataType>, List<List<QuantParam>>, float> tuple4 = ((!(tuple.Item3 < num16 * tuple2.Item3)) ? tuple : ((!(tuple3.Item3 < num16 * tuple2.Item3)) ? tuple3 : tuple2));
			((Call)childrenOfRangeOf.Expr).EnodeQuantConfigWithCosine = list12;
			((Call)childrenOfRangeOf.Expr).EnodeBestQuantConfigWithCosine = tuple4;
			int num17 = 0;
			for (int num18 = 0; num18 < childrenOfRangeOf.Children.Count; num18++)
			{
				if (Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsWildcard()).MatchLeaf(childrenOfRangeOf.Children[num18].Nodes[0].Expr))
				{
					if (((Marker)childrenOfRangeOf.Children[num18].Nodes[0].Expr).MixQuantInfo == null)
					{
						((Marker)childrenOfRangeOf.Children[num18].Nodes[0].Expr).MixQuantInfo = new K230MixQuantInfo();
					}
					((Marker)childrenOfRangeOf.Children[num18].Nodes[0].Expr).MixQuantInfo.HasBindedMixQuantInfo = true;
					((Marker)childrenOfRangeOf.Children[num18].Nodes[0].Expr).MixQuantInfo.MarkerQuantType = tuple4.Item1[num17];
					((Marker)childrenOfRangeOf.Children[num18].Nodes[0].Expr).MixQuantInfo.QuantParameter = tuple4.Item2[num17];
					num17++;
				}
			}
			dictionary.Add(childrenOfRangeOf, list12);
		}
		return dictionary;
	}
}
