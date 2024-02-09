using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace BetterCLSMod
{
    public class Main
    {
        public static Harmony ModHarmony;
        public static UnityModManager.ModEntry ModEntry;


        public static void Log(string str)
        {
            ModEntry.Logger.Log(str);
        }
        
        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnToggle = OnToggle;
            ModHarmony = new Harmony(modEntry.Info.Id);
            ModEntry = modEntry;
            
            BetterCLSAssets.Init(modEntry);
            BetterCLSAssets.LoadAssembly();
        }


        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool toggle)
        {
            if (toggle)
            {
                if(SceneManager.GetActiveScene().name != "scnSplash")
                    CLSPatches.InitBetterCLSScenes();
                ModHarmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                if(SceneManager.GetActiveScene().name != "LevelSelect")
                    SceneManager.LoadScene("scnLevelSelect");
                ModHarmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }
    }
}