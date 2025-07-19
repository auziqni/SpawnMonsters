// CustomMonsterManager.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Spawn_Monsters.Api
{
    public static class CustomMonsterManager
    {
        private static Dictionary<Monster, string> customNames = new Dictionary<Monster, string>();
        private static Dictionary<Vector2, string> pendingNames = new Dictionary<Vector2, string>();
        private static IModHelper modHelper;
        private static IMonitor monitor;

        public static void Initialize(IModHelper helper, IMonitor monitorInstance)
        {
            modHelper = helper;
            monitor = monitorInstance;
            
            // Subscribe to events for name display
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnPlayerWarped;
        }

        public static void RegisterCustomName(Vector2 spawnPosition, string customName)
        {
            // Store pending name with spawn position
            pendingNames[spawnPosition] = customName;
            monitor.Log($"📝 Registered custom name '{customName}' for spawn at {spawnPosition}", LogLevel.Debug);
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || pendingNames.Count == 0)
                return;

            // Check for new monsters near pending spawn positions
            var location = Game1.currentLocation;
            if (location == null) return;

            var toRemove = new List<Vector2>();

            foreach (var pending in pendingNames)
            {
                var spawnPos = pending.Key;
                var customName = pending.Value;

                // Look for monsters near the spawn position
                foreach (var character in location.characters)
                {
                    if (character is Monster monster && !customNames.ContainsKey(monster))
                    {
                        var monsterTile = monster.Tile;
                        var distance = Vector2.Distance(spawnPos, monsterTile);

                        // If monster is within 2 tiles of spawn position, consider it a match
                        if (distance <= 2.0f)
                        {
                            customNames[monster] = customName;
                            toRemove.Add(spawnPos);
                            monitor.Log($"✅ Assigned custom name '{customName}' to {monster.Name} at {monsterTile}", LogLevel.Debug);
                            break;
                        }
                    }
                }
            }

            // Remove processed pending names
            foreach (var pos in toRemove)
            {
                pendingNames.Remove(pos);
            }
        }

        private static void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            // Clear custom names when player changes locations
            customNames.Clear();
            pendingNames.Clear();
        }

        private static void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || customNames.Count == 0)
                return;

            var spriteBatch = e.SpriteBatch;
            var location = Game1.currentLocation;

            // Clean up dead monsters
            var toRemove = new List<Monster>();
            foreach (var kvp in customNames)
            {
                var monster = kvp.Key;
                if (monster.Health <= 0 || !location.characters.Contains(monster))
                {
                    toRemove.Add(monster);
                }
            }

            foreach (var monster in toRemove)
            {
                customNames.Remove(monster);
            }

            // Draw custom names above monsters
            foreach (var kvp in customNames)
            {
                var monster = kvp.Key;
                var customName = kvp.Value;

                if (monster.Health > 0 && location.characters.Contains(monster))
                {
                    DrawMonsterName(spriteBatch, monster, customName);
                }
            }
        }

        private static void DrawMonsterName(SpriteBatch spriteBatch, Monster monster, string name)
        {
            try
            {
                // Get monster position in world coordinates
                var monsterPosition = monster.Position;
                
                // Convert to screen coordinates
                var screenPos = Game1.GlobalToLocal(Game1.viewport, monsterPosition);
                
                // Adjust position to be above the monster
                var namePosition = new Vector2(
                    screenPos.X + (monster.Sprite.SpriteWidth * Game1.pixelZoom / 2), // Center horizontally
                    screenPos.Y - 40 // Above the monster
                );

                // Get font and measure text
                var font = Game1.smallFont;
                var textSize = font.MeasureString(name);

                // Center the text horizontally
                namePosition.X -= textSize.X / 2;

                // Draw background rectangle for better readability
                var backgroundRect = new Rectangle(
                    (int)(namePosition.X - 4),
                    (int)(namePosition.Y - 2),
                    (int)(textSize.X + 8),
                    (int)(textSize.Y + 4)
                );

                // Draw semi-transparent black background
                spriteBatch.Draw(Game1.fadeToBlackRect, backgroundRect, Color.Black * 0.6f);

                // Draw the custom name in white
                spriteBatch.DrawString(font, name, namePosition, Color.White);
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Error drawing monster name: {ex.Message}", LogLevel.Error);
            }
        }

        public static void ClearAllNames()
        {
            customNames.Clear();
            pendingNames.Clear();
            monitor.Log("🧹 Cleared all custom monster names", LogLevel.Debug);
        }

        public static int GetActiveNameCount()
        {
            return customNames.Count;
        }

        public static bool HasCustomName(Monster monster)
        {
            return customNames.ContainsKey(monster);
        }

        public static string GetCustomName(Monster monster)
        {
            return customNames.TryGetValue(monster, out string name) ? name : null;
        }
    }
}