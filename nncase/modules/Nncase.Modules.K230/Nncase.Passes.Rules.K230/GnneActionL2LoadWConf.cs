using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionL2LoadWConf : GnneAction
{
	public Gpr LenCompressed { get; }

	public Gpr LenDecompressed { get; }

	public DataType L2Datatype { get; }

	public DataType DdrDatatype { get; }

	public bool EnableDecompress { get; }

	public GnneActionL2LoadWConf()
		: base(GnneActionName.L2LoadWConf)
	{
		LenCompressed = new Gpr(-1, -1, needRenewal: false);
		LenDecompressed = new Gpr(-1, -1, needRenewal: false);
		L2Datatype = DataTypes.UInt8;
		DdrDatatype = DataTypes.UInt8;
		EnableDecompress = false;
	}

	public GnneActionL2LoadWConf(Gpr lenCompressed, Gpr lenDecompressed, DataType l2Datatype, DataType ddrDatatype, bool enableDecompress = false)
		: base(GnneActionName.L2LoadWConf)
	{
		LenCompressed = lenCompressed;
		LenDecompressed = lenDecompressed;
		L2Datatype = l2Datatype;
		DdrDatatype = ddrDatatype;
		EnableDecompress = enableDecompress;
	}

	public static bool operator ==(GnneActionL2LoadWConf lhs, GnneActionL2LoadWConf rhs)
	{
		if (lhs.LenCompressed.Value == rhs.LenCompressed.Value && lhs.LenDecompressed.Value == rhs.LenDecompressed.Value && lhs.L2Datatype == rhs.L2Datatype && lhs.DdrDatatype == rhs.DdrDatatype)
		{
			return lhs.EnableDecompress == rhs.EnableDecompress;
		}
		return false;
	}

	public static bool operator !=(GnneActionL2LoadWConf lhs, GnneActionL2LoadWConf rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object? obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj == null)
		{
			return false;
		}
		throw new NotImplementedException();
	}
}
