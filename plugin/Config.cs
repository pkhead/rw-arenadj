using Menu.Remix.MixedUI;
using UnityEngine;
using System.IO;
using System;

namespace ArenaTunes
{
    public class Options : OptionInterface
    {
        public static Options instance = new();

        public static Configurable<string> FolderPath = instance.config.Bind(
            key: "folderPath",
            defaultValue: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "pkhead.arenatunes"),
            info: new ConfigurableInfo(
                "The path to the folder containing custom music"
            )
        );

        public static Configurable<bool> CustomOnly = instance.config.Bind(
            key: "customOnly",
            defaultValue: false,
            info: new ConfigurableInfo(
                "Only play custom music registered with this mod",
                null,
                "",
                "CustomOnly"
            )
        );

        public Options() {}

        public override void Initialize()
        {
            base.Initialize();
            InitBuilder(1);

            AddTab("General");
            Title("Arena Mix Config");
            AddTextbox("Music Folder", "The path to the folder containing the custom music", FolderPath, 400);
            AddCheckbox("Custom only", "Only play custom music registered with this mod", CustomOnly);
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

        private void AddTextbox(string text, string desc, Configurable<string> config, int width)
        {
            var textbox = new OpTextBox(config, new Vector2(curX, curY), width) {
                description = desc
            };

            var label = new OpLabel(curX + textbox.size.x + LABEL_MARGIN_X, curY + (textbox.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(textbox, label);
            curY -= textbox.size.y + ITEM_MARGIN_Y;
        }
        private void AddSlider(string text, string desc, Configurable<int> config, int width)
        {
            var slider = new OpSlider(config, new Vector2(curX, curY), width) {
                description = desc
            };

            var label = new OpLabel(curX + slider.size.x + LABEL_MARGIN_X, curY + (slider.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(slider, label);
            curY -= slider.size.y + ITEM_MARGIN_Y;
        }

        private void AddCheckbox(string text, string desc, Configurable<bool> config)
        {
            var checkbox = new OpCheckBox(config, curX, curY) {
                description = desc
            };

            var label = new OpLabel(curX + checkbox.size.x + LABEL_MARGIN_X, curY + (checkbox.size.y - LabelTest.LineHeight(false)) / 2, text, false);
            Tabs[tabIndex].AddItems(checkbox, label);
            curY -= checkbox.size.y + ITEM_MARGIN_Y;
        }

        #endregion
    }
}