using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_PDP1_CONF1 : InstructionOp, ISerializeInst, IEquatable<MFU_PDP1_CONF1?>
{
	public static readonly ParameterInfo stride_w = new ParameterInfo(typeof(MFU_PDP1_CONF1), 0, "stride_w", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo stride_h = new ParameterInfo(typeof(MFU_PDP1_CONF1), 1, "stride_h", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_PDP1_CONF1), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public PDP_FUNCTION funct2 { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(((TensorConst)call[stride_w]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[stride_h]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(funct2, 2);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_PDP1_CONF1(MFU_CONF_FUNCTION funct5, SHAPE_REGISTER rstrideS, PDP_FUNCTION funct2, SHAPE_REGISTER rstrideD)
	{
		this.funct5 = funct5;
		rstride_s = rstrideS;
		this.funct2 = funct2;
		rstride_d = rstrideD;
	}

	public MFU_PDP1_CONF1 With(MFU_CONF_FUNCTION? funct5 = null, SHAPE_REGISTER? rstrideS = null, PDP_FUNCTION? funct2 = null, SHAPE_REGISTER? rstrideD = null)
	{
		return new MFU_PDP1_CONF1(funct5 ?? this.funct5, rstrideS ?? rstride_s, funct2 ?? this.funct2, rstrideD ?? rstride_d);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_PDP1_CONF1);
	}

	public bool Equals(MFU_PDP1_CONF1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rstride_s.Equals(other.rstride_s) && funct2.Equals(other.funct2))
		{
			return rstride_d.Equals(other.rstride_d);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rstride_s, funct2, rstride_d));
	}
}
