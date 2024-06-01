using System.Collections.Generic;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;

namespace ArenaTunes;

class TrackButton : ButtonTemplate
{
    private readonly TrackList listOwner;
    private readonly string signalText;
    private readonly MenuLabel menuLabel;
    private HSLColor labelColor;
    private readonly Menu.Remix.MixedUI.GlowGradient glowGradient;
    private Vector2 relativePos;

    private float labelAlpha = 0f;
    private float prevLabelAlpha = 0f;

    public readonly string TrackName;
    public ref Vector2 RelativePos { get => ref relativePos; }

    public TrackButton(Menu.Menu menu, MenuObject owner, string trackName, string signalText, Vector2 pos, Vector2 size)
        : base(menu, owner, pos, size)
    {
        listOwner = owner as TrackList;
        relativePos = pos;
        this.signalText = signalText;
        TrackName = trackName;

        labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
        menuLabel = new MenuLabel(menu, this, trackName, Vector2.zero, size, false);
        menuLabel.label.alpha = labelAlpha;
        subObjects.Add(menuLabel);
        
        glowGradient = new Menu.Remix.MixedUI.GlowGradient(Container, Vector2.zero, size, 0.5f)
        {
            color = labelColor.rgb
        };
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        glowGradient.sprite.RemoveFromContainer();
    }

    public override void Update()
    {
        base.Update();
        prevLabelAlpha = labelAlpha;
        buttonBehav.greyedOut = labelAlpha < 0.2f; // disable button if outside of scroll list bounds
        buttonBehav.Update();

        // fade out if this button is on edge
        float targetAlpha = 1f;
        if (relativePos.y < listOwner.ViewMin)
        {
            targetAlpha = Custom.LerpMap(relativePos.y, listOwner.ViewMin - size.y, listOwner.ViewMin, 0f, 1f);
        }
        else if (relativePos.y > listOwner.ViewMax - size.y)
        {
            targetAlpha = Custom.LerpMap(relativePos.y, listOwner.ViewMax - size.y, listOwner.ViewMax, 1f, 0f);
        }
        labelAlpha += (targetAlpha - labelAlpha) * 0.4f;
        
        // update position
        pos = Vector2.up * listOwner.size.y + relativePos + Vector2.up * listOwner.ScrollPixelOffset;
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        menuLabel.label.color = InterpColor(timeStacker, labelColor);
        menuLabel.label.alpha = Mathf.Lerp(prevLabelAlpha, labelAlpha, timeStacker);

        float buttonAlpha = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
        buttonAlpha *= buttonBehav.sizeBump;
        glowGradient.alpha = buttonAlpha;
        glowGradient.centerPos = DrawPos(timeStacker) + size / 2.0f;
    }

    public override void Clicked()
    {
        base.Clicked();
        Singal(this, signalText);
    }
}

class TrackList : RectangularMenuObject
{
    private readonly FSprite[] sideBars = new FSprite[2];
    private int scrollInt = 0;
    public float ScrollPixelOffset { get => floatScrollPos; }
    public int ScrollItemOffset { get => scrollInt; set => scrollInt = value; }
    private float nextButtonPos = -ItemHeight;
    private int itemCount = 0;
    private float floatScrollPos = 0;
    private float floatScrollVel = 0;
    private const float ItemHeight = 20f;

    public float ViewMin {
        get => -floatScrollPos - size.y;
    }

    public float ViewMax {
        get => -floatScrollPos;
    }

    public TrackList(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        sideBars[0] = new FSprite("pixel", true)
        {
            anchorX = 0,
            anchorY = 0,
            scaleX = 2f,
            scaleY = size.y,
            color = new Color(1f, 1f, 1f, 1f),
        };

        sideBars[1] = new FSprite("pixel", true)
        {
            anchorX = 0,
            anchorY = 0,
            scaleX = 2f,
            scaleY = size.y,
            color = new Color(1f, 1f, 1f, 1f)
        };

        Container.AddChild(sideBars[0]);
        Container.AddChild(sideBars[1]);
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        sideBars[0].RemoveFromContainer();
        sideBars[1].RemoveFromContainer();
    }

