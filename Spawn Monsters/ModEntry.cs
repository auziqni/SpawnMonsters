using Microsoft.Xna.Framework;
using Spawn_Monsters.Monsters;
using Spawn_Monsters.MonsterSpawning;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace Spawn_Monsters
{
    /// <summary>Represents the mod entry point.</summary>
    public class ModEntry : Mod
    {

        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        public ModConfig config;

        /*********
        ** Public methods
        *********/
        public override void Entry(IModHelper helper) {
            helper.ConsoleCommands.Add("monster_spawn", "Spawns a Monster.", SpawnEntity);
            helper.ConsoleCommands.Add("monster_list", "Shows a lists of all monsters available to spawn.", MonsterList);
            helper.ConsoleCommands.Add("monster_menu", "Shows a menu for spawning monsters", MonsterMenu);
            helper.ConsoleCommands.Add("farmer_position", "Prints the Farmer's current position", FarmerPosition);
            helper.ConsoleCommands.Add("remove_prismatic_jelly", "Removes all Prismatic Jelly from your inventory", DeleteJelly);

            config = helper.ReadConfig<ModConfig>();

            Spawner.GetInstance().RegisterMonitor(Monitor);

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.Saving += OnSaveCreating;
        }

        public void OnSaveCreating(object sender, SavingEventArgs e) {
            Spawner.GetInstance().KillEverything();
        }

        /*********
        ** Input Methods
        *********/
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!Context.IsPlayerFree) {
                return;
            }

            // 🆕 NEW: Custom hotkey for Stone Golem spawning
            if (e.Button == SButton.G) {
                SpawnStoneGolemAtOffset();
                return;
            }

            if (e.Button == config.MenuKey) {
                Game1.activeClickableMenu = new MonsterMenu.MonsterMenu(Helper);
            }
        }

        /*********
        ** 🆕 NEW: Custom Spawn Method
        *********/
        private void SpawnStoneGolemAtOffset() {
            if (!Context.IsWorldReady) {
                Monitor.Log("Load a save first!", LogLevel.Info);
                return;
            }

            try {
                // Get farmer position (using correct property)
                Vector2 farmerPos = Game1.player.Tile;  // Changed from getTileLocation()
                
                // Calculate spawn position (+5X, +5Y)
                Vector2 spawnPos = new Vector2(farmerPos.X + 5, farmerPos.Y + 5);
                
                // Convert to pixel coordinates for non-tile monsters
                Vector2 pixelPos = new Vector2(spawnPos.X * Game1.tileSize, spawnPos.Y * Game1.tileSize);
                
                // Spawn Stone Golem
                bool success = Spawner.GetInstance().SpawnMonster(MonsterData.Monster.StoneGolem, pixelPos);
                
                if (success) {
                    Monitor.Log($"Stone Golem spawned at ({spawnPos.X}, {spawnPos.Y})", LogLevel.Info);
                    Game1.addHUDMessage(new HUDMessage($"Stone Golem spawned!", HUDMessage.newQuest_type));
                } else {
                    Monitor.Log("Could not spawn Stone Golem at that location", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage("Cannot spawn there!", HUDMessage.error_type));
                }
            }
            catch (Exception ex) {
                Monitor.Log($"Error spawning Stone Golem: {ex.Message}", LogLevel.Error);
            }
        }

        /*********
        ** Command Methods
        *********/
        public void SpawnEntity(string command, string[] args) {


            if (args.Length == 0 || args[0].Equals("help")) {
                Monitor.Log($"Usage: monster_spawn \"Monster Name\" [posX] [posY] [amount]" +
                    $"\n\nUses Farmer's coordinates if none or '{config.FarmerPositionCharacter}' was given." +
                    $"\n\nExample: monster_spawn \"Green Slime\" 32 23 4" +
                    $"\nspawns four Green Slimes at coordinates 32|23" +
                    $"\nuse monster_list for a list of available monster names." +
                    $"\n\n🆕 NEW: Press 'G' to quickly spawn Stone Golem at +5X +5Y from your position!", LogLevel.Info);
                return;
            }

            //We need a world to spawn monsters in, duh
            if (Context.IsWorldReady) {

                if (args.Length == 0) {
                    Monitor.Log("You need to provide at least a Monster name!", LogLevel.Info);
                    return;
                }
                MonsterData.Monster m = MonsterData.ForName(args[0]);
                Vector2 location = Game1.player.Tile;

                if (m.Equals((MonsterData.Monster)(-1))) {
                    Monitor.Log($"There is no Monster with the name {args[0]}", LogLevel.Info);
                    return;
                }


                int amount = 1;

                try {
                    //Determine X tile
                    if (args.Length >= 2) {
                        location.X = int.Parse(args[1].Replace(config.FarmerPositionCharacter.ToString(), location.X.ToString()));
                    }

                    //Determine Y tile
                    if (args.Length >= 3) {
                        location.Y = int.Parse(args[2].Replace(config.FarmerPositionCharacter.ToString(), location.Y.ToString()));
                    }

                    if (args.Length >= 4) {
                        amount = int.Parse(args[3]);
                    }

                } catch (Exception e) {
                    Console.Error.WriteLine(e.Message);
                    Monitor.Log("Invalid Arguments! Type \"monster_spawn help\" for usage help.", LogLevel.Info);
                    return;
                }

                if (m != MonsterData.Monster.Duggy && m != MonsterData.Monster.WildernessGolem) {
                    location.X *= Game1.tileSize;
                    location.Y *= Game1.tileSize;
                }

                Spawner.GetInstance().SpawnMonster(m, location, amount);

            } else { Monitor.Log("Load a save first!"); }
        }

        public void MonsterList(string command, string[] args)
        {
            var monsterList = new MonsterList();

            Monitor.Log(
                "Monsters available to spawn:\n\n" +
                monsterList.ToString() +
                "\n\nUse these names with 'monster_spawn'.\n" +
                "Keep in mind that some monsters don't work properly outside of the farm and the mines!\n" +
                "\n🆕 HOTKEYS:\n" +
                "Press 'P' - Open monster menu\n" +
                "Press 'G' - Quick spawn Stone Golem (+5X +5Y from player)\n"
            , LogLevel.Info);
        }

        public void MonsterMenu(string command, string[] args) {
            if (Context.IsWorldReady) {
                Game1.activeClickableMenu = new MonsterMenu.MonsterMenu(Helper);
            } else {
                Monitor.Log("Load a save first!", LogLevel.Info);
            }
        }

        public void FarmerPosition(string command, string[] args) {
            Monitor.Log("The Farmer's coordinates are: " + Game1.player.Tile, LogLevel.Info);
        }

        public void DeleteJelly(string command, string[] args) {
            int amount = 0;
            foreach(Item item in Game1.player.Items) {
                // Prismatic Jelly is of category 0 (Object) and has the id 876
                if (item != null && item.Category == 0 && item.ParentSheetIndex == 876) {
                    Game1.player.removeItemFromInventory(item);
                    amount += item.Stack;
                }
            }
            Monitor.Log($"Removed {amount} Prismatic {(amount == 1 ? "Jelly" : "Jellies")} from your inventory.", LogLevel.Info);
        }
    }
}