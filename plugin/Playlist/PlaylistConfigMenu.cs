using System;
using Menu;
using UnityEngine;

namespace ArenaTunes;

class PlaylistConfigMenu : PositionedMenuObject
{
    public PlaylistConfigMenu(PlaylistConfigDialog menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        menu.tabWrapper = new Menu.Remix.MenuTabWrapper(menu, this);
        subObjects.Add(menu.tabWrapper);

        // main label
        subObjects.Add(new MenuLabel(
            menu: this.menu,
            owner: this,
            text: "DJ Playlist",
            pos: new Vector2(110f, menu.manager.rainWorld.options.ScreenSize.y - 100f),
            size: default,
            bigText: true
        ));

        // back button
        subObjects.Add(new SimpleButton(
            this.menu, this,
            displayText: "BACK",
            singalText: "BACK",
            pos: new(200f, 50f),
            size: new Vector2(110f, 30f)
        ));
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        switch (message)
        {
            case "BACK":
                (menu as PlaylistConfigDialog).RequestClose();
                break;
        }
    }
}