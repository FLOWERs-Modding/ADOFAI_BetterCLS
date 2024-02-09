using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using Object = UnityEngine.Object;

namespace BetterCLSUnity
{
    public class SongListController : MonoBehaviour
    {
        private List<string> Categories = new List<string>() { "all", "favorite", "featured", "nonfeatured" };
        private Tween[] ScrollTween = new Tween[2];
        private Tween songLoadWaiting;
        private bool OverSongCount;
        private Coroutine _audioLoadingCoroutine;
        private float _pressTime;
        private bool _pressEnd = true;
        
        private Dictionary<string, List<SongDetailData>>
            SongByCategory = new();

        public SongCardInfo Current;    
        public int SelectIndex;
        public int VirtualIndex;
        public SongCardInfo[] SongList;
        //public string[] AllSongs;
        public static int CurrentCategory;
        public bool Loading;


        [Header("Song Level Info")]
        public Text LevelText;
        public Text AdofaiGGLevelText;
        public Text Progress;
        public GameObject LevelDiffGameObject;
        public Text Description;
        public Text Attempt;
        
        [Header("Song Card Info")]
        public Text Title;
        public Text Author;
        public Text Maker;
        public Text Time;
        public Text BPM;
        public Image NoFail;
        public Image NeoCosmos;
        public Image SpeedTrialRabbit;
        public Image RankImage;

        [Header("Category")]
        public Text CategoryText;
        public Text NextCategoryText;
        public Text PrevCategoryText;
        
        [Header("Background")]
        public Image PreviewImage;
        public Image PreviewImageColor;
        public Image BG;
        public Image BlurBG;
        public Sprite BaseBG;

        [Header("Edit Folder")]
        public GameObject FolderParent;
        public GameObject FolderList;
        public GameObject FolderCreate;
        public InputField NameInputField;
        public Transform FolderListContentTransform;
        public GameObject FolderPrefab;
        
        [Header("Other")]
        public List<Sprite> RankSprite;
        public RectTransform AllSongsTransform;
        public AudioPlayer Preview;

        public static string DefaultSteamPath = "D:\\SteamLibrary\\steamapps\\workshop\\content\\977950";
        public static string DefaultSavePath = "D:\\SteamLibrary\\steamapps\\common\\A Dance of Fire and Ice\\Mods\\BetterCLS\\";
        
        public static SongListController instance;
        public static Action OnESC;
        public static Action OnLoading;

        private static string _lastSongName;
        private static bool _toggleDescription;
        private static bool _toogleSpeed;
        private static bool _toggleNoFail;
        
        private Dictionary<string, Dictionary<string,bool>> _customSongs = new ();
        public static bool UseXAccuracy;

        public enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        private void InitCategories()
        {
            var featuredLevels = AdofaiAPI.GetFeaturedLevels();
            SongByCategory["featured"] = new List<SongDetailData>();
            SongByCategory["nonfeatured"] = new List<SongDetailData>();

            foreach (var k in _customSongs.Keys)
            {
                SongByCategory[k] = new List<SongDetailData>();
            }

            SongByCategory["all"] = LoadCustomFiles().OrderBy(a => a.Title).ToList();
            for (var n = 0; n < SongByCategory["all"].Count; n++)
            {
                var s = SongByCategory["all"][n];
                //if (n < 8 || SongByCategory[Category.All].Count-4 < n)
                //{
                s.PreviewImage = Utils.LoadSprite(s.PreviewImagePath);
                s.BG = s.PreviewImage;
                //}
                if (_customSongs["favorite"].TryGetValue(s.SongID, out var v))
                    s.IsFavorite = true;


                ColorUtility.TryParseHtmlString("#" + s.TargetColorString, out s.TargetColor);

                foreach (var k in _customSongs.Keys)
                {
                    if (_customSongs[k].TryGetValue(s.SongID, out var v2))
                        SongByCategory[k].Add(s);
                }

                var thisIsFeatured = false;
                if (featuredLevels != null)
                {
                    foreach (var f in featuredLevels)
                    {
                        if (s.Path.Contains(f.ToString()))
                        {
                            SongByCategory["featured"].Add(s);
                            thisIsFeatured = true;
                            break;
                        }
                    }
                }
                
                if(!thisIsFeatured)
                    SongByCategory["nonfeatured"].Add(s);
            }


            SongByCategory["favorite"] = new List<SongDetailData>();
        }





        //private ConcurrentBag<SongInfoMini> entries = new ConcurrentBag<SongInfoMini>();    
        //private ConcurrentBag<SongInfoMini> asdf = new ConcurrentBag<SongInfoMini>();    
        
        
        
