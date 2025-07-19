// BuffManager.cs - PROPER ALPHA BLENDING & POSITION FIX
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace Spawn_Monsters.BuffSystem
{
    public static class BuffManager
    {
        private static BuffData currentBuff = null;
        private static IModHelper modHelper;
        private static IMonitor monitor;
        private static float pulseTimer = 0f;
        private static readonly float PULSE_SPEED = 2000f; // 2 seconds per pulse cycle

        public static void Initialize(IModHelper helper, IMonitor monitorInstance)
        {
            modHelper = helper;
            monitor = monitorInstance;
            
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnPlayerWarped;
        }

        public static void ApplyBuff(BuffType type, string customName, int duration, float value)
        {
            // Clear existing buff if any
            if (currentBuff != null)
            {
                ShowBuffNotification($"{GetBuffColorName(currentBuff.Type)} buff from {currentBuff.CustomName} replaced");
            }

            // Apply new buff
            currentBuff = new BuffData
            {
                Type = type,
                CustomName = customName,
                Duration = duration,
                Value = value,
                StartTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds
            };

            if (type == BuffType.Shield)
            {
                int healthToAdd = (int)(value / 3); // 2 health per percentage point
                Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + healthToAdd);
            }

            ShowBuffNotification($"{GetBuffColorName(type)} buff from {customName}");
            monitor.Log($"Applied {type} buff: {value}% for {duration}ms from {customName}", LogLevel.Debug);
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || currentBuff == null)
                return;

            // Check if buff expired
            double currentTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            if (currentTime - currentBuff.StartTime >= currentBuff.Duration)
            {
                currentBuff = null;
                monitor.Log("Buff expired", LogLevel.Debug);
            }

            // Update pulse timer
            pulseTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if (pulseTimer >= PULSE_SPEED)
                pulseTimer = 0f;
        }

        private static void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            // Keep buff when changing locations
        }

        private static void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || currentBuff == null)
                return;

            DrawBuffAura(e.SpriteBatch);
        }

        private static void DrawBuffAura(SpriteBatch spriteBatch)
        {
            try
            {
                var player = Game1.player;
                if (player == null) return;

                // Get player screen position
                var playerPos = Game1.GlobalToLocal(Game1.viewport, player.Position);
                
                // FIXED: Pulse range 100-120px instead of 110-140px
                float pulseProgress = pulseTimer / PULSE_SPEED;
                float pulseSize = 100f + (float)(Math.Sin(pulseProgress * Math.PI * 2) * 10f); // 100-120px range
                
                // Determine base color based on buff type
                Color baseColor = currentBuff.Type == BuffType.Shield 
                    ? Color.CornflowerBlue 
                    : Color.Gold;

                // FIXED: Simple offset positioning - 30px up from current
                Vector2 center = new Vector2(
                    playerPos.X + 30f, // Center horizontally
                    playerPos.Y - 30f // 30px offset up
                );

                DrawCircleAura(spriteBatch, center, pulseSize, baseColor, currentBuff.Type);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error drawing buff aura: {ex.Message}", LogLevel.Error);
            }
        }

        private static void DrawCircleAura(SpriteBatch spriteBatch, Vector2 center, float radius, Color baseColor, BuffType type)
        {
            // Draw bubble effect filled circle first (background with PROPER alpha blending)
            DrawBubbleCircle(spriteBatch, center, radius * 0.8f, baseColor);
            
            // Draw the dotted circle border
            int segments = 32;
            float angleStep = (float)(2 * Math.PI / segments);
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                float x = center.X + (float)(Math.Cos(angle) * radius);
                float y = center.Y + (float)(Math.Sin(angle) * radius);
                
                // Draw border dots with proper alpha
                Rectangle rect = new Rectangle((int)x - 3, (int)y - 3, 6, 6);
                Color dotColor = new Color(baseColor, 204); // 80% opacity (204/255)
                spriteBatch.Draw(Game1.fadeToBlackRect, rect, dotColor);
            }
        }

        private static void DrawBubbleCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color baseColor)
        {
            // PROPER alpha blending for transparent center
            int radiusInt = (int)radius;
            
            for (int x = -radiusInt; x <= radiusInt; x += 1)
            {
                for (int y = -radiusInt; y <= radiusInt; y += 1)
                {
                    float distance = (float)Math.Sqrt(x * x + y * y);
                    
                    // Check if point is inside circle
                    if (distance <= radius)
                    {
                        // Calculate normalized distance (0.0 at center, 1.0 at edge)
                        float normalizedDistance = distance / radius;
                        
                        // FIXED: Proper alpha blending - start from 0 (fully transparent)
                        // Center: 0% (transparent), Edge: 60% (visible)
                        float opacity = MathHelper.Lerp(0.0f, 0.6f, normalizedDistance * normalizedDistance);
                        
                        // Skip drawing if opacity is too low (saves performance and ensures true transparency)
                        if (opacity < 0.05f) continue;
                        
                        // PROPER alpha blending using Color constructor with alpha
                        Color pixelColor = Color.Lerp(Color.Transparent, baseColor, opacity);
                        
                        // Single pixel for smooth gradient
                        Rectangle pixel = new Rectangle((int)(center.X + x), (int)(center.Y + y), 1, 1);
                        spriteBatch.Draw(Game1.fadeToBlackRect, pixel, pixelColor);
                    }
                }
            }
        }

        private static void ShowBuffNotification(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
        }

        private static string GetBuffColorName(BuffType type)
        {
            return type == BuffType.Shield ? "Blue" : "Gold";
        }

        public static BuffData GetCurrentBuff()
        {
            return currentBuff;
        }

        public static bool HasActiveBuff()
        {
            return currentBuff != null;
        }

        public static float GetBuffMultiplier()
        {
            return currentBuff?.Value / 100f ?? 1f;
        }

        public static void ClearBuff()
        {
            currentBuff = null;
        }

        // Quick shield application for hotkey
        public static void ApplyQuickShield()
        {
            ApplyBuff(BuffType.Shield, "Player", 5000, 50f); // 5 seconds, 50% shield
        }
    }

    public enum BuffType
    {
        Shield,
        Damage
    }

    public class BuffData
    {
        public BuffType Type { get; set; }
        public string CustomName { get; set; }
        public int Duration { get; set; }
        public float Value { get; set; }
        public double StartTime { get; set; }
    }
}