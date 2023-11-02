using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FastScrollSongSelect.Patches
{
    internal class FastScrollSongSelectPatch
    {
        /// <summary>
        /// The max amount of time between hits to enable fast scrolling
        /// </summary>
        static int inputGracePeriod = Plugin.Instance.ConfigJumpInputTime.Value;

        /// <summary>
        /// The amount of time in milliseconds to wait between each jump
        /// </summary>
        static int jumpDelayTime = Plugin.Instance.ConfigJumpDelayTime.Value;

        static float previousJumpTime = 0;

        /// <summary>
        /// The number of songs to move when fast scrolling
        /// </summary>
        static int numSongsJump = Plugin.Instance.ConfigSongJumpCount.Value;

        /// <summary>
        /// The time the first button was pressed in milliseconds
        /// </summary>
        static float timePressed = 0;
        static bool isBuffered = false;
        static ControllerManager.Dir previousDir = ControllerManager.Dir.None;

        static bool enableFastScrollingMouseWheel = true;

        static GameObject additionalFilterMenu = null;

        [HarmonyPatch(typeof(SongSelectManager))]
        [HarmonyPatch(nameof(SongSelectManager.UpdateSongSelect))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPrefix]
        public static bool SongSelectManager_UpdateSongSelect_Prefix(SongSelectManager __instance)
        {
            ControllerManager.Dir dir = TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetDirectionButton(ControllerManager.ControllerPlayerNo.Player1, ControllerManager.Prio.None, false);

            if (dir == ControllerManager.Dir.None && enableFastScrollingMouseWheel)
            {
                dir = TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetDirectionMouseScrollWheel();
                isBuffered = false;
            }

            bool isFastScroll = false;
            if (dir == ControllerManager.Dir.Up || dir == ControllerManager.Dir.Down)
            {
                if (!isBuffered && dir == previousDir)
                {
                    isFastScroll = true;
                    timePressed = (Time.time * 1000);
                }
            }

            if (dir == ControllerManager.Dir.None)
            {
                isBuffered = false;
            }

            if (dir != ControllerManager.Dir.None)
            {
                timePressed = (Time.time * 1000);
                previousDir = dir;
                isBuffered = true;
            }

            if (timePressed + (float)inputGracePeriod < Time.time * 1000)
            {
                timePressed = 0;
                previousDir = ControllerManager.Dir.None;
                isBuffered = false;
            }

            bool isAdditionalFilterMenuEnabled = false;
            if (additionalFilterMenu == null)
            {
                additionalFilterMenu = GameObject.Find("AdditionalFilterMenuParent");
            }
            if (additionalFilterMenu != null)
            {
                if (additionalFilterMenu.activeInHierarchy)
                {
                    isAdditionalFilterMenuEnabled = true;
                }
            }

            if (isFastScroll && !isAdditionalFilterMenuEnabled)
            {
                if (Time.time * 1000 >= previousJumpTime + jumpDelayTime)
                {
                    int songJump = dir == ControllerManager.Dir.Up ? numSongsJump * -1 : numSongsJump;
                    int songIndex = (__instance.SelectedSongIndex + songJump + __instance.SongList.Count) % __instance.SongList.Count;

                    __instance.SelectedSongIndex = songIndex;
                    __instance.UpdateKanbanSurface(false);
                    __instance.isKanbanMoving = false;
                    __instance.kanbanMoveCount = 0;
                    __instance.PlayKanbanMoveAnim(SongSelectManager.KanbanMoveType.MoveEnded);
                    __instance.UpdateSortBarSurface(true);
                    TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySoundManager.CommonSePlay("fast", false, false);
                    __instance.isSongLoadRequested = true;
                    __instance.songPlayer.Stop(true);
                    __instance.isSongPlaying = false;
                    __instance.oniUraChangeTimeCount = 0f;
                    __instance.kanbans[0].DiffCourseChangeAnim.Play("ChangeMania", 0, 1f);
                    __instance.UpdateScoreDisplay();
                }

                previousJumpTime = (Time.time * 1000);

                return false;
            }

            return Time.time * 1000 >= previousJumpTime + jumpDelayTime;
        }
    }
}
