using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MMU_SETID : InstructionOp, ISerializeInst, IEquatable<MMU_SETID?>
{
	public static readonly ParameterInfo mmu_id = new ParameterInfo(typeof(MMU_SETID), 0, "mmu_id", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mmu_set_id, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(((TensorConst)call[mmu_id]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MMU_SETID(GP_REGISTER rd)
	{
		this.rd = rd;
	}

	public MMU_SETID With(GP_REGISTER? rd = null)
	{
		return new MMU_SETID(rd ?? this.rd);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MMU_SETID);
	}

	public bool Equals(MMU_SETID? other)
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
