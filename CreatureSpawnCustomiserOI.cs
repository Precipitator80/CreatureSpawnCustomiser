using KeepThatAwayFromMe;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using UnityEngine;
using static IconSymbol;

namespace CreatureSpawnCustomiser
{
    public class CreatureSpawnCustomiserOI : OptionInterface
    {
        public CreatureSpawnCustomiserOI() : base()
        {
            // Solve ambiguity between itself due to compiler generated copies? - Michael Liu - https://stackoverflow.com/questions/75923114/solve-ambiguity-between-itself-due-to-compiler-generated-copies - Accessed 21.07.2023
            try
            {
                var onConfigChanged = typeof(OptionInterface).GetEvent("OnConfigChanged");
                onConfigChanged.AddEventHandler(this, Delegate.CreateDelegate(onConfigChanged.EventHandlerType, this, typeof(CreatureSpawnCustomiserOI).GetMethod("ConfigOnChange")));
                Debug.Log("Hooked CreatureSpawnCustomiserOI");
            }
            catch (Exception e)
            {
                Debug.Log("Error hooking CreatureSpawnCustomiserOI\n" + e.ToString());
            }
        }

        public void ConfigOnChange()
        {
            // Grab Creatures
            var replaceCrits = new Dictionary<CreatureTemplate.Type, CreatureTemplate.Type>();
            for (int i = 0; i < CreatureSpawnCustomiserPlugin.critTypeReplacements.Length; i++)
            {
                Debug.Log($"Key and Value: {CreatureSpawnCustomiserPlugin.critTypeReplacements[i].key.Replace(KEY_NAME, "")}, {CreatureSpawnCustomiserPlugin.critTypeReplacements[i].Value.value}");
                if (!CreatureSpawnCustomiserPlugin.critTypeReplacements[i].key.Replace(KEY_NAME, "").Equals(CreatureSpawnCustomiserPlugin.critTypeReplacements[i].Value.value))
                {
                    replaceCrits.Add(PhobiaPlugin.allCritTypes[i], CreatureSpawnCustomiserPlugin.critTypeReplacements[i].Value);
                }
            }
            CreatureSpawnCustomiserPlugin.creatureMapper = replaceCrits;

            Debug.Log($"replacedCrits: {replaceCrits.Count}");
        }

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

            Tabs[0].AddItems(new OpRect(new Vector2(30f, 0f), new Vector2(540f, 470f)), lblCritAlert,
                new OpLabel(new Vector2(50f, 520f), new Vector2(240f, 40f), Translate("Creature List"), FLabelAlignment.Left, true),
                new OpLabel(new Vector2(100f, 475f), new Vector2(64f, 20f), Translate("REPLACE"), FLabelAlignment.Center),
                new OpRect(new Vector2(320f, 495f), new Vector2(240f, 80f), 0.2f));

            // Main List
            float GetCritOffset(int idx) => (PhobiaPlugin.allCritTypes.Length - idx) * ITEM_INTERVAL - 15.01f;
            ckCrits = new OpComboBox2[PhobiaPlugin.allCritTypes.Length];
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
                ckCrits[c] = new OpComboBox2(CreatureSpawnCustomiserPlugin.critTypeReplacements[c], new Vector2(90f, GetCritOffset(c) + 3f), 150f, creatureListItems);
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

        internal static string GenerateCritKey(CreatureTemplate.Type type) => $"{KEY_NAME}{type.value}";
        internal static readonly string KEY_NAME = "ReplaceCrit";

        private Dictionary<string, int> idxCrits;
        private OpComboBox2[] ckCrits;
        private OpScrollBox sbCrits;
        private OpLabel lblCritAlert;
    }
}