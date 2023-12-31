using BepInEx;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using Music;
using UnityEngine;
using System.IO;
using RWCustom;
using System.Collections.Generic;

namespace ArenaTunes
{
    public partial class ModMain : BaseUnityPlugin
    {
        struct TrackInfo
        {
            public string fileName;
            public AudioType audioType;
        };

        // this prefix is used to differentiate custom songs
        // from vanilla songs in the playlist
        private const string CUSTOM_PREFIX = "[CUSTOM] ";
        private Dictionary<string, TrackInfo> trackInfoDict = new();

        private void MusicHooks()
        {
            // custom song loading
            IL.Music.MusicPiece.SubTrack.Update += (il) =>
            {
                try
                {
                    var cursor = new ILCursor(il);
                    ILLabel branch = null;
                    ILLabel exitLabel = cursor.DefineLabel();

                    // if (this.piece.isProcedural)
                    cursor.GotoNext(
                        x => x.MatchLdarg(0),
                        x => x.MatchLdfld(typeof(Music.MusicPiece.SubTrack).GetField("piece")),
                        x => x.MatchCallvirt(typeof(Music.MusicPiece).GetMethod("get_IsProcedural")),
                        x => x.MatchBrfalse(out branch)
                    );

                    if (branch == null)
                        throw new Exception("IL code match 1 failed!");

                    cursor.GotoLabel(branch);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate((MusicPiece.SubTrack self) =>
                    {
                        if (!GetCustomSong(self.trackName, out TrackInfo trackInfo))
                            return false;

                        logger.LogDebug("loading custom arena track!");

                        string filePath = Path.Combine(Options.FolderPath.Value, trackInfo.fileName);
                        
                        if (!File.Exists(filePath))
                        {
                            logger.LogDebug("file did not exist");
                            return false;
                        }

                        self.source.clip = AssetManager.SafeWWWAudioClip("file://" + filePath, false, true, trackInfo.audioType);
                        self.isStreamed = true;

                        Debug.Log("loaded custom arena track!");

                        return true;
                    });
                    cursor.Emit(OpCodes.Brtrue, exitLabel);

                    // find exit label
                    ILLabel exitBranch = null;

                    cursor.GotoNext(
                        x => x.MatchLdarg(0),
                        x => x.MatchLdcI4(1),
                        x => x.MatchStfld(typeof(MusicPiece.SubTrack).GetField("isStreamed")),
                        x => x.MatchBr(out exitBranch)
                    );

                    if (exitBranch == null)
                        throw new Exception("IL code match 2 failed!");

                    cursor.GotoLabel(exitBranch);
                    cursor.MarkLabel(exitLabel);

                    logger.LogDebug("IL injection success!");
                }
                catch (Exception e)
                {
                    logger.LogFatal(e);
                }
            };

            // hijack DJ
            On.Music.MultiplayerDJ.ctor += (On.Music.MultiplayerDJ.orig_ctor orig, MultiplayerDJ self, MusicPlayer player) =>
            {
                orig(self, player);
                trackInfoDict.Clear();

                // get custom songs
                if (!Directory.Exists(Options.FolderPath.Value))
                    return;
                    
                List<string> customSongs = new();

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
                    
                    string nameInList = CUSTOM_PREFIX + Path.GetFileNameWithoutExtension(filePath);
                    trackInfoDict.Add(nameInList, trackInfo);
                    customSongs.Add(nameInList);
                }

                // log all the found sounds
                foreach (string filePath in customSongs)
                {
                    logger.LogDebug("Found " + filePath);
                }

                if (customSongs.Count > 0)
                {
                    // add to music playlist
                    if (Options.CustomOnly.Value)
                    {
                        // replace list if Custom Only
                        self.availableSongs = customSongs.ToArray();
                    }
                    else
                    {
                        // append to list
                        var newList = new string[self.availableSongs.Length + customSongs.Count];
                        self.availableSongs.CopyTo(newList, 0);
                        customSongs.CopyTo(newList, self.availableSongs.Length);

                        self.availableSongs = newList;
                    }
                }
            };

            // display author name if it is a custom song
            On.Music.MultiplayerDJ.PlayNext += (On.Music.MultiplayerDJ.orig_PlayNext orig, Music.MultiplayerDJ self, float fadeInTime) =>
            {
                if (!Options.AuthorName.Value)
                {
                    orig(self, fadeInTime);
                    return;
                }
                
                if (self.playList.Count < 1)
                {
                    self.ShufflePlaylist();
                }

                string trackName = self.playList[0];
                orig(self, fadeInTime);

                // if this is a custom song,
                // show all text after the custom prefix
                if (GetCustomSong(trackName, out _))
                {
                    self.announceSong = trackName.Substring(CUSTOM_PREFIX.Length);
                }
            };
        }

        private bool GetCustomSong(string trackName, out TrackInfo trackInfo)
        {
            // a track is a custom track if it starts with the CUSTOM_PREFIX
            if (trackName.Substring(0, CUSTOM_PREFIX.Length) == CUSTOM_PREFIX)
            {
                trackInfo = trackInfoDict[trackName];
                return true;
            }

            trackInfo = new();
            return false;
        }
    }
}