using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_L1_CONF : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_L1_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_L1_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_L1_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_L1_CONF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public DM_CONF_FUNCTION funct4 { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public L2_DATATYPE datatype { get; }

	public L1_TYPE l1_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(datatype, 2);
		bitWriter.Write(l1_type, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 8);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_L1_CONF(DM_CONF_FUNCTION funct4, SHAPE_REGISTER rstrideS, L2_DATATYPE datatype, L1_TYPE l1Type)
	{
		this.funct4 = funct4;
		rstride_s = rstrideS;
		this.datatype = datatype;
		l1_type = l1Type;
	}

	public DM_LOAD_L1_CONF With(DM_CONF_FUNCTION? funct4 = null, SHAPE_REGISTER? rstrideS = null, L2_DATATYPE? datatype = null, L1_TYPE? l1Type = null)
	{
		return new DM_LOAD_L1_CONF(funct4 ?? this.funct4, rstrideS ?? rstride_s, datatype ?? this.datatype, l1Type ?? l1_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_L1_CONF);
	}

	public bool Equals(DM_LOAD_L1_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rstride_s.Equals(other.rstride_s) && datatype.Equals(other.datatype))
		{
			return l1_type.Equals(other.l1_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rstride_s, datatype, l1_type));
	}
}
