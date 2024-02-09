using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterCLSUnity
{
    public class AudioPlayer : MonoBehaviour
    {
        public AudioSource Player;
        
        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
        private int _previewStart;
        private int _previewEnd;


        private void Update()
        {
            if (Player.isPlaying && Player.time > _previewEnd)
            {
                Player.time = _previewStart;
            }
        }

        public void Play(string key, int start, int end)
        {
            _previewEnd = end;
            _previewStart = start;

            if (_audioClips.TryGetValue(key, out var v))
            {
                Player.clip = v;
                Player.Play();
                Player.time = start;
            }
        }


        public void ClearAllAudios()
        {
            Player.clip = null;
            foreach (var a in _audioClips.Values)
            {
                a.UnloadAudioData();
                DestroyImmediate(a, true);
                Resources.UnloadAsset(a);
            }

            _audioClips.Clear();
            GC.Collect();
        }
        
        public IEnumerator LoadSong(string path, string key)
        {
            if (!_audioClips.TryGetValue(key, out var v))
            {
                if (_audioClips.Count > 10)
                {
                    ClearAllAudios();
                    //Debug.Log("reload");
                }
            }
            else
            {
                yield break;
            }
            
            var ext = Path.GetExtension(path).ToLower().Replace(".", "");
            var audioType = ext=="ogg" ? AudioType.OGGVORBIS : (ext=="wav" ? AudioType.WAV : (ext=="aif" ? AudioType.AIFF : (ext=="aiff" ? AudioType.AIFF : AudioType.UNKNOWN)));
            using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(Utils.ToFileUri(path), audioType))
            {
                ((DownloadHandlerAudioClip)webRequest.downloadHandler).streamAudio = true;
                yield return webRequest.SendWebRequest();
                
                var a = DownloadHandlerAudioClip.GetContent(webRequest);
                if(a != null)
                    _audioClips[key] = a;
                
                yield break;
            }
        }
        
        

        public void Stop()
        {
            Player.Stop();
        }
    }
}