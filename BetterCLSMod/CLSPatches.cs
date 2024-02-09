using System;
using System.Diagnostics;
using System.IO;
using BetterCLSUnity;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debugger = DG.Tweening.Core.Debugger;

namespace BetterCLSMod
{
    [HarmonyPatch]
    public class CLSPatches
    {
        private static bool _isFirstLoading;
        
        [HarmonyPatch(typeof(scrController), "Awake")]
        [HarmonyPostfix]
        public static void InitBetterCLSScenes()
        {
            if (_isFirstLoading) return;
            _isFirstLoading = true;
            

            SongListController.DefaultSavePath = Main.ModEntry.Path;
            SongListController.DefaultSteamPath =
                Path.Combine(Main.ModEntry.Path.Split(new[] { "common" }, StringSplitOptions.None)[0], "workshop", "content", "977950");
            

            SongListController.OnESC = () =>
            {
                /*
                var gameObject = UnityEngine.Object.Instantiate<GameObject>(ADOBase.gc.canvasPrefab);
                var pauseMenu = gameObject.transform.Find("RDPauseMenu").GetComponent<PauseMenu>();
                pauseMenu.Show();*/
                SongListController.instance.Preview.ClearAllAudios();
                SceneManager.LoadScene("scnLevelSelect");
            };
    
            SongListController.OnLoading = () =>
            {
                SongListController.instance.Preview.ClearAllAudios();
                var id = new DirectoryInfo(SongListController.instance.Current.DetailData.Path).Name;

                if (GCS.speedTrialMode)
                {
                    //SceneManager.LoadScene("scnGame", LoadSceneMode.Single);
                    GCS.sceneToLoad = "scnGame";
                    GCS.customLevelPaths = new string[1];
                    GCS.customLevelPaths[0] =
                        Path.Combine(SongListController.instance.Current.DetailData.Path, "main.adofai");
                    GCS.checkpointNum = 0;
                    GCS.customLevelId = id;

                    GCS.nextSpeedRun = 1;
                    //SceneManager.LoadScene("scnGame", LoadSceneMode.Single);
                }
                else
                {
                    var skipToMain = Persistence.GetCustomWorldAttempts(SongListController.instance.Current.DetailData.SongID) > 0;
                    GCS.sceneToLoad = "scnGame";
                    GCS.customLevelPaths = scnGame.GetWorldPaths(Path.Combine(SongListController.instance.Current.DetailData.Path, "main.adofai"), false, true);
                    GCS.customLevelIndex = (skipToMain ? (GCS.customLevelPaths.Length - 1) : 0);
                    GCS.customLevelId = id;
                    GCS.checkpointNum = 0;
                }
                
                DOTween.KillAll(false);
                SceneManager.LoadScene("scnGame", LoadSceneMode.Single);
            };
            
            BetterCLSAssets.LoadAssetsAndScenes();
        }
        
        
        [HarmonyPatch(typeof(scrController), "PortalTravelAction")]
        [HarmonyPrefix]
        public static bool OnLandCLSPortal(int destination)
        {
            if(destination != -5) return true;

            scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight, () =>
            {
                SceneManager.LoadScene("LevelSelect");
            });
            return false;
        }   
        
        [HarmonyPatch(typeof(Debugger), "LogSafeModeCapturedError")]
        [HarmonyPrefix]
        public static bool DisableLog()
        {
            return false;
        }
        
        [HarmonyPatch(typeof(Debugger), "Log")]
        [HarmonyPrefix]
        public static bool DisableLog2()
        {
            return false;
        }
        
        [HarmonyPatch(typeof(scnEditor), "QuitToMenu")]
        [HarmonyPrefix]
        public static bool CancelGoToCLSInEditor()
        {
            if (GCS.customLevelPaths == null) return true;
            
            ADOBase.audioManager.StopLoadingMP3File();
            scrController.deaths = 0;
            GCS.currentSpeedTrial = 1f;
            Time.timeScale = 1;
            AudioListener.pause = false;
            scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight, () =>
            {
                SceneManager.LoadScene("LevelSelect");
            });
            
            return false;
        }
        
        
        [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
        [HarmonyPrefix]
        public static bool CancelGoToCLS(ref bool ___exitingToMainMenu)
        {
            if (GCS.customLevelPaths == null) return true;
            
            ADOBase.audioManager.StopLoadingMP3File();
            ___exitingToMainMenu = true;
            scrController.deaths = 0;
            GCS.currentSpeedTrial = 1f;
            Time.timeScale = 1;
            AudioListener.pause = false;
            scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight, () =>
            {
                SceneManager.LoadScene("LevelSelect");
            });
            
            return false;
        }
        
        
    }
}