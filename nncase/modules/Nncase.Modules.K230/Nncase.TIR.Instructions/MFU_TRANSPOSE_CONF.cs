using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_TRANSPOSE_CONF : InstructionOp, ISerializeInst, IEquatable<MFU_TRANSPOSE_CONF?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_TRANSPOSE_CONF), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public L2_DATATYPE l2_datatype { get; }

	public MFU_TRANS_PERMUTE permute { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(l2_datatype, 2);
		bitWriter.Write(permute, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 7);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_TRANSPOSE_CONF(MFU_CONF_FUNCTION funct5, SHAPE_REGISTER rstrideD, SHAPE_REGISTER rstrideS, L2_DATATYPE l2Datatype, MFU_TRANS_PERMUTE permute)
	{
		this.funct5 = funct5;
		rstride_d = rstrideD;
		rstride_s = rstrideS;
		l2_datatype = l2Datatype;
		this.permute = permute;
	}

	public MFU_TRANSPOSE_CONF With(MFU_CONF_FUNCTION? funct5 = null, SHAPE_REGISTER? rstrideD = null, SHAPE_REGISTER? rstrideS = null, L2_DATATYPE? l2Datatype = null, MFU_TRANS_PERMUTE? permute = null)
	{
		return new MFU_TRANSPOSE_CONF(funct5 ?? this.funct5, rstrideD ?? rstride_d, rstrideS ?? rstride_s, l2Datatype ?? l2_datatype, permute ?? this.permute);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_TRANSPOSE_CONF);
	}

	public bool Equals(MFU_TRANSPOSE_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rstride_d.Equals(other.rstride_d) && rstride_s.Equals(other.rstride_s) && l2_datatype.Equals(other.l2_datatype))
		{
			return permute.Equals(other.permute);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rstride_d, rstride_s, l2_datatype, permute));
	}
}
