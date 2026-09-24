using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class EXTRW : InstructionOp, ISerializeInst, IEquatable<EXTRW?>
{
	public static readonly ParameterInfo extrd = new ParameterInfo(typeof(EXTRW), 0, "extrd", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo imm = new ParameterInfo(typeof(EXTRW), 1, "imm", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rs { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.extrw, 7);
		bitWriter.Write(((TensorConst)call[extrd]).Value.ToScalar<uint>(), 10);
		bitWriter.Write(rs, 5);
		bitWriter.Write(((TensorConst)call[imm]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public EXTRW(GP_REGISTER rs)
	{
		this.rs = rs;
	}

	public EXTRW With(GP_REGISTER? rs = null)
	{
		return new EXTRW(rs ?? this.rs);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as EXTRW);
	}

	public bool Equals(EXTRW? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return rs.Equals(other.rs);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rs));
	}
}
