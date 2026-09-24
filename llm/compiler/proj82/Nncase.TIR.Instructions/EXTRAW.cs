using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class EXTRAW : InstructionOp, ISerializeInst, IEquatable<EXTRAW?>
{
	public static readonly ParameterInfo extrd = new ParameterInfo(typeof(EXTRAW), 0, "extrd", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo imm = new ParameterInfo(typeof(EXTRAW), 1, "imm", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rs { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.extraw, 7);
		bitWriter.Write(((TensorConst)call[extrd]).Value.ToScalar<uint>(), 10);
		bitWriter.Write(rs, 5);
		bitWriter.Write(((TensorConst)call[imm]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public EXTRAW(GP_REGISTER rs)
	{
		this.rs = rs;
	}

	public EXTRAW With(GP_REGISTER? rs = null)
	{
		return new EXTRAW(rs ?? this.rs);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as EXTRAW);
	}

	public bool Equals(EXTRAW? other)
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
