// HttpApiServer.cs - MODIFIED FILE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using Spawn_Monsters.Monsters;
using Spawn_Monsters.MonsterSpawning;
using Spawn_Monsters.BuffSystem;

namespace Spawn_Monsters.Api
{
    public class HttpApiServer
    {
        private HttpListener httpListener;
        private readonly IMonitor monitor;
        private readonly int port;
        private bool isRunning;

        public HttpApiServer(IMonitor monitor, int port = 8080)
        {
            this.monitor = monitor;
            this.port = port;
        }

        public void Start()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{port}/");
                httpListener.Start();
                isRunning = true;

                monitor.Log($"🌐 HTTP API Server started on http://localhost:{port}/", LogLevel.Info);
                monitor.Log("📡 Available endpoints:", LogLevel.Info);
                monitor.Log("   POST /api/spawn - Spawn monsters with custom names", LogLevel.Info);
                monitor.Log("   POST /api/effects - Apply buff effects", LogLevel.Info);

                // Start listening for requests
                _ = Task.Run(HandleRequestsAsync);
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Failed to start HTTP server: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task HandleRequestsAsync()
        {
            while (isRunning && httpListener.IsListening)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                catch (Exception ex) when (isRunning)
                {
                    monitor.Log($"❌ Error handling request: {ex.Message}", LogLevel.Error);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Set CORS headers for browser compatibility
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // Handle preflight OPTIONS request
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                // Route requests
                var path = request.Url.AbsolutePath.ToLower();
                var method = request.HttpMethod.ToUpper();

                if (path == "/api/spawn" && method == "POST")
                {
                    await HandleSpawnRequest(request, response);
                }
                else if (path == "/api/effects" && method == "POST")
                {
                    await HandleEffectsRequest(request, response);
                }
                else if (path == "/" || path == "/api")
                {
                    await HandleInfoRequest(response);
                }
                else
                {
                    // 404 Not Found
                    await SendJsonResponse(response, new { error = "Endpoint not found" }, 404);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Error processing request: {ex.Message}", LogLevel.Error);
                await SendJsonResponse(response, new { error = "Internal server error" }, 500);
            }
        }

        private async Task HandleSpawnRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Parse JSON
                var spawnRequest = JsonConvert.DeserializeObject<SpawnRequest>(requestBody);

                // Validate request
                if (string.IsNullOrEmpty(spawnRequest?.MonsterName))
                {
                    await SendJsonResponse(response, new { error = "Monster Name is required" }, 400);
                    return;
                }

                // Apply defaults
                spawnRequest.Qty = spawnRequest.Qty <= 0 ? 1 : spawnRequest.Qty;
                spawnRequest.CustomName = string.IsNullOrEmpty(spawnRequest.CustomName) ? spawnRequest.MonsterName : spawnRequest.CustomName;

                // Check if game is ready
                if (!Context.IsWorldReady)
                {
                    await SendJsonResponse(response, new { error = "Game world is not ready" }, 400);
                    return;
                }

                // Spawn monsters
                var result = await SpawnMonstersAsync(spawnRequest);

                if (result.Success)
                {
                    monitor.Log($"✅ API Spawn: {result.SpawnedCount} x {spawnRequest.MonsterName} as '{spawnRequest.CustomName}'", LogLevel.Info);
                    await SendJsonResponse(response, new 
                    { 
                        success = true, 
                        message = $"Spawned {result.SpawnedCount} monsters",
                        spawned = result.SpawnedCount,
                        requested = spawnRequest.Qty,
                        monsterType = spawnRequest.MonsterName,
                        customName = spawnRequest.CustomName
                    });
                }
                else
                {
                    await SendJsonResponse(response, new { error = result.Error }, 400);
                }
            }
            catch (JsonException)
            {
                await SendJsonResponse(response, new { error = "Invalid JSON format" }, 400);
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Spawn request error: {ex.Message}", LogLevel.Error);
                await SendJsonResponse(response, new { error = "Failed to spawn monsters" }, 500);
            }
        }

