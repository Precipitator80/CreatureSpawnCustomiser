using BepInEx;
using KeepThatAwayFromMe;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using static IconSymbol;

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

                critTypeReplacements = new Configurable<string>[okayNames.Count];
                for (int j = 0; j < okayNames.Count; j++)
                {
                    critTypeReplacements[j] = oi.config.Bind(CreatureSpawnCustomiserOI.GenerateCritKey(PhobiaPlugin.allCritTypes[j]), PhobiaPlugin.allCritTypes[j].value);
                }
            }
        }

        static Dictionary<CreatureTemplate.Type, CreatureTemplate.Type> creatureMapper = new Dictionary<CreatureTemplate.Type, CreatureTemplate.Type>()
        {
            {CreatureTemplate.Type.GreenLizard, CreatureTemplate.Type.PinkLizard}
        };
        public static Configurable<string>[] critTypeReplacements = new Configurable<string>[0];
        private static bool init = false;
        public static CreatureSpawnCustomiserPlugin instance;


        public void Patch()
        {
            try
            {
                Logger.LogInfo("Hooking Creature Spawn Customiser");
                On.WorldLoader.CreatureTypeFromString += CreatureTypeFromStringHook;
                Logger.LogInfo("Hooked Creature Spawn Customiser");
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to enable Creature Spawn Customiser.\n" + e.ToString());
            }
        }

        public CreatureTemplate.Type CreatureTypeFromStringHook(On.WorldLoader.orig_CreatureTypeFromString orig, string s)
        {
            // Run the original code.
            CreatureTemplate.Type type = orig.Invoke(s);

            // Check map for creature replacement and replace string if applicable.
            if (creatureMapper.ContainsKey(type))
            {
                CreatureTemplate.Type newType = creatureMapper[type];
                Logger.LogDebug(string.Concat("Replacing ", type, " with ", newType));
                return newType;
            }
            return type;
        }
    }

    public class CreatureSpawnCustomiserOI : OptionInterface
    {
        public override void Initialize()
        {
            base.Initialize();
            this.Tabs = new OpTab[]
            {
                new OpTab(this, OptionInterface.Translate("Creatures"))
            };
            const float ITEM_INTERVAL = 40f, ITEM_HEIGHT = 30f;
            Vector2 ICON_ANCHOR = new Vector2(1.0f, 0.5f);

            // Decorations
            lblCritAlert = new OpLabel(new Vector2(260f, 575f), new Vector2(300f, 20f), "", FLabelAlignment.Right);
            alertAlpha = new float[] { 0f, 0f };
            alertSin = new float[] { 0f, 0f };
            Tabs[0].AddItems(new OpRect(new Vector2(30f, 0f), new Vector2(540f, 470f)), lblCritAlert,
                new OpLabel(new Vector2(50f, 520f), new Vector2(240f, 40f), Translate("Creature List"), FLabelAlignment.Left, true),
                new OpLabel(new Vector2(100f, 475f), new Vector2(64f, 20f), Translate("REPLACE"), FLabelAlignment.Center),
                new OpRect(new Vector2(320f, 495f), new Vector2(240f, 80f), 0.2f));

            // Main List
            float GetCritOffset(int idx) => (PhobiaPlugin.allCritTypes.Length - idx) * ITEM_INTERVAL - 15.01f;
            ckCrits = new OpComboBox[PhobiaPlugin.allCritTypes.Length];
            idxCrits = new Dictionary<string, int>();
            sbCrits = new OpScrollBox(new Vector2(30f, 0f), new Vector2(540f, 470f), PhobiaPlugin.allCritTypes.Length * ITEM_INTERVAL + 40f, false, false, true);
            Tabs[0].AddItems(sbCrits);

            List<ListItem> creatureListItems = new List<ListItem>();
            for (int c = 0; c < PhobiaPlugin.allCritTypes.Length; c++)
            {
                creatureListItems.Add(new ListItem(PhobiaPlugin.allCritTypes[c].value));
            }

            for (int c = 0; c < PhobiaPlugin.allCritTypes.Length; c++)
            {
                idxCrits.Add(GenerateCritKey(PhobiaPlugin.allCritTypes[c]), c);
                ckCrits[c] = new OpComboBox(CreatureSpawnCustomiserPlugin.critTypeReplacements[c], new Vector2(90f, GetCritOffset(c) + 3f), 150f, creatureListItems);
                sbCrits.AddItems(ckCrits[c]);
                IconSymbolData iconData = new IconSymbolData(PhobiaPlugin.allCritTypes[c], AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                string iconName = CreatureSymbol.SpriteNameOfCreature(iconData);
                if (iconName != "Futile_White")
                {
                    sbCrits.AddItems(new OpImage(new Vector2(80f, GetCritOffset(c) + ITEM_HEIGHT / 2f), iconName)
                    { color = CreatureSymbol.ColorOfCreature(iconData), anchor = ICON_ANCHOR });
                }
                string text = PhobiaPlugin.allCritTypes[c].ToString(), key = "creaturetype-" + text, id = text.ToString();
                text = Translate(key);
                if (text == key) text = id;
                else text += $" ({Translate("MenuModStat_ModID").Replace("<ModID>", id)})";
                sbCrits.AddItems(new OpLabel(new Vector2(260f, GetCritOffset(c)), new Vector2(160f, ITEM_HEIGHT), text, FLabelAlignment.Left) { bumpBehav = ckCrits[c].bumpBehav });
                if (c > 0) UIfocusable.MutualVerticalFocusableBind(ckCrits[c], ckCrits[c - 1]);
            }
        }

        internal static string GenerateCritKey(CreatureTemplate.Type type) => $"ReplaceCrit{type.value}";

        private Dictionary<string, int> idxCrits;
        private OpComboBox[] ckCrits;
        private OpScrollBox sbCrits;
        private OpLabel lblCritAlert;
        private float[] alertAlpha, alertSin;
    }
}
