using BepInEx;
using KeepThatAwayFromMe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;
using System.Text.RegularExpressions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace CreatureSpawnCustomiser
{
    [BepInPlugin(CreatureSpawnCustomiserPlugin.PLUGIN_ID, CreatureSpawnCustomiserPlugin.PLUGIN_NAME, CreatureSpawnCustomiserPlugin.PLUGIN_VERSION)] // (GUID, mod name, mod version)
    [BepInProcess("RainWorld.exe")]
    public class CreatureSpawnCustomiserPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.rainworldgame.precipitator.creaturespawncustomiser";
        public const string PLUGIN_NAME = "Creature Spawn Customiser";
        public const string PLUGIN_VERSION = "0.1.0";

        public void Awake()
        {
            Logger.LogInfo("Creature Spawn Customiser is awake!");
            instance = this;
            On.RainWorld.OnModsInit += Init;
        }

        private static void Init(On.RainWorld.orig_OnModsInit orig, RainWorld rw)
        {
            orig(rw);
            if (init) return;
            init = true;
            CreatureSpawnCustomiserOI coi = new CreatureSpawnCustomiserOI();
            InitializeConfig(coi);
            MachineConnector.SetRegisteredOI("creaturespawncustomiser", coi);
            instance.Patch();
            instance.Logger.LogMessage("Creature Spawn Customiser initialised.");

            void InitializeConfig(CreatureSpawnCustomiserOI oi)
            {
                // Initialize Creature Types
                string[] allNames = ExtEnumBase.GetNames(typeof(CreatureTemplate.Type));

                List<string> okayNames = new List<string>();
                for (int i = 0; i < allNames.Length; i++)
                { if (PhobiaPlugin.IsValidCritType(allNames[i])) { okayNames.Add(allNames[i]); } }

                critTypeReplacements = new Configurable<CreatureTemplate.Type>[okayNames.Count];
                for (int j = 0; j < okayNames.Count; j++)
                {
                    critTypeReplacements[j] = oi.config.Bind(CreatureSpawnCustomiserOI.GenerateCritKey(PhobiaPlugin.allCritTypes[j]), PhobiaPlugin.allCritTypes[j]);
                }
            }
        }

        public static Dictionary<CreatureTemplate.Type, CreatureTemplate.Type> creatureMapper = new Dictionary<CreatureTemplate.Type, CreatureTemplate.Type>()
        {
            {CreatureTemplate.Type.GreenLizard, CreatureTemplate.Type.PinkLizard}
        };
        public static Configurable<CreatureTemplate.Type>[] critTypeReplacements = new Configurable<CreatureTemplate.Type>[0];
        private static bool init = false;
        public static CreatureSpawnCustomiserPlugin instance;


        public void Patch()
        {
            try
            {
                Logger.LogInfo("Hooking Creature Spawn Customiser");
                //On.WorldLoader.CreatureTypeFromString += CreatureTypeFromStringHook;
                On.StaticWorld.GetCreatureTemplate_Type += GetCreatureTemplate_TypeHook;
                On.SaveState.AbstractCreatureFromString += AbstractCreatureFromStringHook;
                Logger.LogInfo("Hooked Creature Spawn Customiser");
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to enable Creature Spawn Customiser.\n" + e.ToString());
            }
        }

        public CreatureTemplate GetCreatureTemplate_TypeHook(On.StaticWorld.orig_GetCreatureTemplate_Type orig, CreatureTemplate.Type type)
        {
            return orig.Invoke(MapCreature(type));
        }

        /*
        public CreatureTemplate.Type CreatureTypeFromStringHook(On.WorldLoader.orig_CreatureTypeFromString orig, string s)
        {
            Logger.LogDebug(string.Concat("Checking ", s));
            // Run the original code.
            CreatureTemplate.Type type = orig.Invoke(s);

            return type;
        }
        */

        private CreatureTemplate.Type MapCreature(CreatureTemplate.Type type)
        {
            // Check map for creature replacement and replace string if applicable.
            if (creatureMapper.ContainsKey(type))
            {
                CreatureTemplate.Type newType = creatureMapper[type];
                if (type != newType)
                {
                    Logger.LogDebug(string.Concat("Replacing ", type, " with ", newType));
                    return newType;
                }
            }
            return type;
        }

        private AbstractCreature AbstractCreatureFromStringHook(On.SaveState.orig_AbstractCreatureFromString orig, World world, string creatureString, bool onlyInCurrentRegion)
        {
            string[] array = Regex.Split(creatureString, "<cA>");
            CreatureTemplate.Type type = new CreatureTemplate.Type(array[0], false);
            if (type.Index == -1)
            {
                if (RainWorld.ShowLogs)
                {
                    Logger.LogDebug("Unknown creature: " + array[0] + " creature not spawning");
                }
                return null;
            }
            EntityID entityID = EntityID.FromString(array[1]);
            World.SimpleSpawner spawner = world.GetSpawner(entityID) as World.SimpleSpawner;

            if (spawner != null && spawner.creatureType != type && MapCreature(spawner.creatureType) == spawner.creatureType)
            {
                string newCreatureString = creatureString.Replace(type.value, spawner.creatureType.value);
                Logger.LogDebug("Current spawner creature type of " + spawner.creatureType + " does not match saved type " + type + "! Changing creature string:\n" + creatureString + "\n" + newCreatureString);
                return orig.Invoke(world, newCreatureString, onlyInCurrentRegion);
            }

            return orig.Invoke(world, creatureString, onlyInCurrentRegion);
        }
    }
}
