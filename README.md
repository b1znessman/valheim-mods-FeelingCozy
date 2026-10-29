# FeelingCozy
Decreases the rate food loses effectiveness when at home

Slows or pauses food decay while you're inside your base (ward area) and comfortable enough.
Only the food timer (and the buffs tied to it) change. Health/stamina regen per tick is untouched.

## Config (all server-synced, server values win)
| Setting | Default | Notes |
|---|---|---|
| Enabled | true | Master switch |
| DegradationMode | 0 | 0 = frozen, 1 = half rate, 2 = scaled by comfort |
| MinimumComfort | 10 | 5-25. Used by modes 0 and 1 only |

Mode 2 rates: comfort 10-14 = 1/2, 15-19 = 1/3, 20+ = 1/4. Below 10 = normal decay.

## Build
1. Install BepInEx and Jotunn in your Valheim folder.
2. Set `VALHEIM_DIR` (env var, or edit the csproj) to the Valheim install path.
3. `dotnet build -c Release`
4. Copy `bin/Release/net48/FoodFreezer.dll` to `BepInEx/plugins/` on the client and on the dedicated server.

## Not verified
Written without access to the Valheim 1.0 assemblies, so these are untested:
- `Player.UpdateFood` (private, `float dt, bool forceUpdate`) still exists with that signature
- `Player.m_foods` field name and `Player.Food.m_time`
- `EffectArea.IsPointInsideArea(Vector3, EffectArea.Type, float)` signature
- Jotunn's `ConfigurationManagerAttributes` namespace (`Jotunn.Configs` vs `Jotunn.Utils`)
