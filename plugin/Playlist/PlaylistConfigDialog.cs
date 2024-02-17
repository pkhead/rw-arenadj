using System;
using Menu;
using UnityEngine;

namespace ArenaTunes;

class PlaylistConfigDialog : Dialog
{
    private bool closing = false;
    private bool opening = true;
    private float targetAlpha = 1f;
    private float uAlpha = 0f;

    private float lastAlpha = 0f;
    private float currentAlpha = 0f;

    public Menu.Remix.MenuTabWrapper tabWrapper;
    private readonly PlaylistConfigMenu slidingMenu;

    public PlaylistConfigDialog(ProcessManager manager) : base(manager)
    {
        slidingMenu = new PlaylistConfigMenu(this, pages[0], new Vector2(0f, manager.rainWorld.options.ScreenSize.y + 100f));
        pages[0].subObjects.Add(slidingMenu);
    }

    public override void Update()
    {
        base.Update();
        lastAlpha = currentAlpha;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 0.2f);

        if (closing && Math.Abs(currentAlpha - targetAlpha) < 0.09f)
        {
            manager.StopSideProcess(this);
            closing = false;
        }
    }

    public void RequestClose()
    {
        if (closing) return;
        closing = true;
        targetAlpha = 0f;
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (opening || closing)
        {
            uAlpha = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastAlpha, currentAlpha, timeStacker)), 1.5f);
            darkSprite.alpha = uAlpha * 0.92f;
        }

        slidingMenu.pos.y = Mathf.Lerp(
            manager.rainWorld.options.ScreenSize.y + 100f, 0f,
            Math.Min(uAlpha, 1f)
        );
    }

    public override void ShutDownProcess()
    {
        base.ShutDownProcess();
        darkSprite.RemoveFromContainer();
    }
}