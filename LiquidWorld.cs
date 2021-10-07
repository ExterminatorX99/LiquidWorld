using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using ILWorldGen = IL.Terraria.WorldGen;

namespace LiquidWorld
{
	public sealed class LiquidWorld : Mod
	{
		public static WeightedRandom<int> WeightedRandom = null!;
		public static ushort? TileTypeToPlace;

		public override void Load()
		{
			WeightedRandom = new WeightedRandom<int>(Main.rand);

			ILWorldGen.KillTile += LiquidEdit;
			ILWorldGen.KillTile += AllowPlace;
		}

		public override void Unload()
		{
			WeightedRandom = null!;
			TileTypeToPlace = null;

			ILWorldGen.KillTile -= LiquidEdit;
			ILWorldGen.KillTile -= AllowPlace;
		}

		private void LiquidEdit(ILContext il)
		{
			ILCursor c = new(il);

			Func<Instruction, bool>[] branchInstructions =
			{
				i => i.MatchLdloc(0),
				i => i.MatchLdfld<Tile>(nameof(Tile.type)),
				i => i.MatchLdcI4(TileID.Hellstone),
				i => i.MatchBneUn(out _),
				i => i.MatchLdarg(1),
				i => i.MatchCall<Main>($"get_{nameof(Main.UnderworldLayer)}"),
				i => i.MatchCgt(),
				i => i.MatchBr(out _),
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(99),
				i => i.MatchLdloc(99),
				i => i.MatchBrfalse(out _)
			};

			if (!c.TryGotoNext(branchInstructions))
			{
				Logger.Fatal("Failed to find Hellstone 'branch` instructions");
				return;
			}

			c.Emit(OpCodes.Ldloc_0);
			c.Emit(OpCodes.Call, typeof(LiquidWorld).GetMethod(nameof(LiquidStuff)));
		}

		private void AllowPlace(ILContext il)
		{
			ILCursor c = new(il);

			Func<Instruction, bool>[] breakInstructions =
			{
				i => i.MatchLdloc(0),
				i => i.MatchLdcI4(0),
				i => i.MatchStfld<Tile>(nameof(Tile.type)),
				i => i.MatchLdloc(0),
				i => i.MatchLdcI4(0),
				i => i.MatchCallvirt<Tile>("inActive")
			};

			if (!c.TryGotoNext(MoveType.After, breakInstructions))
			{
				Logger.Error("Failed to find KillTile 'break` instructions");
				return;
			}

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Call, typeof(LiquidWorld).GetMethod(nameof(PlaceTile)));
		}

		public static void LiquidStuff(Tile tile)
		{
			LiquidConfig config = LiquidConfig.Instance;

			// If not active, do vanilla
			if (WorldGen.gen || !config.Active)
				return;

			switch (config.ReplaceType)
			{
				case ReplaceType.Single:
					tile.PlaceLiquid((int) config.LiquidType, config.LiquidAmount);
					break;
				case ReplaceType.Chance:
					SetupRandom(config);
					tile.PlaceLiquid(WeightedRandom.Get(), config.LiquidAmount);
					break;
				case ReplaceType.Combined:
					SetupRandom(config);
					int liquid1 = WeightedRandom.Get();
					int liquid2 = WeightedRandom.Get();

					ushort? tileType = liquid1 switch
					{
						LiquidID.Water when liquid2 is LiquidID.Lava => TileID.Obsidian,
						LiquidID.Lava when liquid2 is LiquidID.Water => TileID.Obsidian,

						LiquidID.Water when liquid2 is LiquidID.Honey => TileID.HoneyBlock,
						LiquidID.Honey when liquid2 is LiquidID.Water => TileID.HoneyBlock,

						LiquidID.Lava when liquid2 is LiquidID.Honey => TileID.CrispyHoneyBlock,
						LiquidID.Honey when liquid2 is LiquidID.Lava => TileID.CrispyHoneyBlock,

						_ => null
					};

					if (tileType is { } type)
						TileTypeToPlace = type;
					else
						tile.PlaceLiquid(liquid1, config.LiquidAmount);

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(config.ReplaceType), "How the hell did this happen?");
			}
		}

		public static void PlaceTile(int i, int j)
		{
			LiquidConfig config = LiquidConfig.Instance;

			if (WorldGen.gen || !config.Active)
				return;

			if (TileTypeToPlace is not { } type)
				return;
			TileTypeToPlace = null;

			if (type is TileID.CrispyHoneyBlock && (config.BEEEEEES || WorldGen.notTheBees))
				SpawnBees(i, j);
			else
				WorldGen.PlaceTile(i, j, type);
		}

		// Yoinked from vanilla
		private static void SpawnBees(int i, int j)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || Main.rand.NextBool(2))
				return;

			int count = 1;
			if (Main.rand.NextBool(3))
				count = 2;

			for (int _ = 0; _ < count; _++)
			{
				int npcType = Main.rand.Next(210, 212);
				NPC npc = Main.npc[NPC.NewNPC(i * 16 + 8, j * 16 + 15, npcType, 1)];
				npc.velocity.X = Main.rand.Next(-200, 201) * 0.002f;
				npc.velocity.Y = Main.rand.Next(-200, 201) * 0.002f;
				npc.netUpdate = true;
			}
		}

		private static void SetupRandom(LiquidConfig config)
		{
			WeightedRandom.Clear();
			if (config.Water)
				WeightedRandom.Add(LiquidID.Water, config.WaterWeight);
			if (config.Lava)
				WeightedRandom.Add(LiquidID.Lava, config.LavaWeight);
			if (config.Honey)
				WeightedRandom.Add(LiquidID.Honey, config.HoneyWeight);
		}
	}
}
