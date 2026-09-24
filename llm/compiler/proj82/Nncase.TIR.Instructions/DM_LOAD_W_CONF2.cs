using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_W_CONF2 : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_W_CONF2?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF2), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF2), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_W_CONF2), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public DM_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rgroups { get; }

	public GP_REGISTER rgoc { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rgroups, 5);
		bitWriter.Write(rgoc, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_W_CONF2(DM_CONF_FUNCTION funct4, GP_REGISTER rgroups, GP_REGISTER rgoc)
	{
		this.funct4 = funct4;
		this.rgroups = rgroups;
		this.rgoc = rgoc;
	}

	public DM_LOAD_W_CONF2 With(DM_CONF_FUNCTION? funct4 = null, GP_REGISTER? rgroups = null, GP_REGISTER? rgoc = null)
	{
		return new DM_LOAD_W_CONF2(funct4 ?? this.funct4, rgroups ?? this.rgroups, rgoc ?? this.rgoc);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_W_CONF2);
	}

	public bool Equals(DM_LOAD_W_CONF2? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rgroups.Equals(other.rgroups))
		{
			return rgoc.Equals(other.rgoc);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rgroups, rgoc));
	}
}
