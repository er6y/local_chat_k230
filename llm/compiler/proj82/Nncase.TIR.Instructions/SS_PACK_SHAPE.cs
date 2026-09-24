using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class SS_PACK_SHAPE : InstructionOp, ISerializeInst, IEquatable<SS_PACK_SHAPE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(SS_PACK_SHAPE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rn { get; }

	public GP_REGISTER rc { get; }

	public GP_REGISTER rh { get; }

	public GP_REGISTER rw { get; }

	public SHAPE_REGISTER rss { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ss_pack_shape, 7);
		bitWriter.Write(rn, 5);
		bitWriter.Write(rc, 5);
		bitWriter.Write(rh, 5);
		bitWriter.Write(rw, 5);
		bitWriter.Write(rss, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public SS_PACK_SHAPE(GP_REGISTER rn, GP_REGISTER rc, GP_REGISTER rh, GP_REGISTER rw, SHAPE_REGISTER rss)
	{
		this.rn = rn;
		this.rc = rc;
		this.rh = rh;
		this.rw = rw;
		this.rss = rss;
	}

	public SS_PACK_SHAPE With(GP_REGISTER? rn = null, GP_REGISTER? rc = null, GP_REGISTER? rh = null, GP_REGISTER? rw = null, SHAPE_REGISTER? rss = null)
	{
		return new SS_PACK_SHAPE(rn ?? this.rn, rc ?? this.rc, rh ?? this.rh, rw ?? this.rw, rss ?? this.rss);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as SS_PACK_SHAPE);
	}

	public bool Equals(SS_PACK_SHAPE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rn.Equals(other.rn) && rc.Equals(other.rc) && rh.Equals(other.rh) && rw.Equals(other.rw))
		{
			return rss.Equals(other.rss);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rn, rc, rh, rw, rss));
	}
}
