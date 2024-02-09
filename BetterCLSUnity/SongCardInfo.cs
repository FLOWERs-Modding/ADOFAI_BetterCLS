
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCLSUnity
{
    public class SongCardInfo : MonoBehaviour
    {
        /*
        public string SongName;
        public string AuthorName;
*/
        
        
        public Text SongText;
        public Text AuthorText;
        public Image ClearRank;
        public Text FavoriteText;
        public Image PreviewImage;
        public Image BG;
        public Color TargetColor;
        public SongDetailData DetailData;
        public Outline OutlineBG;

        private Tween tweenColor;

        public void Select()
        {
            tweenColor?.Kill();
            tweenColor = OutlineBG.DOColor(TargetColor,0.2f);
        }

        public void Deselect()
        {
            tweenColor?.Kill();
            OutlineBG.effectColor = Color.clear;
        }
        
        public void Init(SongDetailData i)
        {
            
            DetailData = i;
            gameObject.SetActive(true);
            SongText.text = i.Title;
            AuthorText.text= i.Author;
            TargetColor = i.TargetColor;
            TargetColor.a = 0.3f;
            OutlineBG.effectColor = Color.clear;

            var min = Math.Min(i.PreviewImage.texture.width, i.PreviewImage.texture.height);
            var max = Math.Max(i.PreviewImage.texture.width, i.PreviewImage.texture.height);
            var f = max / (float)min;

            PreviewImage.rectTransform.sizeDelta = i.PreviewImage.texture.width > i.PreviewImage.texture.height ? new Vector2(75 * f, 75) : new Vector2(75 , 75 * f);
            PreviewImage.sprite = i.PreviewImage;

            var rank = Utils.GetRankIndex(DetailData);
            ClearRank.gameObject.SetActive(rank != -1);
            ClearRank.sprite = rank == -1 ? null : SongListController.instance.RankSprite[rank];

            UpdateFavorite();
        }


        public void UpdateFavorite()
        {
            FavoriteText.gameObject.SetActive(DetailData.IsFavorite);
        }


    }
}