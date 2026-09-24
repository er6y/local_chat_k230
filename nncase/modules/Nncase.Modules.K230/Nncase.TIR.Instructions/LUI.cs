using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class LUI : InstructionOp, ISerializeInst, IEquatable<LUI?>
{
	public static readonly ParameterInfo imm = new ParameterInfo(typeof(LUI), 0, "imm", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.lui, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(((TensorConst)call[imm]).Value.ToScalar<uint>(), 20);
		bitWriter.Flush();
		writer.Write(array);
	}

	public LUI(GP_REGISTER rd)
	{
		this.rd = rd;
	}

	public LUI With(GP_REGISTER? rd = null)
	{
		return new LUI(rd ?? this.rd);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as LUI);
	}

	public bool Equals(LUI? other)
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
