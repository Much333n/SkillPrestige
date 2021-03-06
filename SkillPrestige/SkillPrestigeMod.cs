﻿using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.IO;
using SkillPrestige.Commands;
using SkillPrestige.Logging;
using SkillPrestige.Menus;
using SkillPrestige.Menus.Elements.Buttons;
using SkillPrestige.Mods;
using SkillPrestige.Professions;
using StardewValley.Menus;
using static SkillPrestige.InputHandling.Mouse;

namespace SkillPrestige
{
    /// <summary>
    /// The Skill Prestige Mod by Alphablackwolf. Enjoy!
    /// </summary>
    public class SkillPrestigeMod : Mod
    {
        #region Manifest Data

        private static string Author => "Alphablackwolf";

        private static System.Version Version => typeof(SkillPrestigeMod).Assembly.GetName().Version;

        public static string Name => "Skill Prestige Mod";

        //public static string Guid => "6b843e60-c8fc-4a25-a67b-4a38ac8dcf9b";

        public static string ModPath { get; private set; }

        public static string OptionsPath { get; private set; }

        public static IMonitor LogMonitor { get; private set; }

        public static string CurrentSaveOptionsPath { get; private set; }

        public static string PerSaveOptionsDirectory { get; private set; }

        public static Texture2D PrestigeIconTexture { get; private set; }

        public static Texture2D CheckmarkTexture { get; private set; }
        

        #endregion

        public override void Entry(IModHelper helper)
        {
            LogMonitor = Monitor;
            ModPath = helper.DirectoryPath;
            PerSaveOptionsDirectory = Path.Combine(ModPath, "psconfigs/");
            OptionsPath = Path.Combine(ModPath, "config.json");
            Logger.LogInformation("Detected game entry.");
            PrestigeSaveData.Instance.Read();
            RegisterGameEvents();
            Logger.LogDisplay($"{Name} version {Version} by {Author} Initialized.");
        }

        private void GameLoaded(object sender, EventArgs args)
        {
            Logger.LogInformation("Detected game load.");
            if (Type.GetType("AllProfessions.AllProfessions, AllProfessions") != null)
            {
                Logger.LogCriticalWarning("Conflict Detected. This mod cannot work with AllProfessions. Skill Prestige disabled.");
                Logger.LogDisplay("Skill Prestige Mod: If you wish to use this mod in place of AllProfessions, remove the AllProfessions mod and run the player_resetallprofessions command.");
                DeregisterGameEvents();
                return;
            }
            ModHandler.RegisterLoadedMods();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse - constant is manually changed for testing.
            if (Options.Instance.TestingMode) RegisterTestingCommands();
            RegisterCommands();
        }

        private void RegisterGameEvents()
        {
            Logger.LogInformation("Registering game events...");
            GameEvents.GameLoaded += GameLoaded;
            GameEvents.LoadContent += LoadSprites;
            ControlEvents.MouseChanged += MouseChanged;
            LocationEvents.CurrentLocationChanged += LocationChanged;
            GraphicsEvents.OnPostRenderGuiEvent += PostRenderGuiEvent;
            GameEvents.UpdateTick += GameUpdate;
            Logger.LogInformation("Game events registered.");
        }

        private void DeregisterGameEvents()
        {
            Logger.LogInformation("Deregistering game events...");
            GameEvents.GameLoaded -= GameLoaded;
            GameEvents.LoadContent -= LoadSprites;
            ControlEvents.MouseChanged -= MouseChanged;
            LocationEvents.CurrentLocationChanged -= LocationChanged;
            GraphicsEvents.OnPostRenderGuiEvent -= PostRenderGuiEvent;
            GameEvents.UpdateTick -= GameUpdate;
            Logger.LogInformation("Game events deregistered.");
        }

        private static void MouseChanged(object sender, EventArgsMouseStateChanged args)
        {
            HandleState(args);
        }

        private static void LocationChanged(object sender, EventArgs args)
        {
            Logger.LogVerbose("Location change detected.");
            CurrentSaveOptionsPath = Path.Combine(ModPath, "psconfigs/", $@"{Game1.player.name.RemoveNonAlphanumerics()}_{Game1.uniqueIDForThisGame}.json");
            PrestigeSaveData.Instance.UpdateCurrentSaveFileInformation();
            PerSaveOptions.Instance.Check();
            Profession.AddMissingProfessions();
        }

        private static void PostRenderGuiEvent(object sender, EventArgs args)
        {
            SkillsMenuExtension.AddPrestigeButtonsToMenu();
        }

        private static void LoadSprites(object sender, EventArgs args)
        {
            Logger.LogInformation("Loading sprites...");
            Button.DefaultButtonTexture = Game1.content.Load<Texture2D>(@"LooseSprites\DialogBoxGreen");
            MinimalistProfessionButton.ProfessionButtonTexture = Game1.content.Load<Texture2D>(@"LooseSprites\boardGameBorder");

            var prestigeIconFilePath = Path.Combine(ModPath, @"PrestigeIcon.png");
            Logger.LogInformation($"Prestige Icon Path: {prestigeIconFilePath}");
            var prestigeIconFileStream = new FileStream(prestigeIconFilePath, FileMode.Open);
            PrestigeIconTexture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, prestigeIconFileStream);

            var checkmarkFilePath = Path.Combine(ModPath, @"Checkmark.png");
            Logger.LogInformation($"Checkmark Path: {checkmarkFilePath}");
            var checkmarkFileStream = new FileStream(checkmarkFilePath, FileMode.Open);
            CheckmarkTexture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, checkmarkFileStream);
            Logger.LogInformation("Sprites loaded.");
        }

        private static void GameUpdate(object sender, EventArgs args)
        {
            CheckForGameSave();
            CheckForLevelUpMenu();
        }

        private static void CheckForGameSave()
        {
            if (!Game1.newDay || Game1.fadeToBlackAlpha <= 0.95f) return;
            Logger.LogInformation("New game day detected.");
            PrestigeSaveData.Instance.Save();
        }

        private static void CheckForLevelUpMenu()
        {
            var levelUpMenu = Game1.activeClickableMenu as LevelUpMenu;
            var currentLevel = (int?)levelUpMenu?.GetInstanceField("currentLevel");
            if (currentLevel % 5 != 0) return;
            Logger.LogInformation("Level up menu as profession chooser detected.");
            var currentSkill = (int)levelUpMenu.GetInstanceField("currentSkill");
            Game1.activeClickableMenu = new LevelUpMenuDecorator(currentSkill, currentLevel.Value);
            Logger.LogInformation("Replaced level up menu with custom menu.");
        }

        private static void RegisterTestingCommands()
        {
            Logger.LogInformation("Registering Testing commands...");
            SkillPrestigeCommand.RegisterCommands(true);
            Logger.LogInformation("Testing commands registered.");
        }

        private static void RegisterCommands()
        {
            Logger.LogInformation("Registering commands...");
            SkillPrestigeCommand.RegisterCommands(false);
            Logger.LogInformation("Commands registered.");
        }
    }
}