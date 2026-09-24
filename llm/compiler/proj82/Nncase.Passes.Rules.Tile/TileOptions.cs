namespace Nncase.Passes.Rules.Tile;

public sealed record TileOptions(int[] TargetTileSize, bool ForceFence = false);
