using UnityEngine;

namespace BetterCLSUnity
{
    public class SongDetailData
    {
        public string Title;
        public string Author;
        public string BPM;
        public Sprite BG;
        public string Maker;
        public string SongPath;
        public Sprite PreviewImage;
        public string PreviewImagePath;
        public string Time;
        public string Path;
        public string SongID;
        public Color TargetColor;
        public string TargetColorString;
        public string ArtistLink;
        public string Description;
        
        public float Difficulty;
        public float GGDifficulty;
        public int TileCount;

        public int PreviewSongStart;
        public int PreviewSongEnd;

        public int Attempt;
        public bool RequireNeocosmos;
        public float MaxProgress;
        public float MaxAccuracy;
        public bool IsPerfect;
        
        public bool IsFavorite;
    }
}