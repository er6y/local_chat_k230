using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_W_CONF : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_W_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo kernel_h = new ParameterInfo(typeof(DM_LOAD_W_CONF), 2, "kernel_h", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo kernel_w = new ParameterInfo(typeof(DM_LOAD_W_CONF), 3, "kernel_w", TypePatternUtility.IsIntegralScalar());

	public DM_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rstride_oc { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[kernel_h]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[kernel_w]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(rstride_oc, 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_W_CONF(DM_CONF_FUNCTION funct4, GP_REGISTER rstrideOc)
	{
		this.funct4 = funct4;
		rstride_oc = rstrideOc;
	}

	public DM_LOAD_W_CONF With(DM_CONF_FUNCTION? funct4 = null, GP_REGISTER? rstrideOc = null)
	{
		return new DM_LOAD_W_CONF(funct4 ?? this.funct4, rstrideOc ?? rstride_oc);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_W_CONF);
	}

	public bool Equals(DM_LOAD_W_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4))
		{
			return rstride_oc.Equals(other.rstride_oc);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rstride_oc));
	}
}
