using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_STRIDE : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_STRIDE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_STRIDE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public SHAPE_REGISTER rstride_s1 { get; }

	public SHAPE_REGISTER rstride_s2 { get; }

	public SHAPE_REGISTER rstride_d1 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rstride_s1, 3);
		bitWriter.Write(rstride_s2, 3);
		bitWriter.Write(rstride_d1, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 11);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_STRIDE(MFU_CONF_FUNCTION funct5, SHAPE_REGISTER rstrideS1, SHAPE_REGISTER rstrideS2, SHAPE_REGISTER rstrideD1)
	{
		this.funct5 = funct5;
		rstride_s1 = rstrideS1;
		rstride_s2 = rstrideS2;
		rstride_d1 = rstrideD1;
	}

	public MFU_ACT1_CONF_STRIDE With(MFU_CONF_FUNCTION? funct5 = null, SHAPE_REGISTER? rstrideS1 = null, SHAPE_REGISTER? rstrideS2 = null, SHAPE_REGISTER? rstrideD1 = null)
	{
		return new MFU_ACT1_CONF_STRIDE(funct5 ?? this.funct5, rstrideS1 ?? rstride_s1, rstrideS2 ?? rstride_s2, rstrideD1 ?? rstride_d1);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_STRIDE);
	}

	public bool Equals(MFU_ACT1_CONF_STRIDE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rstride_s1.Equals(other.rstride_s1) && rstride_s2.Equals(other.rstride_s2))
		{
			return rstride_d1.Equals(other.rstride_d1);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rstride_s1, rstride_s2, rstride_d1));
	}
}
