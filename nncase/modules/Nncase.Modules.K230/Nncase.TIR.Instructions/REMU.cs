using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class REMU : InstructionOp, ISerializeInst, IEquatable<REMU?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(REMU), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public GP_REGISTER rs1 { get; }

	public ARITHMETIC_FUNCTION funct5 { get; }

	public GP_REGISTER rs2 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.arithm, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(rs1, 5);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rs2, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public REMU(GP_REGISTER rd, GP_REGISTER rs1, ARITHMETIC_FUNCTION funct5, GP_REGISTER rs2)
	{
		this.rd = rd;
		this.rs1 = rs1;
		this.funct5 = funct5;
		this.rs2 = rs2;
	}

	public REMU With(GP_REGISTER? rd = null, GP_REGISTER? rs1 = null, ARITHMETIC_FUNCTION? funct5 = null, GP_REGISTER? rs2 = null)
	{
		return new REMU(rd ?? this.rd, rs1 ?? this.rs1, funct5 ?? this.funct5, rs2 ?? this.rs2);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as REMU);
	}

	public bool Equals(REMU? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rd.Equals(other.rd) && rs1.Equals(other.rs1) && funct5.Equals(other.funct5))
		{
			return rs2.Equals(other.rs2);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd, rs1, funct5, rs2));
	}
}
