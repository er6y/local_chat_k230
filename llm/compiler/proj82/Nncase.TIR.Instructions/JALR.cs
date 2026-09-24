using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class JALR : InstructionOp, ISerializeInst, IEquatable<JALR?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(JALR), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo offset = new ParameterInfo(typeof(JALR), 1, "offset", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public GP_REGISTER rs { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.jalr, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(rs, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[offset]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public JALR(GP_REGISTER rd, GP_REGISTER rs)
	{
		this.rd = rd;
		this.rs = rs;
	}

	public JALR With(GP_REGISTER? rd = null, GP_REGISTER? rs = null)
	{
		return new JALR(rd ?? this.rd, rs ?? this.rs);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as JALR);
	}

	public bool Equals(JALR? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rd.Equals(other.rd))
		{
			return rs.Equals(other.rs);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd, rs));
	}
}
