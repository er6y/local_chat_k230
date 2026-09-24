using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class L2_LOAD_CONF : InstructionOp, ISerializeInst, IEquatable<L2_LOAD_CONF?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(L2_LOAD_CONF), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public SHAPE_REGISTER rstride_d { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public L2_DATATYPE l2_datatype { get; }

	public DDR_DATATYPE ddr_datatype { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.l2_load_conf, 7);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(l2_datatype, 2);
		bitWriter.Write(ddr_datatype, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 14);
		bitWriter.Flush();
		writer.Write(array);
	}

	public L2_LOAD_CONF(SHAPE_REGISTER rstrideD, SHAPE_REGISTER rstrideS, L2_DATATYPE l2Datatype, DDR_DATATYPE ddrDatatype)
	{
		rstride_d = rstrideD;
		rstride_s = rstrideS;
		l2_datatype = l2Datatype;
		ddr_datatype = ddrDatatype;
	}

	public L2_LOAD_CONF With(SHAPE_REGISTER? rstrideD = null, SHAPE_REGISTER? rstrideS = null, L2_DATATYPE? l2Datatype = null, DDR_DATATYPE? ddrDatatype = null)
	{
		return new L2_LOAD_CONF(rstrideD ?? rstride_d, rstrideS ?? rstride_s, l2Datatype ?? l2_datatype, ddrDatatype ?? ddr_datatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as L2_LOAD_CONF);
	}

	public bool Equals(L2_LOAD_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rstride_d.Equals(other.rstride_d) && rstride_s.Equals(other.rstride_s) && l2_datatype.Equals(other.l2_datatype))
		{
			return ddr_datatype.Equals(other.ddr_datatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rstride_d, rstride_s, l2_datatype, ddr_datatype));
	}
}
