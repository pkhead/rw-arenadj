using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;

// allow access to private members of Rain World code
#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ArenaTunes
{
    [BepInPlugin(MOD_ID, "Custom Arena DJ", VERSION)]
    public partial class ModMain : BaseUnityPlugin
    {
        public const string MOD_ID = "pkhead.arenatunes";
        public const string AUTHOR = "pkhead";
        public const string VERSION = "1.2.0";

        private bool isInit = false;
        private bool postInit = false;

        public BepInEx.Logging.ManualLogSource logger;
        public Options options;

        public ModMain() { }

        public void OnEnable()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("Arena Tunes");

            On.RainWorld.OnModsInit += (On.RainWorld.orig_OnModsInit orig, RainWorld self) =>
            {
                orig(self);

                try
                {
                    if (isInit) return;
                    isInit = true;

                    MusicHooks();
                    PlaylistHooks();
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };

            On.RainWorld.PostModsInit += (On.RainWorld.orig_PostModsInit orig, RainWorld self) =>
            {
                orig(self);
                if (postInit) return;
                postInit = true;

                try
                {
                    options = new Options(this);
                    MachineConnector.SetRegisteredOI(MOD_ID, options);
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }
    }
}