    public void AddTrack(string trackName, string signalText)
    {
        int maxScrollValue = Custom.IntClamp(itemCount - (int)(size.y / ItemHeight), 0, itemCount);

        // if user had already scrolled to the bottom of the list,
        // scroll downwards when an item is added
        if (scrollInt == maxScrollValue)
        {
            scrollInt++;
        }

        // add the button
        var btn = new TrackButton(
            menu: menu,
            owner: this,
            trackName: trackName,
            signalText: signalText,
            pos: new Vector2(2f, nextButtonPos),
            size: new Vector2(size.x - 4f, ItemHeight)
        );
        nextButtonPos -= btn.size.y;
        subObjects.Add(btn);
        itemCount++;
    }

    public void RemoveTrack(string trackName)
    {
        float shiftY = 0.0f;

        for (int i = 0; i < subObjects.Count;)
        {
            if (subObjects[i] is TrackButton trackButton)
            {
                if (trackButton.TrackName == trackName)
                {
                    shiftY = trackButton.size.y;
                    subObjects.RemoveAt(i);
                    trackButton.RemoveSprites();
                    page.selectables.Remove(trackButton);
                    nextButtonPos += trackButton.size.y;
                    itemCount--;
                }
                else
                {
                    trackButton.RelativePos.y += shiftY;
                    i++;
                }
            }
        }
    }

    public List<TrackButton> GetItems()
    {
        List<TrackButton> buttons = new();

        foreach (var item in subObjects)
        {
            if (item is TrackButton trackButton)
                buttons.Add(trackButton);
        }

        return buttons;
    }

