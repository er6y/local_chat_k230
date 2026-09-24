using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class ADDI : InstructionOp, ISerializeInst, IEquatable<ADDI?>
{
	public static readonly ParameterInfo imm = new ParameterInfo(typeof(ADDI), 0, "imm", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public GP_REGISTER rs { get; }

	public ARITHMETIC_IMM_FUNCTION funct5 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.arithm_imm, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(rs, 5);
		bitWriter.Write(funct5, 3);
		bitWriter.Write(((TensorConst)call[imm]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public ADDI(GP_REGISTER rd, GP_REGISTER rs, ARITHMETIC_IMM_FUNCTION funct5)
	{
		this.rd = rd;
		this.rs = rs;
		this.funct5 = funct5;
	}

	public ADDI With(GP_REGISTER? rd = null, GP_REGISTER? rs = null, ARITHMETIC_IMM_FUNCTION? funct5 = null)
	{
		return new ADDI(rd ?? this.rd, rs ?? this.rs, funct5 ?? this.funct5);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ADDI);
	}

	public bool Equals(ADDI? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rd.Equals(other.rd) && rs.Equals(other.rs))
		{
			return funct5.Equals(other.funct5);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd, rs, funct5));
	}
}
