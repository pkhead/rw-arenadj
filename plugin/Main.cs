using System;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using System.IO;
using System.Linq;

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

        public BepInEx.Logging.ManualLogSource logger;
        public Options options;

        private string[] availableTracks = Array.Empty<string>();
        private readonly List<string> activeTracks = new();

        struct TrackInfo
        {
            public string fileName;
            public AudioType audioType;
        };
        private Dictionary<string, TrackInfo> trackInfoDict = new();

        public ModMain() { }

        public void OnEnable()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("Arena Tunes");

            On.RainWorld.OnModsInit += (On.RainWorld.orig_OnModsInit orig, RainWorld self) =>
            {
                orig(self);

                try
                {
                    options = new Options(this);
                    MachineConnector.SetRegisteredOI(MOD_ID, options);

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
                
                try
                {
                    availableTracks = Array.Empty<string>();
                    activeTracks.Clear();
                    
                    logger.LogDebug("Scan tracks");
                    ScanTracks();
                } catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }

        private void ScanTracks()
        {
            List<string> tracks;

            var mpMusicPath = AssetManager.ResolveFilePath(Path.Combine("Music", "MPMusic.txt"));
            if (File.Exists(mpMusicPath))
            {
                logger.LogInfo($"Reading {mpMusicPath}...");

                tracks = File.ReadLines(mpMusicPath).ToList();
                activeTracks.Add(tracks[0]);

                logger.LogInfo("Successfully loaded MPMusic.txt");
            }
            else
            {
                logger.LogError(mpMusicPath + " does not exist");
                return;
            }
            
            logger.LogInfo($"Reading {Options.FolderPath.Value}...");
            foreach (var filePath in Directory.EnumerateFiles(Options.FolderPath.Value))
            {
                TrackInfo trackInfo = new()
                {
                    fileName = Path.GetFileName(filePath)
                };
                
                switch (Path.GetExtension(filePath))
                {
                    case ".ogg":
                        trackInfo.audioType = AudioType.OGGVORBIS;
                        break;

                    case ".mp3": case ".mpeg":
                        trackInfo.audioType = AudioType.MPEG;
                        break;

                    case ".wav":
                        trackInfo.audioType = AudioType.WAV;
                        break;

                    default:
                        continue; // file extension unknown, skip this file
                }
                
                string nameInList = Path.GetFileNameWithoutExtension(filePath);
                trackInfoDict.Add(nameInList, trackInfo);
                tracks.Add(nameInList);

                logger.LogInfo("Found " + nameInList);
            }

            availableTracks = tracks.ToArray();
        }
    }
}