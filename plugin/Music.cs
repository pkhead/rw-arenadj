using BepInEx;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using Music;
using UnityEngine;
using System.IO;
using RWCustom;

namespace ArenaTunes
{
    public partial class ModMain : BaseUnityPlugin
    {
        private const string CUSTOM_PREFIX = "arenacustom-";

        private void MusicHooks()
        {
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
                        logger.LogDebug("delegate called");

                        if (!GetCustomSong(self.trackName, out string fileName))
                            return false;

                        Debug.Log("loading custom arena track!");

                        // %USERPROFILE%\Documents on Windows
                        // home directory on Unix/Linux
                        string folderPath = Options.FolderPath.Value;
                        string filePath = Path.Combine(folderPath, fileName);

                        if (!File.Exists(filePath))
                            return false;

                        self.source.clip = AssetManager.SafeWWWAudioClip("file://" + filePath, false, true, AudioType.OGGVORBIS);
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
                    logger.LogDebug(il.ToString());
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }

        private bool GetCustomSong(string trackName, out string fileName)
        {
            if (trackName.Substring(0, CUSTOM_PREFIX.Length) == CUSTOM_PREFIX)
            {
                fileName = trackName.Substring(CUSTOM_PREFIX.Length);
                return true;
            }

            fileName = "";
            return false;
        }
    }
}