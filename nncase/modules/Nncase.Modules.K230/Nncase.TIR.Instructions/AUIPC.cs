using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class AUIPC : InstructionOp, ISerializeInst, IEquatable<AUIPC?>
{
	public static readonly ParameterInfo imm = new ParameterInfo(typeof(AUIPC), 0, "imm", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.auipc, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(((TensorConst)call[imm]).Value.ToScalar<uint>(), 20);
		bitWriter.Flush();
		writer.Write(array);
	}

	public AUIPC(GP_REGISTER rd)
	{
		this.rd = rd;
	}

	public AUIPC With(GP_REGISTER? rd = null)
	{
		return new AUIPC(rd ?? this.rd);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as AUIPC);
	}

	public bool Equals(AUIPC? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return rd.Equals(other.rd);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd));
	}
}
