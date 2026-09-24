namespace Nncase.Passes.Rules.K230;

public class BoxOnGlb
{
	private readonly ItemName _ownerName;

	private readonly int[] _box;

	public ItemName OwnerName => _ownerName;

	public int[] Box => _box;

	public BoxOnGlb(int[] box, ItemName ownerName)
	{
		_box = box;
		_box = box;
		_ownerName = ownerName;
	}

	public override string ToString()
	{
		return $"[w: {Box[0]}, h: {Box[1]}]";
	}
}
