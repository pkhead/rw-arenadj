using Menu.Remix.MixedUI;
using UnityEngine;
using System.IO;
using System;

namespace ArenaTunes
{
    public class Options : OptionInterface
    {
        private readonly BepInEx.Logging.ManualLogSource logger;

        public static Configurable<string> FolderPath = null;
        public static Configurable<bool> AuthorName = null;

        public Options(ModMain mod)
        {
            logger = mod.logger;

            FolderPath = config.Bind(
                key: "folderPath",
                defaultValue: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "RWArenaMusic"),
                info: new ConfigurableInfo(
                    "The path to the folder containing custom tracks"
                )
            );

            AuthorName = config.Bind(
                key: "authorName",
                defaultValue: false,
                info: new ConfigurableInfo(
                    "Show author name when playing a custom track",
                    null,
                    "",
                    "AuthorName"
                )
            );
        }

        private OpLabel existsLabel = null;
        private OpTextBox folderTextbox = null;

        public override void Initialize()
        {
            base.Initialize();
            InitBuilder(1);

            AddTab("General");
            Title("Arena DJ");
            folderTextbox = AddTextbox("Music Folder", "The path to the folder containing the custom tracks", FolderPath, 400);
            existsLabel = AddLabel("Folder does not exist!");
            AddCheckbox("Author Name", "Show author name when playing a custom track", AuthorName);
            
            CheckExists();
        }

        public override void Update()
        {
            base.Update();
            CheckExists();
        }

        private static string lastFolderName = "";

        private void CheckExists()
        {
            if (existsLabel == null || folderTextbox == null) return;
            if (folderTextbox.value == lastFolderName) return;
            lastFolderName = folderTextbox.value;
            
            if (Directory.Exists(folderTextbox.value))
            {
                existsLabel.Hide();
            }
            else
            {
                existsLabel.Show();
            }
        }

        #region UI Builder functions

        private const float ITEM_HEIGHT = 30f;
        private const float ITEM_MARGIN_Y = 5f;
        private const float LABEL_MARGIN_X = 20f;
        private const float MENU_TOP = 600f - 50f;
        private const float MENU_RIGHT = 600f; // i don't really know

        private int tabIndex = -1;
        private float curY = MENU_TOP - ITEM_HEIGHT;
        private float curX = 5;

        private void InitBuilder(int tabCount)
        {
            tabIndex = -1;
            curY = MENU_TOP - 60;
            curX = 0;
            Tabs = new OpTab[tabCount];
        }

        private OpTab AddTab(string tabName, Color color)
        {
            var tab = AddTab(tabName);
            tab.colorButton = color;
            return tab;
        }

        private OpTab AddTab(string tabName)
        {
            var tab = new OpTab(this, tabName);
            Tabs[++tabIndex] = tab;
            curY = MENU_TOP - 60;
            curX = 0;
            
            return tab;
        }

        private void Title(string text)
        {
            var label = new OpLabel(
                new Vector2(0, MENU_TOP - 10f),
                new Vector2(MENU_RIGHT, 10f), text, FLabelAlignment.Center, true);
            
            Tabs[tabIndex].AddItems(label);
        }

        private OpLabel AddLabel(string text)
        {
            var label = new OpLabel(curX, curY, text, false);
            Tabs[tabIndex].AddItems(label);
            curY -= label.size.y + ITEM_MARGIN_Y;
            return label;
        }

        private OpTextBox AddTextbox(string text, string desc, Configurable<string> config, int width)
        {
            var textbox = new OpTextBox(config, new Vector2(curX, curY), width) {
                description = desc
            };

            var label = new OpLabel(curX + textbox.size.x + LABEL_MARGIN_X, curY + (textbox.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(textbox, label);
            curY -= textbox.size.y + ITEM_MARGIN_Y;
            return textbox;
        }

        private OpSlider AddSlider(string text, string desc, Configurable<int> config, int width)
        {
            var slider = new OpSlider(config, new Vector2(curX, curY), width) {
                description = desc
            };

            var label = new OpLabel(curX + slider.size.x + LABEL_MARGIN_X, curY + (slider.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(slider, label);
            curY -= slider.size.y + ITEM_MARGIN_Y;
            return slider;
        }

        private OpCheckBox AddCheckbox(string text, string desc, Configurable<bool> config)
        {
            var checkbox = new OpCheckBox(config, curX, curY) {
                description = desc
            };

            var label = new OpLabel(curX + checkbox.size.x + LABEL_MARGIN_X, curY + (checkbox.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(checkbox, label);
            curY -= checkbox.size.y + ITEM_MARGIN_Y;
            return checkbox;
        }

        private OpSimpleButton AddSimpleButton(string text, int width)
        {
            var btn = new OpSimpleButton(new Vector2(curX, curY), new Vector2(width, ITEM_HEIGHT), text);
            Tabs[tabIndex].AddItems(btn);
            curY -= btn.size.y + ITEM_MARGIN_Y;
            return btn;
        }

        #endregion
    }
}