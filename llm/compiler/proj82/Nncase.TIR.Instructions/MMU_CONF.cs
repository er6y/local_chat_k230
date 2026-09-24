using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MMU_CONF : InstructionOp, ISerializeInst, IEquatable<MMU_CONF?>
{
	public static readonly ParameterInfo mmu_id = new ParameterInfo(typeof(MMU_CONF), 0, "mmu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MMU_CONF), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rstart { get; }

	public GP_REGISTER rdepth { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mmu_conf, 7);
		bitWriter.Write(rstart, 5);
		bitWriter.Write(rdepth, 5);
		bitWriter.Write(((TensorConst)call[mmu_id]).Value.ToScalar<uint>(), 4);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 11);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MMU_CONF(GP_REGISTER rstart, GP_REGISTER rdepth)
	{
		this.rstart = rstart;
		this.rdepth = rdepth;
	}

	public MMU_CONF With(GP_REGISTER? rstart = null, GP_REGISTER? rdepth = null)
	{
		return new MMU_CONF(rstart ?? this.rstart, rdepth ?? this.rdepth);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MMU_CONF);
	}

	public bool Equals(MMU_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rstart.Equals(other.rstart))
		{
			return rdepth.Equals(other.rdepth);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rstart, rdepth));
	}
}
