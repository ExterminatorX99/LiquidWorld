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
		public static ushort?             TileTypeToPlace;

		public override void Load()
		{
			WeightedRandom = new WeightedRandom<int>(Main.rand);

			ILWorldGen.KillTile += LiquidEdit;
			ILWorldGen.KillTile += AllowPlace;
		}

		public override void Unload()
		{
			WeightedRandom  = null!;
			TileTypeToPlace = null;

			ILWorldGen.KillTile -= LiquidEdit;
			ILWorldGen.KillTile -= AllowPlace;
		}

		private void LiquidEdit(ILContext il)
		{
			ILCursor c = new(il);

			Func<Instruction, bool>[] branchInstructions =
			{
				// if (tile.type == 58 && j > Main.UnderworldLayer)
				i => i.MatchLdloca(0),               // IL_0af8: ldloca.s 0
				i => i.MatchCall<Tile>("get_type"),  // IL_0afa: call instance uint16& Terraria.Tile::get_type()
				i => i.MatchLdindU2(),               // IL_0aff: ldind.u2
				i => i.MatchLdcI4(TileID.Hellstone), // IL_0b00: ldc.i4.s 58
				i => i.MatchBneUn(out _),            // IL_0b02: bne.un.s IL_0b23

				i => i.MatchLdarg(1),                          // IL_0b04: ldarg.1
				i => i.MatchCall<Main>("get_UnderworldLayer"), // IL_0b05: call  int32 Terraria.Main::get_UnderworldLayer()
				i => i.MatchBle(out _),                        // IL_0b0a: ble.s IL_0b23
				// tile.lava(lava: true);
				i => i.MatchLdloca(0),          // IL_0b0c: ldloca.s 0
				i => i.MatchLdcI4(1),           // IL_0b0e: ldc.i4.1
				i => i.MatchCall<Tile>("lava"), // IL_0b0f: call instance void Terraria.Tile::lava(bool)
				// tile.liquid = 128;
				i => i.MatchLdloca(0),                // IL_0b14: ldloca.s 0
				i => i.MatchCall<Tile>("get_liquid"), // IL_0b16: call instance uint8& Terraria.Tile::get_liquid()
				i => i.MatchLdcI4(128),               // IL_0b1b: ldc.i4 128
				i => i.MatchStindI1(),                // IL_0b20: stind.i1
				// else if (tile.type == 419)
				i => i.MatchBr(out _), // IL_0b21: br.s IL_0b55
			};

			if (!c.TryGotoNext(MoveType.Before, branchInstructions))
			{
				Logger.Fatal("Failed to find Hellstone 'branch` instructions");
				return;
			}

			c.Emit(OpCodes.Ldloc_0);
			c.EmitDelegate(LiquidStuff);
		}

		private void AllowPlace(ILContext il)
		{
			ILCursor c = new(il);

			Func<Instruction, bool>[] breakInstructions =
			{
				// tile.type = 0;
				i => i.MatchLdloca(0),              // IL_0b55: ldloca.s 0
				i => i.MatchCall<Tile>("get_type"), // IL_0b57: call instance uint16& Terraria.Tile::get_type()
				i => i.MatchLdcI4(0),               // IL_0b5c: ldc.i4.0
				i => i.MatchStindI2(),              // IL_0b5d: stind.i2
				// tile.inActive(inActive: false);
				i => i.MatchLdloca(0),              // IL_0b5e: ldloca.s 0
				i => i.MatchLdcI4(0),               // IL_0b60: ldc.i4.0
				i => i.MatchCall<Tile>("inActive"), // IL_0b61: call instance void Terraria.Tile::inActive(bool)
			};

			if (!c.TryGotoNext(MoveType.After, breakInstructions))
			{
				Logger.Error("Failed to find KillTile 'break` instructions");
				return;
			}

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(PlaceTile);
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

						_ => null,
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
				NPC npc     = Main.npc[NPC.NewNPC(i * 16 + 8, j * 16 + 15, npcType, 1)];
				npc.velocity.X = Main.rand.Next(-200, 201) * 0.002f;
				npc.velocity.Y = Main.rand.Next(-200, 201) * 0.002f;
				npc.netUpdate  = true;
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
