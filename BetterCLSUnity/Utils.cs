using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterCLSUnity
{
    public class Utils
    {
        public static int GetIndex(int index, int size)
        {
            if (size <= index)
                return index - size;

            if (index < 0)
                return size + index;

            return index;
        }

        public static bool IsStart(Tween t)
        {
            if (t != null)
                if (t.active)
                    return true;
            return false;
        }

        public static string RemoveHTMLTag(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        public static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }


        public static string FloatToDiffString(float diff)
        {
            if (diff == 0)
                return "???";
            
            var t = diff - (int)diff;
            if (t >= 0.75 && t < 1)
                return (((int)diff)+1).ToString();
            
            if(t >= 0.25 && t < 0.75)
                return ((int)diff)+(diff >= 18 && diff < 20? "+":"");
            
            if (t >= 0 && t < 0.25)
                return ((int)diff).ToString();

            return "???";
        }
        
        public static int CalculateDurationOGG(string filePath)
        {
            try
            {
                var t = File.ReadAllBytes(filePath);
                var rate = -1;
                var length = -1L;

                for (var i = 0; i < 255; i++)
                {
                    if (
                        t[i] == 'v'
                        && t[i + 1] == 'o'
                        && t[i + 2] == 'r'
                        && t[i + 3] == 'b'
                        && t[i + 4] == 'i'
                        && t[i + 5] == 's'
                    )
                    {
                        var byteArray = new byte[] { t[i + 11], t[i + 12], t[i + 13], t[i + 14] };
                        rate = BitConverter.ToInt32(byteArray, 0);
                        break;
                    }
                }

                for (var i = t.Length-14; i > t.Length-65307; i--)
                {
                    if (t[i] == 'O' && t[i+1] == 'g' && t[i+2] == 'g' && t[i+3] == 'S')
                    {
                        var byteArray = new byte[]
                            { t[i+6],t[i+7], t[i+8], t[i+9], t[i+10], t[i+11], t[i+12], t[i+13] };
                        length = BitConverter.ToInt64(byteArray, 0);
                        break;
                    }
                }
                
                
                return (int)(length / rate);
                
            }
            catch
            {
                Debug.Log("skip, "+filePath);
                return -1;
            }
        }


        public static string ToFileUri(string path)
        {
            var uri = new Uri(path);
            return !uri.IsFile ? null : uri.ToString();
        }
        
        
        public static string GetMd5Hash(string input)
        {
            var array = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            var stringBuilder = new StringBuilder();
            foreach (var t in array)
                stringBuilder.Append(t.ToString("x2"));

            return stringBuilder.ToString();
        }


        public static int GetRankIndex(SongDetailData d)
        {
            var pro = d.MaxProgress;
            var acc = d.MaxAccuracy;

            if (pro >= 100)
            {
                if (d.IsPerfect)
                    return 0;

                var f = acc / (!SongListController.UseXAccuracy ? (100 + (d.TileCount / 100f)) : 100);
                if (0.95 <= f && f < 1)
                    return 1;

                if (0.9 <= f && f < 95)
                    return 2;

                return 3;
            }
            else
            {
                if (pro > 90)
                    return 4;
            }
            
            return -1;
        }


        public static bool IsAlphabet(int c)
        {
            return (65 <= c && c <= 90) || (97 <= c && c <= 122);
        }
        



    }
}