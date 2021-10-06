using Terraria;

namespace LiquidWorld
{
	public static class Extensions
	{
		public static void PlaceLiquid(this Tile tile, int liquidType, byte liquidAmount)
		{
			tile.LiquidType = liquidType;
			tile.LiquidAmount = liquidAmount;
		}
	}
}
