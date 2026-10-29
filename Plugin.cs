using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Utils;

namespace FeelingCozy
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class FeelingCozyPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "biznessman.feelingcozy";
        public const string PluginName = "FeelingCozy";
        public const string PluginVersion = "1.1.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> Mode;
        internal static ConfigEntry<int> MinComfort;

        private void Awake()
        {
            // IsAdminOnly = true: Jotunn syncs these from the server. The server's values win.
            var synced = new ConfigurationManagerAttributes { IsAdminOnly = true };

            Enabled = Config.Bind("General", "Enabled", true,
                new ConfigDescription("Turn the mod on or off.", null, synced));

            Mode = Config.Bind("General", "DegradationMode", 0,
                new ConfigDescription(
                    "How food behaves while you rest in your base:\n" +
                    "0 = Frozen: food timer and buffs stop completely.\n" +
                    "1 = Slow: food lasts twice as long.\n" +
                    "2 = Scaled: more comfort = slower decay (1/2 rate at 10 comfort, 1/3 at 15, 1/4 at 20+). Ignores MinimumComfort.",
                    new AcceptableValueRange<int>(0, 2), synced));

            MinComfort = Config.Bind("General", "MinimumComfort", 10,
                new ConfigDescription(
                    "Comfort level needed for modes 0 and 1 to apply. Not used in mode 2.",
                    new AcceptableValueRange<int>(5, 25), synced));

            new Harmony(PluginGuid).PatchAll();
        }

        /// <summary>
        /// Fraction of normal food decay that still happens (0 = frozen, 1 = vanilla).
        /// </summary>
        internal static float GetDecayRate(int comfort)
        {
            switch (Mode.Value)
            {
                case 0:
                    return comfort >= MinComfort.Value ? 0f : 1f;
                case 1:
                    return comfort >= MinComfort.Value ? 0.5f : 1f;
                case 2:
                    if (comfort >= 20) return 1f / 4f;
                    if (comfort >= 15) return 1f / 3f;
                    if (comfort >= 10) return 1f / 2f;
                    return 1f;
                default:
                    return 1f;
            }
        }
    }

    internal class FoodState
    {
        public Player.Food Food;
        public float Time;
    }

    internal class FreezeState
    {
        public float Rate;
        public List<FoodState> Foods;
    }

    [HarmonyPatch(typeof(Player), "UpdateFood")]
    internal static class UpdateFoodPatch
    {
        private static readonly AccessTools.FieldRef<Player, List<Player.Food>> FoodsRef =
            AccessTools.FieldRefAccess<Player, List<Player.Food>>("m_foods");

        private static float GetRate(Player player)
        {
            if (!FeelingCozyPlugin.Enabled.Value) return 1f;
            if (player != Player.m_localPlayer) return 1f; // food ticks run on the owning client
            if (!EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.PlayerBase, 0f)) return 1f;
            return FeelingCozyPlugin.GetDecayRate(player.GetComfortLevel());
        }

        // Snapshot each food's remaining time before the vanilla tick.
        private static void Prefix(Player __instance, out FreezeState __state)
        {
            __state = null;
            float rate = GetRate(__instance);
            if (rate >= 1f) return;

            var foods = FoodsRef(__instance);
            __state = new FreezeState { Rate = rate, Foods = new List<FoodState>(foods.Count) };
            foreach (var food in foods)
                __state.Foods.Add(new FoodState { Food = food, Time = food.m_time });
        }

        // Scale whatever the vanilla tick took off the timer. Only m_time is touched, so regen per tick
        // and the hp/stamina buffs are left exactly as vanilla computed them.
        private static void Postfix(Player __instance, FreezeState __state)
        {
            if (__state == null) return;

            var foods = FoodsRef(__instance);
            foreach (var s in __state.Foods)
            {
                bool stillThere = foods.Contains(s.Food);
                // If vanilla removed it this tick, assume it lost one second.
                float lost = stillThere ? s.Time - s.Food.m_time : 1f;
                float newTime = s.Time - lost * __state.Rate;

                if (newTime <= 0f) continue; // genuinely expired even at the reduced rate

                s.Food.m_time = newTime;
                if (!stillThere) foods.Add(s.Food);
            }
        }
    }
}
