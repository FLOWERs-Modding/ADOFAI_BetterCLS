using System.IO;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace BetterCLSMod
{
    public class BetterCLSAssets
    {
        public static Assembly Assembly;
        public static AssetBundle Scenes;
        public static AssetBundle Assets;
        private static string _basePath;

        public static void Init(UnityModManager.ModEntry modEntry)
        {
            _basePath = modEntry.Path;
        }
        
        public static void LoadAssembly()
        {
            Assembly = Assembly.LoadFrom(Path.Combine(_basePath, "BetterCLSUnity.dll"));
            Main.Log("Unity Assembly loaded successfully.");
        }

        public static void LoadAssetsAndScenes()
        {
            Scenes = AssetBundle.LoadFromFile(Path.Combine(_basePath, "bettercls_scenes"));
            Assets = AssetBundle.LoadFromFile(Path.Combine(_basePath, "bettercls_assets"));
            Main.Log("Asset loaded successfully.");
        }
        
    }
}