    public void Clear()
    {
        nextButtonPos = -ItemHeight;
        scrollInt = 0;
        itemCount = 0;

        for (int i = subObjects.Count - 1; i >= 0; i--)
        {
            if (subObjects[i] is TrackButton trackButton)
            {
                subObjects.RemoveAt(i);
                trackButton.RemoveSprites();
                page.selectables.Remove(trackButton);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
        {
            Debug.Log(menu.mouseScrollWheelMovement);
            scrollInt += menu.mouseScrollWheelMovement * 4;
        }

        // clamp scroll
        int maxScrollValue = Custom.IntClamp(itemCount - (int)(size.y / ItemHeight), 0, itemCount);
        scrollInt = Custom.IntClamp(scrollInt, 0, maxScrollValue);

        // spring-based scroll movement
        float desiredScrollPos = scrollInt * ItemHeight;
        floatScrollVel += (desiredScrollPos - floatScrollPos) * 0.08f - floatScrollVel * 0.37f;
        floatScrollPos += floatScrollVel;
        
        // i don't know what i'm doing
        /*int si = scrollInt;
        floatScrollPos = Custom.LerpAndTick(floatScrollPos, si, 0.01f, 0.01f);
        floatScrollVel *= Custom.LerpMap(Math.Abs(si - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f);
        floatScrollVel += Mathf.Clamp(floatScrollVel, -1.2f, 1.2f);
        floatScrollPos += floatScrollVel;*/
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        // maintain sidebar positions
        sideBars[0].SetPosition(ScreenPos);
        sideBars[1].SetPosition(ScreenPos + Vector2.right * (size.x - 2f));
    }
}

class PlaylistConfigMenu : PositionedMenuObject
{
    private readonly TrackList availableTracksUi;
    private readonly TrackList activeTracksUi;

    private readonly List<string> activeTracks;

    public readonly SimpleButton backButton;
    public readonly SimpleButton addAllButton;
    public readonly SimpleButton removeAllButton;

    public PlaylistConfigMenu(
        PlaylistConfigDialog menu, MenuObject owner,
        Vector2 pos,
        string[] availableTracks,
        List<string> activeTracks
    )
        : base(menu, owner, pos)
    {
        this.activeTracks = activeTracks;

        menu.tabWrapper = new Menu.Remix.MenuTabWrapper(menu, this);
        subObjects.Add(menu.tabWrapper);

        // main label
        /*subObjects.Add(new MenuLabel(
            menu: this.menu,
            owner: this,
            text: "DJ Playlist",
            pos: new Vector2(110f, menu.manager.rainWorld.options.ScreenSize.y - 100f),
            size: default,
            bigText: true
        ));*/

        // back button
        subObjects.Add(backButton = new SimpleButton(
            this.menu, this,
            displayText: "DONE",
            singalText: "BACK",
            pos: new(200f, 50f),
            size: new Vector2(110f, 30f)
        ));

        // available songs track list label
        subObjects.Add(new MenuLabel(
            menu: this.menu,
            owner: this,
            text: "AVAILABLE",
            pos: new Vector2(563f, 400f + 216f),
            size: default,
            bigText: false
        ));

        // available songs track list
        subObjects.Add(availableTracksUi = new(
            menu: this.menu,
            owner: this,
            pos: new Vector2(463f, 216f),
            size: new Vector2(200f, 400f - 15f)
        ));

        // active songs track list label
        subObjects.Add(new MenuLabel(
            menu: this.menu,
            owner: this,
            text: "ACTIVE",
            pos: new Vector2(803f, 400f + 216f),
            size: default,
            bigText: false
        ));

        // active songs track list
        subObjects.Add(activeTracksUi = new(
            menu: this.menu,
            owner: this,
            pos: new Vector2(703f, 216f),
            size: new Vector2(200f, 400f - 15f)
        ));

        // add all button
        subObjects.Add(addAllButton = new SimpleButton(
            menu: this.menu,
            owner: this,
            displayText: "ADD ALL",
            singalText: "ADD_ALL",
            pos: new Vector2(463f, 216f - 45f),
            size: new Vector2(100f, 30f)
        ));

        // remove all button
        subObjects.Add(removeAllButton = new SimpleButton(
            menu: this.menu,
            owner: this,
            displayText: "CLEAR PLAYLIST",
            singalText: "REMOVE_ALL",
            pos: new Vector2(703f, 216f - 45f),
            size: new Vector2(110f, 30f)
        ));

        // add the track items
        foreach (var trackName in availableTracks)
        {
            if (activeTracks.Contains(trackName))
                activeTracksUi.AddTrack(trackName, "REMOVE_TRACK");
            else
                availableTracksUi.AddTrack(trackName, "ADD_TRACK");
        }

        availableTracksUi.ScrollItemOffset = 0;
        activeTracksUi.ScrollItemOffset = 0;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        switch (message)
        {
            case "BACK":
                (menu as PlaylistConfigDialog).RequestClose();
                ModMain.Instance.SavePlaylist();
                
                menu.PlaySound(SoundID.MENU_Switch_Page_Out);

                // reset dj
                var musicPlayer = menu.manager.musicPlayer;
                if (musicPlayer.multiplayerDJ != null)
                {
                    var dj = musicPlayer.multiplayerDJ;
                    dj.availableSongs = activeTracks.ToArray();
                    dj.playList.Clear();
                }
                break;

            case "ADD_TRACK":
            {
                if (sender is TrackButton trackButton)
                {
                    activeTracks.Add(trackButton.TrackName);
                    availableTracksUi.RemoveTrack(trackButton.TrackName);
                    activeTracksUi.AddTrack(trackButton.TrackName, "REMOVE_TRACK");
                    menu.PlaySound(SoundID.MENU_Add_Level);
                }
                break;
            }

            case "REMOVE_TRACK":
            {
                if (sender is TrackButton trackButton)
                {
                    activeTracks.Remove(trackButton.TrackName);
                    activeTracksUi.RemoveTrack(trackButton.TrackName);
                    availableTracksUi.AddTrack(trackButton.TrackName, "ADD_TRACK");
                    menu.PlaySound(SoundID.MENU_Remove_Level);
                }
                break;
            }

            case "ADD_ALL":
            {
                menu.PlaySound(SoundID.MENU_Add_Level);
                
                foreach (var trackButton in availableTracksUi.GetItems())
                {
                    activeTracks.Add(trackButton.TrackName);
                    activeTracksUi.AddTrack(trackButton.TrackName, "REMOVE_TRACK");
                }

                availableTracksUi.Clear();
                break;
            }

            case "REMOVE_ALL":
            {
                menu.PlaySound(SoundID.MENU_Remove_Level);
                
                foreach (var trackButton in activeTracksUi.GetItems())
                {
                    activeTracks.Remove(trackButton.TrackName);
                    availableTracksUi.AddTrack(trackButton.TrackName, "ADD_TRACK");
                }

                activeTracksUi.Clear();
                break;
            }
        }
    }
}