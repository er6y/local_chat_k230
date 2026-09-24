using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_W : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_W?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_W), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_W), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_W), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_s { get; }

	public GP_REGISTER raddr_bw { get; }

	public SHAPE_REGISTER r_iochannels { get; }

	public DM_LOAD_W_DEST dest_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_load_w, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(raddr_bw, 5);
		bitWriter.Write(r_iochannels, 3);
		bitWriter.Write(dest_type, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_W(GP_REGISTER raddrS, GP_REGISTER raddrBw, SHAPE_REGISTER rIochannels, DM_LOAD_W_DEST destType)
	{
		raddr_s = raddrS;
		raddr_bw = raddrBw;
		r_iochannels = rIochannels;
		dest_type = destType;
	}

	public DM_LOAD_W With(GP_REGISTER? raddrS = null, GP_REGISTER? raddrBw = null, SHAPE_REGISTER? rIochannels = null, DM_LOAD_W_DEST? destType = null)
	{
		return new DM_LOAD_W(raddrS ?? raddr_s, raddrBw ?? raddr_bw, rIochannels ?? r_iochannels, destType ?? dest_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_W);
	}

	public bool Equals(DM_LOAD_W? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_s.Equals(other.raddr_s) && raddr_bw.Equals(other.raddr_bw) && r_iochannels.Equals(other.r_iochannels))
		{
			return dest_type.Equals(other.dest_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_s, raddr_bw, r_iochannels, dest_type));
	}
}
