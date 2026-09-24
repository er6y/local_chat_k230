using System.Collections.Generic;
using System.Linq;

namespace Nncase.Passes.Rules.Tile;

internal sealed class RoofLineInfo
{
	public const string Mac = "Mac";

	public const string FLOPs = "FLOPs";

	public const string OnChipMemTraffic = "OnChipMemTraffic";

	public const string OffChipMemTraffic = "OffChipMemTraffic";

	public const string OffChipMemLWTraffic = "OffChipMemLWTraffic";

	public const string OffChipLoadStoreCnt = "OffChipLoadStoreCnt";

	public const string Header = "Mac, FLOPs, OnChipMemTraffic, OffChipMemTraffic, OffChipMemLWTraffic, OffChipLoadStoreCnt";

	private readonly Dictionary<string, ulong> _opTypeMap = new Dictionary<string, ulong>
	{
		{ "FC", 0uL },
		{ "CONV2D", 1uL },
		{ "DWCONV", 2uL },
		{ "GEMM", 3uL },
		{ "Logit", 4uL },
		{ "Attend", 5uL }
	};

	private readonly Dictionary<string, ulong> _dict;

	private readonly List<string> _operators;

	public List<string> Operators => _operators;

	public ulong this[string name]
	{
		get
		{
			return _dict[name];
		}
		set
		{
			_dict[name] = value;
		}
	}

	public RoofLineInfo()
	{
		_dict = new Dictionary<string, ulong>
		{
			{ "Mac", 0uL },
			{ "FLOPs", 0uL },
			{ "OnChipMemTraffic", 0uL },
			{ "OffChipMemTraffic", 0uL },
			{ "OffChipMemLWTraffic", 0uL },
			{ "OffChipLoadStoreCnt", 0uL }
		};
		_operators = new List<string>();
	}

	public void AddOperator(string name, params (string, ulong)[] values)
	{
		_operators.Add($"{name}\n{string.Join(",", values.Select(((string, ulong) p) => p.Item1))}\n{string.Join(",", values.Select(((string, ulong) p) => p.Item2))}");
	}

	public override string ToString()
	{
		return $"{_dict["Mac"]}, {_dict["FLOPs"]}, {_dict["OnChipMemTraffic"]}, {_dict["OffChipMemTraffic"]}, {_dict["OffChipMemLWTraffic"]}, {_dict["OffChipLoadStoreCnt"]}";
	}
}
