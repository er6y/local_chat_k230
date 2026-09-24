using System;
using System.IO;
using Nncase.CodeGen;
using Nncase.CodeGen.StackVM;
using Nncase.PatternMatch;
using Nncase.Targets;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class DynamicGNNEMatMul : CustomOp, IEquatable<DynamicGNNEMatMul?>
{
	public static readonly ParameterInfo Text = new ParameterInfo(typeof(DynamicGNNEMatMul), 0, "text", TypePatternUtility.HasDataType(DataTypes.UInt8) & TypePatternUtility.HasRank(1));

	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(DynamicGNNEMatMul), 1, "input_a", TypePatternUtility.HasRank(4));

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(DynamicGNNEMatMul), 2, "input_b", TypePatternUtility.HasRank(4));

	public static readonly ParameterInfo InputABias = new ParameterInfo(typeof(DynamicGNNEMatMul), 3, "input_a_bias", TypePatternUtility.HasRank(4) & TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo InputBBias = new ParameterInfo(typeof(DynamicGNNEMatMul), 4, "input_b_bias", TypePatternUtility.IsScalar() & TypePatternUtility.HasDataType(DataTypes.Int32));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(DynamicGNNEMatMul), 5, "act", TypePatternUtility.HasDataType(DataTypes.Float16) & TypePatternUtility.HasRank(4));

	public static readonly ParameterInfo ShiftBits = new ParameterInfo(typeof(DynamicGNNEMatMul), 6, "shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo DynamicChannel = new ParameterInfo(typeof(DynamicGNNEMatMul), 7, "dynamic_channel", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public PrimType OutputDType { get; }

	public override ModuleType ModuleType => K230Target.ModuleType;

	public override string RegisteredName => "K230DynamicGNNEMatMul";

	public override byte[] SerializeFields()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter writer = new BinaryWriter(memoryStream))
		{
			new StackVMEmitter(writer).Write(OutputDType);
		}
		return memoryStream.ToArray();
	}

	public DynamicGNNEMatMul(PrimType outputDType)
	{
		OutputDType = outputDType;
	}

	public DynamicGNNEMatMul With(PrimType? outputDType = null)
	{
		return new DynamicGNNEMatMul(outputDType ?? OutputDType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DynamicGNNEMatMul);
	}

	public bool Equals(DynamicGNNEMatMul? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return OutputDType.Equals(other.OutputDType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(OutputDType));
	}
}
