namespace Nncase.Passes.Rules.K230;

public class Gnne_action_mmu_conf : GnneAction
{
	public MmuItem Item { get; }

	public Gpr Start { get; }

	public Gpr Depth { get; }

	public Gnne_action_mmu_conf(MmuItem item, Gpr start, Gpr depth)
		: base(GnneActionName.MmuConf)
	{
		Item = item;
		Start = start;
		Depth = depth;
	}
}
