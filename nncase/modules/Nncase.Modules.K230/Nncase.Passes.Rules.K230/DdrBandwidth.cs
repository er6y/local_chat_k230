using System;
using Nncase.IR;
using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public class DdrBandwidth
{
	private long[] _ofBw;

	public long[] TotalBw { get; private set; }

	public long[] IfBw { get; private set; }

	public long[] WBw { get; private set; }

	public long[] OtherBw { get; private set; }

	public DdrBandwidth()
	{
		IfBw = new long[2];
		WBw = new long[2];
		_ofBw = new long[2];
		OtherBw = new long[2];
		TotalBw = new long[2];
	}

	public static int GetBytesPerElement(DataType type)
	{
		if ((object)type != null)
		{
			if (type == DataTypes.Int8 || type == DataTypes.UInt8)
			{
				return 1;
			}
			DataType dataType = type;
			if (dataType == DataTypes.BFloat16 || dataType == DataTypes.Int16 || dataType == DataTypes.Float16)
			{
				return 2;
			}
			DataType dataType2 = type;
			if (dataType2 == DataTypes.Float32 || dataType2 == DataTypes.UInt32 || dataType2 == DataTypes.Int32)
			{
				return 4;
			}
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	public void CalcBw(GnneAction action)
	{
		long num = 0L;
		switch (action.Name)
		{
		case GnneActionName.L2Load:
		{
			GnneActionL2Load obj3 = (GnneActionL2Load)action;
			int shape_size2 = obj3.SliceInfo.Shape_size;
			int bytesPerElement3 = GetBytesPerElement(obj3.Input[GNNELoad.Input].CheckedDataType);
			IfBw[0] += shape_size2 * bytesPerElement3;
			TotalBw[0] += shape_size2 * bytesPerElement3;
			break;
		}
		case GnneActionName.L2LoadWConf:
		{
			GnneActionL2LoadWConf obj2 = (GnneActionL2LoadWConf)action;
			int bytesPerElement2 = GetBytesPerElement(obj2.DdrDatatype);
			num = ((TensorConst)obj2.LenCompressed.Value).Value.ToScalar<int>() * bytesPerElement2;
			break;
		}
		case GnneActionName.L2LoadW:
			WBw[0] += num;
			TotalBw[0] += num;
			break;
		case GnneActionName.L2Store:
		{
			GnneActionL2Store obj = (GnneActionL2Store)action;
			int bytesPerElement = GetBytesPerElement(obj.Output.CheckedDataType);
			int shape_size = obj.SliceInfo.Shape_size;
			_ofBw[0] += shape_size * bytesPerElement;
			TotalBw[0] += shape_size * bytesPerElement;
			break;
		}
		case GnneActionName.L2StoreConf:
			break;
		}
	}

	public void ResetBw()
	{
		IfBw = new long[2];
		WBw = new long[2];
		_ofBw = new long[2];
		OtherBw = new long[2];
		TotalBw = new long[2];
	}
}
