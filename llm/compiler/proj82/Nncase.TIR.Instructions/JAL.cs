using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class JAL : InstructionOp, ISerializeInst, IEquatable<JAL?>
{
	public static readonly ParameterInfo offset = new ParameterInfo(typeof(JAL), 0, "offset", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.jal, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(((TensorConst)call[offset]).Value.ToScalar<uint>(), 20);
		bitWriter.Flush();
		writer.Write(array);
	}

	public JAL(GP_REGISTER rd)
	{
		this.rd = rd;
	}

	public JAL With(GP_REGISTER? rd = null)
	{
		return new JAL(rd ?? this.rd);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as JAL);
	}

	public bool Equals(JAL? other)
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
