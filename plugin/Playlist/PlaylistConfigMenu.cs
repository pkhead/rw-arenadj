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

    public TrackButton(Menu.Menu menu, MenuObject owner, string displayText, string signalText, Vector2 pos, Vector2 size)
        : base(menu, owner, pos, size)
    {
        listOwner = owner as TrackList;
        relativePos = pos;
        this.signalText = signalText;

        labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
        menuLabel = new MenuLabel(menu, this, displayText, Vector2.zero, size, false);
        menuLabel.label.alpha = labelAlpha;
        subObjects.Add(menuLabel);
        
        glowGradient = new Menu.Remix.MixedUI.GlowGradient(Container, Vector2.zero, size, 0.5f)
        {
            color = labelColor.rgb
        };
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
        labelAlpha += (targetAlpha - labelAlpha) * 0.5f;
        
        // update position
        pos = relativePos + Vector2.down * (owner as TrackList).ScrollOffset;
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
    public float ScrollOffset { get => floatScrollPos; }
    private float nextButtonPos = 0f;
    private int itemCount = 0;
    private float floatScrollPos = 0;
    private float floatScrollVel = 0;
    private const float ItemHeight = 20f;

    public float ViewMin {
        get => floatScrollPos;
    }

    public float ViewMax {
        get => floatScrollPos + size.y;
    }

    public TrackList(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        sideBars[0] = new FSprite("pixel", true)
        {
            anchorX = 0,
            anchorY = 0,
            scaleX = 1f,
            scaleY = size.y,
            color = new Color(1f, 1f, 1f, 1f),
        };

        sideBars[1] = new FSprite("pixel", true)
        {
            anchorX = 0,
            anchorY = 0,
            scaleX = 1f,
            scaleY = size.y,
            color = new Color(1f, 1f, 1f, 1f)
        };

        Container.AddChild(sideBars[0]);
        Container.AddChild(sideBars[1]);
    }

    public void AddTrack(string trackName)
    {
        var btn = new TrackButton(
            menu: menu,
            owner: this,
            displayText: trackName,
            signalText: "CLICK",
            pos: new Vector2(0f, nextButtonPos),
            size: new Vector2(size.x, ItemHeight)
        );
        nextButtonPos += btn.size.y;
        subObjects.Add(btn);
        itemCount++;
    }

    public override void Update()
    {
        base.Update();

        if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
        {
            Debug.Log(menu.mouseScrollWheelMovement);
            scrollInt -= menu.mouseScrollWheelMovement * 4;
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
        sideBars[1].SetPosition(ScreenPos + Vector2.right * (size.x - 1f));
    }
}

class PlaylistConfigMenu : PositionedMenuObject
{
    private readonly TrackList availableTracksUi;
    private readonly TrackList activeTracksUi;

    private readonly string[] availableTracks;
    private readonly List<string> activeTracks;

    public PlaylistConfigMenu(
        PlaylistConfigDialog menu, MenuObject owner,
        Vector2 pos,
        string[] availableTracks,
        List<string> activeTracks
    )
        : base(menu, owner, pos)
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

        // available songs track list
        subObjects.Add(availableTracksUi = new(
            menu: this.menu,
            owner: this,
            pos: new Vector2(463f, 100f),
            size: new Vector2(200f, 400f)
        ));

        subObjects.Add(activeTracksUi = new(
            menu: this.menu,
            owner: this,
            pos: new Vector2(703f, 100f),
            size: new Vector2(200f, 400f)
        ));

        foreach (var trackName in availableTracks)
        {
            if (activeTracks.Contains(trackName))
                activeTracksUi.AddTrack(trackName);
            else
                availableTracksUi.AddTrack(trackName);
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        switch (message)
        {
            case "BACK":
                (menu as PlaylistConfigDialog).RequestClose();
                menu.PlaySound(SoundID.MENU_Switch_Page_Out);
                break;
        }
    }
}