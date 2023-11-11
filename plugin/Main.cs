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
    [BepInPlugin(MOD_ID, "Arena Mix", VERSION)]
    public partial class ModMain : BaseUnityPlugin
    {
        public const string MOD_ID = "pkhead.arenatunes";
        public const string AUTHOR = "pkhead";
        public const string VERSION = "1.0.0";

        private bool isInit = false;

        public BepInEx.Logging.ManualLogSource logger;

        public ModMain() { }

        public void OnEnable()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("Arena Tunes");

            logger.LogDebug("before");
            MusicHooks();
            logger.LogDebug("help");

            On.RainWorld.OnModsInit += (On.RainWorld.orig_OnModsInit orig, RainWorld self) =>
            {
                orig(self);
                if (isInit) return;
                isInit = true;

                try
                {
                    logger.LogDebug("setting registed OI");
                    MachineConnector.SetRegisteredOI(MOD_ID, Options.instance);
                    logger.LogDebug("SetRegisteredOI");
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }
    }
}