        private async Task HandleEffectsRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Parse JSON
                var effectRequest = JsonConvert.DeserializeObject<EffectRequest>(requestBody);

                // Validate request
                if (string.IsNullOrEmpty(effectRequest?.Effect))
                {
                    await SendJsonResponse(response, new { error = "Effect type is required" }, 400);
                    return;
                }

                if (effectRequest.Effect != "shield" && effectRequest.Effect != "damage")
                {
                    await SendJsonResponse(response, new { error = "Effect must be 'shield' or 'damage'" }, 400);
                    return;
                }

                // Apply defaults
                effectRequest.CustomName = string.IsNullOrEmpty(effectRequest.CustomName) ? "API" : effectRequest.CustomName;
                effectRequest.Duration = effectRequest.Duration <= 0 ? 5000 : effectRequest.Duration; // Default 5 seconds
                effectRequest.Value = effectRequest.Value <= 0 ? 50 : effectRequest.Value; // Default 50%

                // Check if game is ready
                if (!Context.IsWorldReady)
                {
                    await SendJsonResponse(response, new { error = "Game world is not ready" }, 400);
                    return;
                }

                // Apply buff effect
                var buffType = effectRequest.Effect == "shield" ? BuffType.Shield : BuffType.Damage;
                BuffManager.ApplyBuff(buffType, effectRequest.CustomName, effectRequest.Duration, effectRequest.Value);

                monitor.Log($"✅ API Effect: {effectRequest.Effect} {effectRequest.Value}% for {effectRequest.Duration}ms from '{effectRequest.CustomName}'", LogLevel.Info);
                
