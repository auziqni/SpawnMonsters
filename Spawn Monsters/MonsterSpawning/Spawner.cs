// Spawner.cs
using Microsoft.Xna.Framework;
using Spawn_Monsters.Monsters;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spawn_Monsters.MonsterSpawning
{
    class Spawner
    {

        private IMonitor Monitor;
        private static Spawner instance;
        private readonly List<Monster> spawnedMonsters;
        private readonly Random random;

        public Spawner() {
            instance = this;
            random = new Random();
            spawnedMonsters = new List<Monster>();
        }

        public static Spawner GetInstance() {
            if (instance == null) {
                instance = new Spawner();
            }
            return instance;
        }

        public void RegisterMonitor(IMonitor monitor) {
            Monitor = monitor;
        }

        public static bool IsOkToPlace(MonsterData.Monster monster, Vector2 tile) {
            if (monster == MonsterData.Monster.Duggy || monster == MonsterData.Monster.MagmaDuggy) {
                if (Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y] != null) {
                    if (Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y].TileIndexProperties.ContainsKey("Diggable")) {
                        return true;
                    } else if (!Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y].TileIndexProperties.ContainsKey("Diggable") && Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y].TileIndex == 0) {
                        return true;
                    }
                }
                return false;
            }
            // 🆕 ADD: Special placement validation for new golems
            else if (monster == MonsterData.Monster.RockGolem || monster == MonsterData.Monster.IridiumGolem) {
                // Rock golems prefer solid ground - avoid water tiles
                if (Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y] != null) {
                    int tileIndex = Game1.currentLocation.map.GetLayer("Back").Tiles[(int)tile.X, (int)tile.Y].TileIndex;
                    // Avoid water tiles (common water tile indices)
                    if (tileIndex >= 76 && tileIndex <= 79) return false;
                }
            }
            return true;
        }

        public bool SpawnMonster(MonsterData.Monster monster, Vector2 location) {
            //spawn monster
            if (IsOkToPlace(monster, location)) {
                MonsterData monsterData = MonsterData.GetMonsterData(monster);
                

                IEnumerable<object> args = new object[] { location };
                if(monsterData.SecondConstructorArg != null) {
                    args = args.Append( monsterData.SecondConstructorArg);
                }

                if (monster == MonsterData.Monster.BlackSlime) {
                    args = args.Append(new Color(40 + random.Next(10), 40 + random.Next(10), 40 + random.Next(10)));
                }

                Monster m = (Monster) Activator.CreateInstance(monsterData.Type, args.ToArray());
                m.currentLocation = Game1.currentLocation;

                // 🔧 ENHANCED: Existing special handling
                if (monster == MonsterData.Monster.GraySlime) {
                    int num = Game1.random.Next(120, 200);
                    (m as GreenSlime).color.Value = new Color(num, num, num);
                    while (Game1.random.NextDouble() < 0.33) {
                        m.objectsToDrop.Add("(O)380");
                    }
                    m.Speed = 1;
                } else if (monster == MonsterData.Monster.Duggy || monster == MonsterData.Monster.MagmaDuggy || monster == MonsterData.Monster.WildernessGolem) {
                    m.setTileLocation(location); //For Tile-Locked Monsters like Duggy
                } else if (monster == MonsterData.Monster.StickBug) {
                    (m as RockCrab).makeStickBug();
                } else if (monster == MonsterData.Monster.TigerSlime) {
                    (m as GreenSlime).makeTigerSlime();
                } else if (monster == MonsterData.Monster.PrismaticSlime) {
                    (m as GreenSlime).makePrismatic();
                }
                // 🆕 NEW: Special handling for new Big Slime variants
                else if (monster == MonsterData.Monster.BigBlueSlime) {
                    // BigSlime doesn't have color property, use constructor args instead
                    m.Health = m.MaxHealth = 280; // Slightly stronger than regular big slime
                }
                else if (monster == MonsterData.Monster.BigRedSlime) {
                    m.Health = m.MaxHealth = 320; // Red slimes are typically stronger
                    m.DamageToFarmer = 12; // Increased damage
                }
                else if (monster == MonsterData.Monster.BigPurpleSlime) {
                    m.Health = m.MaxHealth = 360; // Purple slimes are high-tier
                    m.DamageToFarmer = 15; // High damage
                    // Purple slimes sometimes drop rare items
                    if (random.NextDouble() < 0.1) {
                        m.objectsToDrop.Add("(O)768"); // Solar Essence
                    }
                }
                // 🆕 NEW: Special handling for Rock Golem
                else if (monster == MonsterData.Monster.RockGolem) {
                    m.Health = m.MaxHealth = 200; // Sturdy but not overpowered
                    m.DamageToFarmer = 10;
                    // Rock golems drop stone and ore
                    m.objectsToDrop.Add("(O)390"); // Stone
                    if (random.NextDouble() < 0.3) {
                        m.objectsToDrop.Add("(O)378"); // Copper Ore
                    }
                }
                // 🆕 NEW: Special handling for Iridium Golem  
                else if (monster == MonsterData.Monster.IridiumGolem) {
                    m.Health = m.MaxHealth = 500; // Very tough
                    m.DamageToFarmer = 20; // High damage
                    // Iridium golems drop valuable items
                    m.objectsToDrop.Add("(O)337"); // Iridium Ore
                    if (random.NextDouble() < 0.2) {
                        m.objectsToDrop.Add("(O)386"); // Iridium Bar
                    }
                }
                // 🆕 NEW: Special handling for Shadow Sniper
                else if (monster == MonsterData.Monster.ShadowSniper) {
                    m.Health = m.MaxHealth = 150; // Glass cannon
                    m.DamageToFarmer = 18; // High ranged damage
                    m.Speed = 3; // Fast movement
                    // Shadow creatures drop void essence
                    if (random.NextDouble() < 0.4) {
                        m.objectsToDrop.Add("(O)769"); // Void Essence
                    }
                }

                Game1.currentLocation.addCharacter(m);
                spawnedMonsters.Add(m);
                Monitor.Log("Spawned " + monster + " at " + location, LogLevel.Debug);

                return true;
            } else {
                return false;
            }
        }

        public void SpawnMonster(MonsterData.Monster monster, Vector2 location, int amount) {
            for (int i = 0; i < amount; i++) {
                if (!SpawnMonster(monster, location)) {
                    Monitor.Log("You may not place this monster there.", LogLevel.Info);
                    break;
                }
            }
            Monitor.Log($"Spawned {amount} {MonsterData.GetMonsterData(monster).Displayname} at {location}", LogLevel.Info);
        }

        public void KillEverything() {
            List<Monster> toKill = new List<Monster>();

            //Determine Monsters to kill
            foreach (Monster m in spawnedMonsters) {
                //GreenSlime, BigSlime, Bug, Grub, Monster, MetalHead
                if (m.GetType() != typeof(GreenSlime) && m.GetType() != typeof(BigSlime) && m.GetType() != typeof(Bug) && m.GetType() != typeof(Grub) && m.GetType() != typeof(MetalHead)) {
                    toKill.Add(m);
                }
            }

            Monitor.Log("Removing " + toKill.Count + " Monsters", LogLevel.Trace);
            foreach (Monster m in toKill) {
                Monitor.Log("Removed " + m.getTextureName(), LogLevel.Trace);
                m.currentLocation.characters.Remove(m);
                m.Removed();
                spawnedMonsters.Remove(m);
            }
            if (toKill.Count > 0) {
                Game1.addHUDMessage(new HUDMessage("Removed " + toKill.Count + " Monsters to prevent saving-errors.", 2));
            }
        }
    }
}