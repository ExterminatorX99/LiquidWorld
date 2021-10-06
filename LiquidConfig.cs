using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace LiquidWorld
{
	public sealed class LiquidConfig : ModConfig
	{
		[DefaultValue(true)]
		[Tooltip("Whether the mod is active")]
		public bool Active;

		[Tooltip("Single:   Blocks release what is selected by LiquidType\n" +
				 "Chance:   Blocks release one of the activated liquids\n" +
				 "Combined: Blocks release multiple liquids")]
		[DefaultValue(ReplaceType.Single)]
		public ReplaceType ReplaceType;

		[Tooltip("Only works with ReplaceType: Combine")]
		[DefaultValue(false)]
		public bool BEEEEEES;

		[Label("Liquid")]
		[Tooltip("Liquid to be used when in 'Single' mode")]
		[DefaultValue(LiquidType.Lava)]
		public LiquidType LiquidType;

		[Tooltip("Amount of liquid that appears when a block is mined. Default for Hellstone is 128")]
		[DefaultValue(128)]
		public byte LiquidAmount;

		[Header("Water")]
		[Label("Blocks release Water when mined")]
		[DefaultValue(true)]
		public bool Water;

		[Label("Weight")]
		[Tooltip("The higher the weight the higher the chance")]
		[Range(0, 1000)]
		[DefaultValue(100)]
		public int WaterWeight;

		[Header("Lava")]
		[Label("Blocks release Lava when mined")]
		[DefaultValue(true)]
		public bool Lava;

		[Label("Weight")]
		[Tooltip("The higher the weight the higher the chance")]
		[Range(0, 1000)]
		[DefaultValue(100)]
		public int LavaWeight;

		[Header("Honey")]
		[Label("Blocks release Honey when mined")]
		[DefaultValue(true)]
		public bool Honey;

		[Label("Weight")]
		[Tooltip("The higher the weight the higher the chance")]
		[Range(0, 1000)]
		[DefaultValue(100)]
		public int HoneyWeight;

		public static LiquidConfig Instance => ModContent.GetInstance<LiquidConfig>();
		public override ConfigScope Mode => ConfigScope.ServerSide;

		public override void OnChanged()
		{
			if (BEEEEEES)
				ReplaceType = ReplaceType.Combined;
		}

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
	}
}