                await SendJsonResponse(response, new 
                { 
                    success = true, 
                    message = $"Applied {effectRequest.Effect} buff",
                    effect = effectRequest.Effect,
                    customName = effectRequest.CustomName,
                    duration = effectRequest.Duration,
                    value = effectRequest.Value
                });
            }
            catch (JsonException)
            {
                await SendJsonResponse(response, new { error = "Invalid JSON format" }, 400);
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Effects request error: {ex.Message}", LogLevel.Error);
                await SendJsonResponse(response, new { error = "Failed to apply effect" }, 500);
            }
        }

        private async Task HandleInfoRequest(HttpListenerResponse response)
        {
            var info = new
            {
                service = "Stardew Valley Monster Spawner API",
                version = "1.0.0",
                status = "running",
                endpoints = new
                {
                    spawn = new
                    {
                        method = "POST",
                        url = "/api/spawn",
                        description = "Spawn monsters with custom names",
                        parameters = new
                        {
                            MonsterName = "string (required) - Name of monster to spawn",
                            Qty = "number (optional) - Quantity to spawn (default: 1)",
                            CustomName = "string (optional) - Custom display name (default: Monster Name)"
                        },
                        example = new
                        {
                            MonsterName = "Stone Golem",
                            Qty = 3,
                            CustomName = "Guardian"
                        }
                    },
                    effects = new
                    {
                        method = "POST",
                        url = "/api/effects",
                        description = "Apply buff effects to player",
                        parameters = new
                        {
                            effect = "string (required) - 'shield' or 'damage'",
                            CustomName = "string (optional) - Source identifier (default: 'API')",
                            duration = "number (optional) - Duration in milliseconds (default: 5000)",
                            value = "number (optional) - Percentage value (default: 50)"
                        },
                        example = new
                        {
                            effect = "damage",
                            CustomName = "Boss Buff",
                            duration = 3000,
                            value = 50
                        }
                    }
                }
            };

            await SendJsonResponse(response, info);
        }

        private async Task<SpawnResult> SpawnMonstersAsync(SpawnRequest request)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Find monster type
                    var monsterType = MonsterData.ForName(request.MonsterName);
                    if (monsterType == (MonsterData.Monster)(-1))
                    {
                        return new SpawnResult { Success = false, Error = $"Monster '{request.MonsterName}' not found" };
                    }

                    // Get player position
                    var playerPos = Game1.player.Tile;
                    var spawner = Spawner.GetInstance();
                    int spawnedCount = 0;
                    var random = new Random();

                    // Try to spawn requested quantity
                    for (int i = 0; i < request.Qty; i++)
                    {
                        // Generate random position within ±5 tiles
                        int offsetX = random.Next(-5, 6); // -5 to +5
                        int offsetY = random.Next(-5, 6); // -5 to +5
                        
                        var spawnTile = new Vector2(playerPos.X + offsetX, playerPos.Y + offsetY);
                        var spawnPixel = new Vector2(spawnTile.X * Game1.tileSize, spawnTile.Y * Game1.tileSize);

                        // Check if position is valid (collision detection)
                        if (IsValidSpawnPosition(spawnTile))
                        {
                            bool success = spawner.SpawnMonster(monsterType, spawnPixel);
                            if (success)
                            {
                                spawnedCount++;
                                
                                // Add custom name to monster (this will be handled in enhanced monster tracking)
                                CustomMonsterManager.RegisterCustomName(spawnTile, request.CustomName);
                            }
                        }
                    }

                    return new SpawnResult 
                    { 
                        Success = true, 
                        SpawnedCount = spawnedCount,
                        Error = spawnedCount < request.Qty ? $"Only spawned {spawnedCount}/{request.Qty} due to blocked positions" : null
                    };
                }
                catch (Exception ex)
                {
                    return new SpawnResult { Success = false, Error = ex.Message };
                }
            });
        }

        private bool IsValidSpawnPosition(Vector2 tile)
        {
            try
            {
                var location = Game1.currentLocation;
                if (location == null) return false;

                // Check if tile is within map bounds
                if (tile.X < 0 || tile.Y < 0 || tile.X >= location.map.Layers[0].LayerWidth || tile.Y >= location.map.Layers[0].LayerHeight)
                    return false;

                // Check if tile is blocked by objects or furniture
                if (location.getObjectAtTile((int)tile.X, (int)tile.Y) != null)
                    return false;

                // Check for terrain features that might block spawning
                if (location.terrainFeatures.ContainsKey(tile))
                {
                    var feature = location.terrainFeatures[tile];
                    // Allow spawning on grass, but not on trees or other blocking features
                    if (feature.isPassable() == false)
                        return false;
                }

                // Simple water tile check (basic water tile indices)
                var backLayer = location.map.GetLayer("Back");
                if (backLayer != null)
                {
                    var mapTile = backLayer.Tiles[(int)tile.X, (int)tile.Y];
                    if (mapTile != null)
                    {
                        // Common water tile indices - avoid spawning in water
                        int tileIndex = mapTile.TileIndex;
                        if (tileIndex >= 76 && tileIndex <= 79) // Basic water tiles
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false; // If any error, consider position invalid
            }
        }

        private async Task SendJsonResponse(HttpListenerResponse response, object data, int statusCode = 200)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var buffer = Encoding.UTF8.GetBytes(json);
                
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Error sending response: {ex.Message}", LogLevel.Error);
            }
        }

        public void Stop()
        {
            try
            {
                isRunning = false;
                httpListener?.Stop();
                httpListener?.Close();
                monitor.Log("🛑 HTTP API Server stopped", LogLevel.Info);
            }
            catch (Exception ex)
            {
                monitor.Log($"❌ Error stopping server: {ex.Message}", LogLevel.Error);
            }
        }
    }

    // Data models
    public class SpawnRequest
    {
        [JsonProperty("Monster Name")]
        public string MonsterName { get; set; }

        [JsonProperty("Qty")]
        public int Qty { get; set; } = 1;

        [JsonProperty("Custom Name")]
        public string CustomName { get; set; }
    }

    public class EffectRequest
    {
        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("Custom Name")]
        public string CustomName { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; } = 5000;

        [JsonProperty("value")]
        public float Value { get; set; } = 50;
    }

    public class SpawnResult
    {
        public bool Success { get; set; }
        public int SpawnedCount { get; set; }
        public string Error { get; set; }
    }
}