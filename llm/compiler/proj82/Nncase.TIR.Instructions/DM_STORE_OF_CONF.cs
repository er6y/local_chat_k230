using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_STORE_OF_CONF : InstructionOp, ISerializeInst, IEquatable<DM_STORE_OF_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_STORE_OF_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_STORE_OF_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_STORE_OF_CONF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public DM_CONF_FUNCTION funct4 { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public L2_DATATYPE datatype { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(datatype, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_STORE_OF_CONF(DM_CONF_FUNCTION funct4, SHAPE_REGISTER rstrideD, L2_DATATYPE datatype)
	{
		this.funct4 = funct4;
		rstride_d = rstrideD;
		this.datatype = datatype;
	}

	public DM_STORE_OF_CONF With(DM_CONF_FUNCTION? funct4 = null, SHAPE_REGISTER? rstrideD = null, L2_DATATYPE? datatype = null)
	{
		return new DM_STORE_OF_CONF(funct4 ?? this.funct4, rstrideD ?? rstride_d, datatype ?? this.datatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_STORE_OF_CONF);
	}

	public bool Equals(DM_STORE_OF_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rstride_d.Equals(other.rstride_d))
		{
			return datatype.Equals(other.datatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rstride_d, datatype));
	}
}