        private List<SongDetailData> LoadCustomFiles()
        {
            var path = DefaultSteamPath;
            var files = Directory.GetDirectories(path);

            //List<SongInfoMini> myList = new List<SongInfoMini>();
            //object lockObject = new object();
            var entries = new ConcurrentBag<SongDetailData>();

            //int fileCounter = 0;

            var start = DateTime.Now.Ticks;

            Parallel.ForEach(files.ToList(), file =>
            {

                var cachedArtist = "";
                var cachedSong = "";
                var cachedMaker = "";
                
                var songInfo = new SongDetailData();
                songInfo.Path = file;
                
                
                using (var sr = File.OpenText(Path.Combine(file, "main.adofai")))
                {
                    var s = String.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        //songInfo.Title = "sanstv";
                        
                        

                        if (s.Contains("\"artist\": \""))
                        {
                            cachedArtist = s.Split("\"artist\": \"")[1].Split("\",")[0].Trim();
                            songInfo.Author =
                                Utils.RemoveHTMLTag(cachedArtist);
                            continue;
                        }


                        if (s.Contains("\"song\": \""))
                        {
                            cachedSong = s.Split("\"song\": \"" )[1].Split("\",")[0].Trim();
                            songInfo.Title =
                                Utils.RemoveHTMLTag(cachedSong).Replace("\\n"," ");
                            continue;
                        }

                        if (s.Contains("\"songFilename\": \""))
                        {
                            songInfo.SongPath = s.Split("\"songFilename\": \"" )[1]
                                .Split("\"")[0].Trim();
                            
                            if(!string.IsNullOrEmpty(songInfo.SongPath))
                                songInfo.SongPath = Path.Combine(file, songInfo.SongPath);
                            continue;
                        }
                        
                        
                        if (s.Contains("\"levelDesc\": \""))
                        {
                            songInfo.Description = s.Split("\"levelDesc\": \"")[1].Split("\",")[0].Trim().Replace("\\n","\n");
                            
                            continue;
                        }

                        if (s.Contains("\"previewIconColor\": \""))
                        {
                            songInfo.TargetColorString =
                                s.Split("\"previewIconColor\": \"")[1].Split("\"")[0]
                                    .Trim();
                            
                            continue;
                        }
                        
                        if (s.Contains("\"artistLinks\": \""))
                        {
                            songInfo.ArtistLink =
                                s.Split("\"artistLinks\": \"")[1].Split("\"")[0]
                                    .Trim();

                            if (songInfo.ArtistLink.Contains(","))
                                songInfo.ArtistLink = songInfo.ArtistLink.Split(",")[0];
                            
                            continue;
                        }
                        
                        if (s.Contains("\"difficulty\": "))
                        {
                            var d =
                                s.Split("\"difficulty\": ")[1].Split(",")[0]
                                    .Trim();
                            float.TryParse(d, out songInfo.Difficulty);
                            songInfo.Difficulty = (songInfo.Difficulty * 1.8f);
                            continue;
                        }
                        
                        if (s.Contains("\"previewSongStart\": "))
                        {
                            var start =
                                s.Split("\"previewSongStart\": ")[1].Split(",")[0]
                                    .Trim();
                            int.TryParse(start, out songInfo.PreviewSongStart);
                            continue;
                        }
                        
                        if (s.Contains("\"previewSongDuration\": "))
                        {
                            var end =
                                s.Split("\"previewSongDuration\": ")[1].Split(",")[0]
                                    .Trim();
                            int.TryParse(end, out var v);
                            songInfo.PreviewSongEnd = songInfo.PreviewSongStart+v;

                            if (songInfo.PreviewSongEnd == 0)
                                songInfo.PreviewSongEnd = 10;
                            
                            continue;
                        }

                        if (s.Contains("\"previewImage\": \""))
                        {
                            songInfo.PreviewImagePath =
                                s.Split("\"previewImage\": \"")[1].Split("\"")[0]
                                    .Trim();
                            
                            if(!string.IsNullOrEmpty(songInfo.PreviewImagePath))
                                songInfo.PreviewImagePath = Path.Combine(file, songInfo.PreviewImagePath);
                            continue;
                        }
                        
                        if (s.Contains("\"author\": \""))
                        {
                            cachedMaker = s.Split("\"author\": \"")[1].Split("\"")[0].Trim();
                            songInfo.Maker = cachedMaker;
                            continue;
                        }
                        
                        if (s.Contains("\"levelTags\": \""))
                        {
                            songInfo.RequireNeocosmos = s.Contains("Neo Cosmos");
                            continue;
                        }
                        
                        if (s.Contains("\"angleData\": ["))
                        {
                            var str = s.Split("\"angleData\": [")[1].Split("], ")[0].Trim();
                            songInfo.TileCount = str.Split(", ").Length;
                            continue;
                        }
                        
                        if (s.Contains("\"pathData\": \""))
                        {
                            var str = s.Split("\"pathData\": \"")[1].Split("\"")[0].Trim();
                            songInfo.TileCount = str.Length;
                            continue;
                        }

                        if (s.Contains("\"bpm\": "))
                        {
                            songInfo.BPM = s.Split("\"bpm\": " )[1].Split(",")[0]
                                .Trim();
                            break;
                        }
                        
                    }
                }

                songInfo.SongID = Utils.GetMd5Hash(cachedMaker + cachedArtist + cachedSong);
                songInfo.MaxAccuracy = (UseXAccuracy? AdofaiAPI.GetCustomWorldXAccuracy(songInfo.SongID):AdofaiAPI.GetCustomWorldAccuracy(songInfo.SongID)) * 100;
                songInfo.MaxProgress = AdofaiAPI.GetCustomWorldCompletion(songInfo.SongID) * 100;
                songInfo.IsPerfect = AdofaiAPI.GetCustomWorldIsHighestPossibleAcc(songInfo.SongID);
                songInfo.Attempt = AdofaiAPI.GetCustomWorldAttempts(songInfo.SongID);
                
                entries.Add(songInfo);
            });

