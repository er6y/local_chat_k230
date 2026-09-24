using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_L1 : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_L1?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_L1), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_L1), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_L1), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_s { get; }

	public GP_REGISTER rhtoc_window { get; }

	public SHAPE_REGISTER rshape { get; }

	public L1_TYPE l1_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_load_l1, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rhtoc_window, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(l1_type, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_L1(GP_REGISTER raddrS, GP_REGISTER rhtocWindow, SHAPE_REGISTER rshape, L1_TYPE l1Type)
	{
		raddr_s = raddrS;
		rhtoc_window = rhtocWindow;
		this.rshape = rshape;
		l1_type = l1Type;
	}

	public DM_LOAD_L1 With(GP_REGISTER? raddrS = null, GP_REGISTER? rhtocWindow = null, SHAPE_REGISTER? rshape = null, L1_TYPE? l1Type = null)
	{
		return new DM_LOAD_L1(raddrS ?? raddr_s, rhtocWindow ?? rhtoc_window, rshape ?? this.rshape, l1Type ?? l1_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_L1);
	}

	public bool Equals(DM_LOAD_L1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_s.Equals(other.raddr_s) && rhtoc_window.Equals(other.rhtoc_window) && rshape.Equals(other.rshape))
		{
			return l1_type.Equals(other.l1_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_s, rhtoc_window, rshape, l1_type));
	}
}
