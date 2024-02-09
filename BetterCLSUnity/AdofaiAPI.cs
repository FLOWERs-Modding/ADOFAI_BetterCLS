using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BetterCLSUnity
{
    public class AdofaiAPI
    {
        private static readonly BindingFlags _all = BindingFlags.Public 
                                                  | BindingFlags.NonPublic
                                                  | BindingFlags.Instance
                                                  | BindingFlags.Static
                                                  | BindingFlags.GetField
                                                  | BindingFlags.SetField
                                                  | BindingFlags.GetProperty
                                                  | BindingFlags.SetProperty;

        private static Assembly _adofaiAssembly;
        private static Assembly _steamAssembly;

        public static void Init()
        {
            _adofaiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            
            _steamAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "com.rlabrecque.steamworks.net");
            
        }
        
        public static int GetCustomWorldAttempts(string hash)
        {
            if (_adofaiAssembly == null)
                return 0;
            return (int)_adofaiAssembly.GetType("Persistence")?.GetMethod("GetCustomWorldAttempts", _all)?.Invoke(null,new object[]{hash})!;
        }

        public static float GetCustomWorldAccuracy(string hash)
        {
            if (_adofaiAssembly == null)
                return 0;
            return (float)_adofaiAssembly.GetType("Persistence")?.GetMethod("GetCustomWorldAccuracy", _all)?.Invoke(null,new object[]{hash})!;
        }
        
        public static float GetCustomWorldXAccuracy(string hash)
        {
            if (_adofaiAssembly == null)
                return 0;
            return (float)_adofaiAssembly.GetType("Persistence")?.GetMethod("GetCustomWorldXAccuracy", _all)?.Invoke(null,new object[]{hash})!;
        }
        
        public static float GetCustomWorldCompletion(string hash)
        {
            if (_adofaiAssembly == null)
                return 0;
            return (float)_adofaiAssembly.GetType("Persistence")?.GetMethod("GetCustomWorldCompletion", _all)?.Invoke(null,new object[]{hash})!;
        }
        
        public static bool GetCustomWorldIsHighestPossibleAcc(string hash)
        {
            if (_adofaiAssembly == null)
                return false;
            return (bool)_adofaiAssembly.GetType("Persistence")?.GetMethod("GetCustomWorldIsHighestPossibleAcc", _all)?.Invoke(null,new object[]{hash})!;
        }
        
        public static bool ShowXAccuracy()
        {
            if (_adofaiAssembly == null)
                return false;
            return (bool)_adofaiAssembly.GetType("Persistence")?.GetProperty("showXAccuracy", _all)?.GetValue(null)!;
        }

        public static void OpenWorkshop()
        {
            if (_adofaiAssembly == null)
                return;
            _adofaiAssembly.GetType("SteamWorkshop")?.GetMethod("OpenWorkshop", _all)?.Invoke(null,null);
        }

        public static object GetADObaseData()
        {
            if (_adofaiAssembly == null)
                return null;
            return _adofaiAssembly.GetType("ADOBase")?.GetProperty("gc", _all)?.GetValue(null)!;
        }
        
        public static bool HasSubscribedToFeatured(ulong levelid)
        {
            if (_adofaiAssembly == null)
                return false;
            return (bool)_adofaiAssembly.GetType("Persistence")?.GetMethod("HasSubscribedToFeatured", _all)?.Invoke(null,new object[]{levelid})!;
        }
        
        public static void SetSubscribedToFeatured(ulong levelid, bool subscribed)
        {
            if (_adofaiAssembly == null)
                return;
            _adofaiAssembly.GetType("Persistence")?.GetMethod("SetSubscribedToFeatured", _all)?.Invoke(null,new object[]{levelid, subscribed});
        }
        
        public static void Subscribe(ulong levelId)
        {
            if (_adofaiAssembly == null)
                return;
            var t = _steamAssembly.GetType("Steamworks.PublishedFileId_t");
            var p = Activator.CreateInstance(t, levelId);
            _adofaiAssembly.GetType("SteamWorkshop")?.GetMethod("Subscribe", _all)?.Invoke(null,new []{p});
        }
        
        
        public static void SetSpeedTrialMode(bool toggle)
        {
            if (_adofaiAssembly == null)
                return;
            _adofaiAssembly.GetType("GCS")?.GetField("speedTrialMode",_all)?.SetValue(null,toggle);
        }
        
        public static void SetNoFail(bool toggle)
        {
            if (_adofaiAssembly == null)
                return;
            _adofaiAssembly.GetType("GCS")?.GetField("useNoFail",_all)?.SetValue(null,toggle);
        }

        public static void SubscribeToFeatured()
        {
            var featureds = GetFeaturedLevels(); 
            if (featureds == null) return;
            
            foreach (var num2 in featureds)
            {
                var num = (ulong)num2;
                if (!HasSubscribedToFeatured(num))
                {
                    Subscribe(num);
                    SetSubscribedToFeatured(num, true);
                }
            }
        }


        public static void Unsubscribe(ulong levelId, bool isFeatured)
        {
            if (_adofaiAssembly == null)
                return;
            var t = _steamAssembly.GetType("Steamworks.PublishedFileId_t");
            var p = Activator.CreateInstance(t, levelId);
            if(isFeatured)
                SetSubscribedToFeatured(levelId, false);
            _adofaiAssembly.GetType("SteamWorkshop")?.GetMethod("Unsubscribe", _all)?.Invoke(null,new []{p});
        }
        

        public static uint[] GetFeaturedLevels()
        {
            if (_adofaiAssembly == null)
                return null;
            
            return _adofaiAssembly?.GetType("GCNS").GetField("FeaturedLevelsIDs", _all)?.GetValue(null) as uint[];
        }

        public static void PlaySfx(string name)
        {
            if (_adofaiAssembly == null)
                return;

            try
            {
                var enumType = _adofaiAssembly.GetType("SfxSound");
                var sfxEnum = Enum.Parse(enumType, name);
                var mixerType = _adofaiAssembly.GetType("MixerGroup");
                var mixerEnum = Enum.Parse(mixerType, "Fallback");

                var sfxType = _adofaiAssembly.GetType("scrSfx");
                var instance = sfxType.GetProperty("instance", _all)?.GetValue(null);

                var method = sfxType.GetMethod("PlaySfx", _all, null,
                        CallingConventions.Any,
                        new[] { enumType, mixerType, typeof(float), typeof(float), typeof(float) },
                        null);
                
                method?.Invoke(instance, new[] { sfxEnum , mixerEnum, 1,1,0});
                

            }
            catch(Exception e)
            {
                // ignored
            }
        }
        

    }
}