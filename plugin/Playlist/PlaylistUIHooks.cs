using BepInEx;
using Menu;
using UnityEngine;

namespace ArenaTunes;

public partial class ModMain : BaseUnityPlugin
{
    private SymbolButton playlistButton;

    private void PlaylistHooks()
    {
        On.Menu.MultiplayerMenu.ctor += (On.Menu.MultiplayerMenu.orig_ctor orig, MultiplayerMenu self, ProcessManager manager) =>
        {
            orig(self, manager);

            // playlist button
            playlistButton = new SymbolButton(
                self, self.pages[0],
                "musicSymbol", "PLAYLIST",
                self.infoButton.pos + new Vector2(-self.infoButton.size.x - 10f, 0f)    
            );
            
            self.pages[0].subObjects.Add(playlistButton);
        };

        // process playlist button info text
        On.Menu.MultiplayerMenu.UpdateInfoText += (On.Menu.MultiplayerMenu.orig_UpdateInfoText orig, MultiplayerMenu self) =>
        {
            if (self.selectedObject == playlistButton)
            {
                return "Configure playlist";
            }
            else
            {
                return orig(self);
            }
        };

        // process playlist button press
        On.Menu.MultiplayerMenu.Singal += (On.Menu.MultiplayerMenu.orig_Singal orig, MultiplayerMenu self, MenuObject sender, string message) =>
        {
            if (message == "PLAYLIST")
            {
                logger.LogInfo("Configure Playlist");
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                Vector2 dialogPos = new(1000f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, self.manager.rainWorld.screenSize.y - 100f);
                var dialog = new PlaylistConfigDialog(self.manager, availableTracks, activeTracks);
                self.manager.ShowDialog(dialog);
            }
            else
            {
                orig(self, sender, message);
            }
        };
    }
}