            Debug.Log(entries.Count+" Loaded!");
            Debug.Log(((DateTime.Now.Ticks - start)/10000 )+"ms");


            return entries.ToList();
        }

        private void SearchStartWith()
        {
            for (var n = 97; n < 123; n++)
            {
                var keycode = (KeyCode)n;
                if (Input.GetKeyDown(keycode))
                {
                    var l = (char)n;
                    var u = (char)(n-32);
                    var i = SongByCategory[Categories[CurrentCategory]].FindIndex(s => s.Title[0] == l || s.Title[0] == u);

                    if (i != -1)
                    {
                        if (OverSongCount)
                        {
                            VirtualIndex = i;
                            ResortLayout();
                        }
                        else
                        {
                            SelectIndex = i;
                            if(Current != null)
                                Current.Deselect();
                            Select(SelectIndex);
                        }


                    } 
                }
            }
        }

        public void ToggleFolderUI()
        {
            FolderParent.SetActive(!FolderParent.activeSelf);
            FolderList.SetActive(FolderParent.activeSelf);
            FolderCreate.SetActive(false);

            if (FolderParent.activeSelf)
                RefreshFolderList();    
        }

        private void Awake()
        {
            DOTween.KillAll(false);
            
            AdofaiAPI.Init();
            UseXAccuracy = AdofaiAPI.ShowXAccuracy();
            
            BlurBG.color = _toogleSpeed ? new Color(0.4f,0,0) : Color.black;
            SpeedTrialRabbit.gameObject.SetActive(_toogleSpeed);
            NoFail.gameObject.SetActive(_toggleNoFail);
            
            instance = this;
            SongList = FindObjectsOfType<SongCardInfo>().OrderBy(s => s.name).ToArray();
            Author.gameObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                if(Current != null)
                    Application.OpenURL(Current.DetailData.ArtistLink);
            });
            
            _customSongs["favorite"] = new Dictionary<string, bool>();
            LoadCustomFolder();
            
            //loadCustomFiles();
            
            InitCategories();

            SwitchCategory(MoveDirection.None, true);

            BlurBG.material.SetFloat("_Size", 8);
            BlurBG.material.SetFloat("_Opacity", 0.5f);
            
        }

        private void Select(int index)
        {
            Preview.Stop();
            
            if (SongByCategory[Categories[CurrentCategory]].Count == 0 || index == -1)
            {
                Title.text = "???";
                Author.text = "???";
                BPM.text = "BPM ???";
                Time.text = "Time ???";
                Maker.text = "<i>by</i> ???";
                AdofaiGGLevelText.text = "Lv.???";
                LevelText.text = "Lv.???";
                Attempt.text = "Attempt ???";
                Description.text = "";
                PreviewImage.sprite = null;
                PreviewImageColor.color = Color.black;
                _lastSongName = null;
                BG.sprite = BaseBG;
                RankImage.gameObject.SetActive(false);
                NeoCosmos.gameObject.SetActive(false);
                
                
                songLoadWaiting.Kill();
                Preview.StopAllCoroutines();
                Preview.Stop();
                return;
            }

            var song = SongList[index + 1];
            Current = song;


            if (string.IsNullOrEmpty(song.DetailData.Time))
            {
                if (song.DetailData.SongPath.EndsWith(".ogg"))
                {
                    var sec = TimeSpan.FromSeconds(Utils.CalculateDurationOGG(song.DetailData.SongPath));
                    song.DetailData.Time = sec.ToString(@"mm\:ss");
                }
                else
                {
                    song.DetailData.Time = "???";
                }
            }

            song.Select();
            
            _lastSongName = song.DetailData.Title;
            
            Title.text = song.DetailData.Title;
            Author.text = song.DetailData.Author;
            Attempt.text = "Attempt "+song.DetailData.Attempt;
            BPM.text = "BPM " + song.DetailData.BPM;
            Time.text = "Time " + song.DetailData.Time;
            Maker.text = "<i>by</i> " + song.DetailData.Maker;
            AdofaiGGLevelText.text = "Lv."+Utils.FloatToDiffString(song.DetailData.GGDifficulty);
            LevelText.text = "Lv."+Utils.FloatToDiffString(song.DetailData.Difficulty);
            Description.text = song.DetailData.Description;
            PreviewImage.sprite = song.DetailData.PreviewImage;
            PreviewImageColor.color = song.DetailData.TargetColor;
            BG.sprite = song.DetailData.BG;
            
            NeoCosmos.gameObject.SetActive(song.DetailData.RequireNeocosmos);
            
            RankImage.gameObject.SetActive(song.ClearRank.gameObject.activeSelf);
            RankImage.sprite = song.ClearRank.sprite;
            
            if(song.DetailData.MaxProgress >= 100)
                Progress.text = "Best Accuracy "+song.DetailData.MaxAccuracy.ToString("0.00")+"%";
            else
                Progress.text = "Best Progress "+song.DetailData.MaxProgress.ToString("0.00")+"%";

            var texture = song.DetailData.PreviewImage.texture;
            
            var min = Math.Min(texture.width, texture.height);
            var max = Math.Max(texture.width, texture.height);
            var f = max / (float)min;

            PreviewImage.rectTransform.sizeDelta = texture.width > texture.height ? new Vector2(485 * f, 485) : new Vector2(485 , 485 * f);
            
            //Debug.Log(SongList[index + 1].DetailData.TileCount);
            if (Loading) return;
            if(Utils.IsStart(songLoadWaiting))
                songLoadWaiting.Kill();
            
            songLoadWaiting = DOVirtual.DelayedCall(2, () =>
            {
                if (_audioLoadingCoroutine != null)
                {
                    Preview.StopAllCoroutines();
                    _audioLoadingCoroutine = null;
                }

                _audioLoadingCoroutine = Preview.StartCoroutine(UpdateSong(song.DetailData));
            });

        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_lastSongName))
            {
                var i = SongByCategory[Categories[CurrentCategory]].FindIndex(s => s.Title == _lastSongName);
                if (i != -1)
                {
                    if (OverSongCount)
                    {
                        VirtualIndex = i;
                        ResortLayout();
                    }
                    else
                    {
                        SelectIndex = i;
                        if(Current != null)
                            Current.Deselect();
                        Select(SelectIndex);
                    }
                }
                else
                {
                    Select(SelectIndex);
                }
            }
            else
            {
                Select(SelectIndex);
            }

            
            
            AdofaiAPI.SubscribeToFeatured();

            //var songs = SongByCategory[Categories[CurrentCategory]];
            /*
            var a = Resources.Load<AudioClip>($"{songs[OverSongCount? VirtualIndex:SelectIndex].Path}/Preview");
            if (a != null)
            {
                Preview.clip = a;
                Preview.Play();
            }*/
        }

        private void OnlyFavorite()
        {
            SongByCategory["favorite"].Clear();
            for(var n =0;n<SongByCategory["all"].Count;n++)
            {
                var s = SongByCategory["all"][n];
                if(s.IsFavorite)
                    SongByCategory["favorite"].Add(s);
            }
        }

        private void OnDisable()
        {
            //File.WriteAllLines(Path.Combine(DefaultSavePath,"favorite.dat"), _favoriteSongs);
            SaveCustomFolder();
        }

        public void UpdateShortLayout(List<SongDetailData> songs)
        {
            for (var n = 0; n < 7; n++)
            {
                if (songs.Count > n)
                {

                    SongList[n + 1].Init(songs[n]);
                    SongList[n + 1].gameObject.SetActive(true);
                }
                else
                {
                    SongList[n + 1].gameObject.SetActive(false);
                }
            }
        }

        public void SwitchCategory(MoveDirection direction, bool isAwake = false, bool select = false)
        {
            if (direction == MoveDirection.Right)
            {
                CurrentCategory++;
                if (CurrentCategory >= Categories.Count)
                    CurrentCategory = 0;
            }
            else if (direction == MoveDirection.Left)
            {
                CurrentCategory--;
                if (CurrentCategory < 0)
                    CurrentCategory = Categories.Count - 1;
            }

            var c = Categories[CurrentCategory];
            if (c == "favorite")
                OnlyFavorite();
            
            var songs = SongByCategory[c];
            CategoryText.text = c.ToString().ToUpper();
            PrevCategoryText.text = Categories[Utils.GetIndex(CurrentCategory+1, Categories.Count)].ToString().ToUpper();
            NextCategoryText.text = Categories[Utils.GetIndex(CurrentCategory-1, Categories.Count)].ToString().ToUpper();
            
            if (songs.Count > 7)
            {
                OverSongCount = true;
                for (var n = 0; n < 7; n++)
                {
                    SongList[n + 1].Init(songs[n]);
                }

                if (!select)
                {
                    SelectIndex = 3;
                    VirtualIndex = 0;
                }

                UpdateLayoutByIndex();
            }
            else
            {
                OverSongCount = false;
                UpdateShortLayout(songs);

                if(!select)
                    SelectIndex = 0;

            }

            if(!isAwake)
                Select(SelectIndex);
            
        }

        private void ResortLayout()
        {
            UpdateLayoutByIndex();
            AllSongsTransform.anchoredPosition = new Vector2(0, 0);
            Select(SelectIndex);
        }

        public void RefreshFolderList()
        {
            foreach (var f in FolderListContentTransform.GetComponentsInChildren<FolderCard>())
            {
                Object.DestroyImmediate(f.gameObject);
            }

            foreach (var k in _customSongs.Keys)
            {
                if(k == "favorite") continue;
                Instantiate(FolderPrefab).TryGetComponent(out FolderCard fc);
                fc.Init(_customSongs[k].TryGetValue(Current.DetailData.SongID, out var v), k);
                fc.transform.SetParent(FolderListContentTransform);
                fc.transform.localScale = Vector3.one;

                fc.OnRemove = () =>
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    var now = Categories[CurrentCategory];
                    
                    SongByCategory.Remove(k);
                    _customSongs.Remove(k);
                    Categories.Remove(k);
                    
                    CurrentCategory = Utils.GetIndex(CurrentCategory, Categories.Count);
                    
                    CategoryText.text = Categories[CurrentCategory].ToString().ToUpper();
                    PrevCategoryText.text = Categories[Utils.GetIndex(CurrentCategory+1, Categories.Count)].ToString().ToUpper();
                    NextCategoryText.text = Categories[Utils.GetIndex(CurrentCategory-1, Categories.Count)].ToString().ToUpper();
                    
                    if(now == k)
                        SwitchCategory(MoveDirection.None, false, true);
                };

                fc.OnToggle = (t) =>
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    if (Current == null) return;

                    if (t)
                    {
                        if (!_customSongs[k].TryGetValue(Current.DetailData.SongID, out var v2))
                        {
                            SongByCategory[k].Add(Current.DetailData);
                            _customSongs[k][Current.DetailData.SongID] = true;
                            SongByCategory[k] = SongByCategory[k].OrderBy(a => a.Title).ToList();
                        }
                    }
                    else
                    {
                        SongByCategory[k].Remove(Current.DetailData);
                        _customSongs[k].Remove(Current.DetailData.SongID);
                    }

                    if (Categories[CurrentCategory] == k)
                    {
                        var songs = SongByCategory[Categories[CurrentCategory]];
                        OverSongCount = songs.Count > 7;
                        if (songs.Count > 7)
                            ResortLayout();
                        else
                        {
                            UpdateShortLayout(songs);
                            SelectIndex = 0;
                            Select(0);
                        }
                    }


                };
            }
        }
        


        private void ScrollDown(List <SongDetailData> songs, MoveDirection direction)
        {
            
            if (!OverSongCount)
            {
                    
                SongList[SelectIndex + 1].Deselect();
                switch (direction)
                {
                    case MoveDirection.Down:
                        SelectIndex++;
                        break;
                    case MoveDirection.Up:
                        SelectIndex--;
                        break;
                }

                if (SelectIndex >= songs.Count)
                    SelectIndex = 0;
                if (SelectIndex < 0)
                    SelectIndex = songs.Count - 1;
                
                Select(SelectIndex);
            }
            else
            {
                switch (direction)
                {
                    case MoveDirection.Down:
                        VirtualIndex++;
                        break;
                    case MoveDirection.Up:
                        VirtualIndex--;
                        break;
                }
                
                if (VirtualIndex >= songs.Count)
                    VirtualIndex = 0;
                if (VirtualIndex < 0)
                    VirtualIndex = songs.Count - 1;

                var animationIndex = direction == MoveDirection.Down ? 1 : 0;

                if (ScrollTween[animationIndex]!=null)
                {   
                    if (ScrollTween[animationIndex].IsPlaying())
                    {
                        
                        VirtualIndex += direction == MoveDirection.Down ? -1 : 1;
                        //VirtualIndex--;
                        ScrollTween[animationIndex].Kill(true);
                        VirtualIndex += direction == MoveDirection.Down ? 1 : -1;
                        //VirtualIndex++;
                    }
                }
                
                

                ScrollTween[animationIndex]?.Kill(true);
                /*
                ScrollTween[0] = AllSongsTransform.TweenAnchorPosY(0.2f, -125f);
                ScrollTween[0].OnComplete = () =>
                {
                    UpdateLayoutByIndex();
                    AllSongsTransform.anchoredPosition = new Vector2(0, 0);
                    Select(SelectIndex);
                };*/
                    
                ScrollTween[animationIndex] = AllSongsTransform.DOAnchorPosY(direction == MoveDirection.Down ? 125f : -125f,0.2f);
                ScrollTween[animationIndex].OnComplete(ResortLayout);
            }
            
        }

        private IEnumerator UpdateSong(SongDetailData detailData)
        {
            yield return Preview.LoadSong(detailData.SongPath, detailData.SongID);
            Preview.Play(detailData.SongID,detailData.PreviewSongStart,detailData.PreviewSongEnd);


            if (detailData.GGDifficulty == 0)
            {
                using (var request = UnityWebRequest.Get(
                           "https://adofai.gg/api/v1/levels?offset=0&amount=1&sort=RECENT_DESC&query=" +
                           detailData.Title + "&minTiles=" + (detailData.TileCount - 1) + "&maxTiles=" +
                           (detailData.TileCount + 1) + "&includeTags=&excludeTags="))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var text = request.downloadHandler.text;
                        if (text.Contains("\"difficulty\":"))
                        {
                            var d =
                                text.Split("\"difficulty\":")[1].Split(",")[0]
                                    .Trim();
                            float.TryParse(d, out detailData.GGDifficulty);
                            AdofaiGGLevelText.text = "Lv." + Utils.FloatToDiffString(detailData.GGDifficulty);
                        }
                    }
                }
            }
            
            _audioLoadingCoroutine = null;
            yield break;
        }
        

        public void SaveCustomFolder()
        {
            var sb = new StringBuilder();
            foreach (var f in _customSongs.Keys)
            {
                if (string.IsNullOrEmpty(f)) continue;
                
                sb.Append("!");
                sb.Append(f);
                sb.Append("\n");

                foreach (var c in _customSongs[f].Keys)
                {
                    sb.Append(c);
                    sb.Append("\n");
                }
            }

            File.WriteAllText(Path.Combine(DefaultSavePath, "folder.dat"), sb.ToString().Trim());
        }
        
        
        public void LoadCustomFolder()
        {
            if (!File.Exists(Path.Combine(DefaultSavePath,"folder.dat"))) return;
            var lastFolder = "";
            foreach (var f in File.ReadAllLines(Path.Combine(DefaultSavePath,"folder.dat")))
            {
                if (f.StartsWith("!"))
                {
                    lastFolder = f.Substring(1);
                    if(string.IsNullOrEmpty(lastFolder)) continue;
                    
                    Categories.Add(lastFolder);
                    _customSongs[lastFolder] = new Dictionary<string, bool>();
                }
                else
                {
                    if(!string.IsNullOrEmpty(lastFolder) && !string.IsNullOrEmpty(f))
                        _customSongs[lastFolder][f] = true;
                }
            }
        }


        public void RemoveLevel()
        {
            if (Current == null) return;

            try
            {
                var id = new DirectoryInfo(Current.DetailData.Path).Name;
                AdofaiAPI.Unsubscribe(ulong.Parse(id), _customSongs["featured"].TryGetValue(id, out var v));
            }
            catch
            {
                // ignored
            }
            
            Directory.Delete(Current.DetailData.Path, true);


            foreach (var c in Categories)
            {
                SongByCategory[c].RemoveAll(a => a.SongID == Current.DetailData.SongID);
            }

            var songs = SongByCategory[Categories[CurrentCategory]];
            OverSongCount = songs.Count > 7;
            if (songs.Count > 7)
                ResortLayout();
            else
            {
                UpdateShortLayout(songs);
                SelectIndex = 0;
                Select(0);
            }
        }

        public void MoveNextChar(bool up)
        {
            if (Current == null) return;
            
            var now = SongByCategory[Categories[CurrentCategory]];
            var f = (int)now.First().Title.ToLower()[0];
            var e = (int)now.Last().Title.ToLower()[0];
            var c = (int)Current.DetailData.Title.ToLower()[0];

            var lastDiff = 99999;
            var lastIndex = -1;

            if (!Utils.IsAlphabet(c))
            {
                /*
                if (c == 122 && !up)
                    lastIndex = 0;*/

                if (!up)
                {
                    lastIndex = now.FindIndex((s) =>
                    {
                        var n = (int)s.Title.ToLower()[0];
                        return Utils.IsAlphabet(n);
                    });
                }
                else
                {
                    lastIndex = now.FindLastIndex((s) =>
                    {
                        var n = (int)s.Title.ToLower()[0];
                        return Utils.IsAlphabet(n);
                    });
                }
            }
            
            
            if(lastIndex == -1) {
                for (var i = 0; i < now.Count; i++)
                {
                    var n = (int)now[i].Title.ToLower()[0];
                    var diff = Math.Abs(c - n);

                    if (diff < lastDiff && up && n < c)
                    {
                        lastDiff = diff;
                        lastIndex = i;
                    }

                    if (diff < lastDiff && !up && n > c)
                    {
                        lastDiff = diff;
                        lastIndex = i;
                    }
                }
            }

            if (lastIndex != -1)
            {
                if (OverSongCount)
                {
                    VirtualIndex = lastIndex;
                    ResortLayout();
                }
                else
                {
                    SelectIndex = lastIndex;
                    if(Current != null)
                        Current.Deselect();
                    Select(SelectIndex);
                }
            } 

        }
        
        
        private void Update()
        {
            if (Loading) return;
            
            
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (FolderList.activeSelf)
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    FolderList.SetActive(false);
                    FolderCreate.SetActive(true);

                    NameInputField.Select();


                } else if (FolderCreate.activeSelf)
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    var f_name = NameInputField.text.Trim();


                    if (!string.IsNullOrEmpty(f_name))
                    {
                        _customSongs[f_name] = new Dictionary<string, bool>();
                        foreach (var k in _customSongs.Keys)
                        {
                            if (!Categories.Contains(k))
                                Categories.Add(k);

                            if (!SongByCategory.TryGetValue(k, out var v))
                                SongByCategory[k] = new List<SongDetailData>();
                        }
                    }

                    CategoryText.text = Categories[Utils.GetIndex(CurrentCategory, Categories.Count)].ToString().ToUpper();
                    PrevCategoryText.text = Categories[Utils.GetIndex(CurrentCategory+1, Categories.Count)].ToString().ToUpper();
                    NextCategoryText.text = Categories[Utils.GetIndex(CurrentCategory-1, Categories.Count)].ToString().ToUpper();

                    NameInputField.text = "";
                    
                    FolderList.SetActive(true);
                    FolderCreate.SetActive(false);

                    RefreshFolderList();
                }
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                AdofaiAPI.PlaySfx("NotificationIn");
                
                ToggleFolderUI();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (FolderList.activeSelf)
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    ToggleFolderUI();
                }
                else if (FolderCreate.activeSelf)
                {
                    AdofaiAPI.PlaySfx("NotificationIn");
                    
                    FolderList.SetActive(true);
                    FolderCreate.SetActive(false);

                    RefreshFolderList();
                }
                else
                {
                    Loading = true;
                    Loader.instance.MoveBlack(() =>
                    {
                        Preview.Stop();
                        OnESC?.Invoke();
                    });
                }
            }

            if (FolderParent.activeSelf) return;
            
            SearchStartWith();
            
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _toggleDescription = !_toggleDescription;

                LevelDiffGameObject.SetActive(!_toggleDescription);
                Description.gameObject.SetActive(_toggleDescription);
                
                AdofaiAPI.PlaySfx("NotificationIn");
                
            }
            

            if (Input.GetKeyDown(KeyCode.F2))
            {
                _toogleSpeed = !_toogleSpeed;
                BlurBG.color = _toogleSpeed ? new Color(0.4f,0,0) : Color.black;
                SpeedTrialRabbit.gameObject.SetActive(_toogleSpeed);
                
                AdofaiAPI.PlaySfx(_toogleSpeed? "SpeedTrialOn":"SpeedTrialOff");
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                _toggleNoFail = !_toggleNoFail;
                NoFail.gameObject.SetActive(_toggleNoFail);
                
                AdofaiAPI.PlaySfx(_toggleNoFail? "ModifierActivate":"ModifierDeactivate");
            }
            
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Loader.instance.MoveBlack(() =>
                {
                    SceneManager.LoadScene("LevelSelect");
                });
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                RemoveLevel();
                AdofaiAPI.PlaySfx("NotificationIn");
            }

            if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.PageDown))
            {
                MoveNextChar(Input.GetKeyDown(KeyCode.PageUp));
            }


            if (Input.GetKeyDown(KeyCode.F6))
            {
                AdofaiAPI.OpenWorkshop();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (Current != null)
                {
                    if (Current.DetailData.IsFavorite)
                        _customSongs["favorite"].Remove(Current.DetailData.SongID);
                    else
                        _customSongs["favorite"].Add(Current.DetailData.SongID, true);
                    
                    Current.DetailData.IsFavorite = !Current.DetailData.IsFavorite;
                    Current.UpdateFavorite();
                }
                AdofaiAPI.PlaySfx("NotificationIn");
            }
            
            
            var songs = SongByCategory[Categories[CurrentCategory]];


            if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                _pressTime = 0;
                _pressEnd = true;
            }
            
                
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                AdofaiAPI.PlaySfx("NotificationOut");
                ScrollDown(songs, MoveDirection.Down);
            }

            if (Input.GetAxis("Mouse ScrollWheel") > 0f || Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                _pressTime += UnityEngine.Time.deltaTime;
                AdofaiAPI.PlaySfx("NotificationOut");
                ScrollDown(songs, Input.GetAxis("Mouse ScrollWheel") > 0f? MoveDirection.Up : MoveDirection.Down);
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.UpArrow))
            {
                _pressTime += UnityEngine.Time.deltaTime;
                if (_pressTime > 0.5 && _pressEnd)
                {
                    _pressTime = 0;
                    _pressEnd = false;
                }
                
                if (_pressTime > 0.1 && !_pressEnd)
                {
                    _pressTime = 0;
                    AdofaiAPI.PlaySfx("NotificationOut");
                    ScrollDown(songs, Input.GetKey(KeyCode.DownArrow)? MoveDirection.Down : MoveDirection.Up);
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                //NotificationOut
                AdofaiAPI.PlaySfx("NotificationOut");
                ScrollDown(songs, MoveDirection.Up);
            }


            if (Input.GetKeyDown(KeyCode.RightShift))
            {
                ScrollTween[0]?.Kill();
                ScrollTween[1]?.Kill();
                AllSongsTransform.anchoredPosition = new Vector2(0, 0);
                AdofaiAPI.PlaySfx("PortalSelect");
                SwitchCategory(MoveDirection.Right);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                ScrollTween[0]?.Kill();
                ScrollTween[1]?.Kill();
                AllSongsTransform.anchoredPosition = new Vector2(0, 0);
                AdofaiAPI.PlaySfx("PortalSelect");
                SwitchCategory(MoveDirection.Left);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                Preview.Stop();
                LoadSong(SelectIndex);
            }
        }

        public void UpdateLayoutByIndex()
        {
            var c = Categories[CurrentCategory];
            var songs = SongByCategory[c];
            
            SongList[0].Init(songs[Utils.GetIndex(VirtualIndex-1-SelectIndex, songs.Count)]);
            SongList[8].Init(songs[Utils.GetIndex(VirtualIndex+7-SelectIndex, songs.Count)]);
            for (var n = 0; n < 7; n++)
            {
                SongList[n + 1].Init(songs[Utils.GetIndex(VirtualIndex+n-SelectIndex, songs.Count)]);
            }
        }


        private void LoadSong(int index)
        {
            if (SongByCategory[Categories[CurrentCategory]].Count == 0 || index == -1) return;
            Loading = true;
            Loader.instance.MoveBlack(()=>
            {
                Preview.Stop();
                AdofaiAPI.SetSpeedTrialMode(_toogleSpeed);
                AdofaiAPI.SetNoFail(_toggleNoFail);
                OnLoading?.Invoke();
            });
            //SongLoader.LoadSong(SongList[index + 1].InfoMini.Title);
        }
        
        
        
        
    }
}