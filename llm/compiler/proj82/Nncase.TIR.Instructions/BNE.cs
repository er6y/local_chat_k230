using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class BNE : InstructionOp, ISerializeInst, IEquatable<BNE?>
{
	public static readonly ParameterInfo offset = new ParameterInfo(typeof(BNE), 0, "offset", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rs1 { get; }

	public GP_REGISTER rs2 { get; }

	public BRANCH_FUNCTION funct3 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.branch, 7);
		bitWriter.Write(rs1, 5);
		bitWriter.Write(rs2, 5);
		bitWriter.Write(funct3, 3);
		bitWriter.Write(((TensorConst)call[offset]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public BNE(GP_REGISTER rs1, GP_REGISTER rs2, BRANCH_FUNCTION funct3)
	{
		this.rs1 = rs1;
		this.rs2 = rs2;
		this.funct3 = funct3;
	}

	public BNE With(GP_REGISTER? rs1 = null, GP_REGISTER? rs2 = null, BRANCH_FUNCTION? funct3 = null)
	{
		return new BNE(rs1 ?? this.rs1, rs2 ?? this.rs2, funct3 ?? this.funct3);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as BNE);
	}

	public bool Equals(BNE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rs1.Equals(other.rs1) && rs2.Equals(other.rs2))
		{
			return funct3.Equals(other.funct3);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rs1, rs2, funct3));
	}
}
