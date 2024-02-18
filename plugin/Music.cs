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
                        x => x.MatchLdfld(typeof(MusicPiece.SubTrack).GetField("piece")),
                        x => x.MatchCallvirt(typeof(MusicPiece).GetMethod("get_IsProcedural")),
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
                self.availableSongs = activeTracks.ToArray();
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
                    self.announceSong = trackName;
                }
            };
        }
    }
}