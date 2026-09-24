using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_MEMSET : InstructionOp, ISerializeInst, IEquatable<MFU_MEMSET?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_MEMSET), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public GP_REGISTER rv { get; }

	public SHAPE_REGISTER rstride { get; }

	public SHAPE_REGISTER rshape { get; }

	public L2_DATATYPE l2_datatype { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_memset, 7);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(rv, 5);
		bitWriter.Write(rstride, 3);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(l2_datatype, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 7);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_MEMSET(GP_REGISTER raddrD, GP_REGISTER rv, SHAPE_REGISTER rstride, SHAPE_REGISTER rshape, L2_DATATYPE l2Datatype)
	{
		raddr_d = raddrD;
		this.rv = rv;
		this.rstride = rstride;
		this.rshape = rshape;
		l2_datatype = l2Datatype;
	}

	public MFU_MEMSET With(GP_REGISTER? raddrD = null, GP_REGISTER? rv = null, SHAPE_REGISTER? rstride = null, SHAPE_REGISTER? rshape = null, L2_DATATYPE? l2Datatype = null)
	{
		return new MFU_MEMSET(raddrD ?? raddr_d, rv ?? this.rv, rstride ?? this.rstride, rshape ?? this.rshape, l2Datatype ?? l2_datatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_MEMSET);
	}

	public bool Equals(MFU_MEMSET? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && rv.Equals(other.rv) && rstride.Equals(other.rstride) && rshape.Equals(other.rshape))
		{
			return l2_datatype.Equals(other.l2_datatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, rv, rstride, rshape, l2_datatype));
	}
}
