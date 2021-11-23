﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Utils;
using Jotunn.Managers;
using UnityEngine;
using VNEI.Logic;
using VNEI.UI;

namespace VNEI {
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin {
        public const string ModName = "VNEI";
        public const string ModGuid = "com.maxsch.valheim.vnei";
        public const string ModVersion = "0.3.0";
        public static Plugin Instance { get; private set; }
        public static AssetBundle AssetBundle { get; private set; }
        public static HashSet<string> ItemBlacklist { get; private set; } = new HashSet<string>();

        public static ConfigEntry<bool> fixPlants;
        public static ConfigEntry<bool> useBlacklist;
        public static ConfigEntry<bool> invertScroll;
        public static ConfigEntry<int> rowCount;
        public static ConfigEntry<int> columnCount;
        public static ConfigEntry<int> transparency;
        public static ConfigEntry<KeyboardShortcut> openHotkey;

        public static bool isUiOpen = true;
        public static event Action OnOpenHotkey;
        public Sprite noIconSprite;

        private Harmony harmony;

        private void Awake() {
            Instance = this;
            Log.Init(Logger);
            AcceptableValueRange<int> rowRange = new AcceptableValueRange<int>(1, 25);

            AcceptableValueRange<int> percentRange = new AcceptableValueRange<int>(0, 100);

            const string fixPlantsDescription = "This combines plants which are stored as two separate objects to one, " +
                                                "as one is used for seeds and the other for the real plant. " +
                                                "Turn this off if some other mod has problems with this fix and provide a bug report, please";
            fixPlants = Config.Bind("General", "Fix Cultivate Plants", true, new ConfigDescription(fixPlantsDescription));

            const string useBlacklistDescription = "Disables items that are not used currently in the game. This doesn't include " +
                                                   "placeholder items but testing objects for the devs or not obtainable items/effects. " +
                                                   "This list is manual, so please contact me if an item is missing/not on the list";
            useBlacklist = Config.Bind("General", "Use Item Blacklist", true, new ConfigDescription(useBlacklistDescription));

            const string invertScrollDescription = "Inverts scrolling for page switching";
            invertScroll = Config.Bind("General", "Invert Scroll", false, new ConfigDescription(invertScrollDescription));

            const string openHotkeyDescription = "Hotkey to open and close the UI";
            openHotkey = Config.Bind("Hotkeys", "Open UI Hotkey", new KeyboardShortcut(KeyCode.H, KeyCode.LeftAlt), openHotkeyDescription);

            const string columnDescription = "Count of visible horizontal items. Determines the width of the UI";
            columnCount = Config.Bind("UI", "Items Horizontal", 12, new ConfigDescription(columnDescription, rowRange));

            const string rowDescription = "Count of visible vertical items. Determines the height of the UI";
            rowCount = Config.Bind("UI", "Items Vertical", 6, new ConfigDescription(rowDescription, rowRange));

            const string transparentDescription = "Transparency of the background image. Values in percent, 0 = full visible, " +
                                                   "100 = completely transparent";
            transparency = Config.Bind("Visual", "Background Transparency", 0, new ConfigDescription(transparentDescription, percentRange));

            harmony = new Harmony(ModGuid);
            harmony.PatchAll();

            // load embedded asset bundle
            AssetBundle = AssetUtils.LoadAssetBundleFromResources("VNEI_AssetBundle", Assembly.GetExecutingAssembly());

            CustomLocalization localization = new CustomLocalization();

            // load embedded localisation
            string englishJson = AssetUtils.LoadTextFromResources("Localization.English.json", Assembly.GetExecutingAssembly());
            localization.AddJsonFile("English", englishJson);

            LocalizationManager.Instance.AddLocalization(localization);

            // load embedded blacklist
            string blacklistJson = AssetUtils.LoadTextFromResources("ItemBlacklist.json", Assembly.GetExecutingAssembly());
            ItemBlacklist = SimpleJson.SimpleJson.DeserializeObject<List<string>>(blacklistJson).ToHashSet();

            noIconSprite = AssetBundle.LoadAsset<Sprite>("NoSprite.png");
            GUIManager.OnCustomGUIAvailable += BaseUI.CreateDefault;
            CommandManager.Instance.AddConsoleCommand(new SelectUITest.ToggleUIConsoleCommand());
            CommandManager.Instance.AddConsoleCommand(new FileWriterController());
        }

        private void Update() {
            if (openHotkey.Value.IsDown()) {
                isUiOpen = !isUiOpen;
                OnOpenHotkey?.Invoke();
            }
        }

        private void OnDestroy() {
            harmony?.UnpatchAll(ModGuid);
        }
    }
}
