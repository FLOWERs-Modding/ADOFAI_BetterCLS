using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCLSUnity
{
    public class Loader : MonoBehaviour
    {
        public Image BlackImage;
        public static Loader instance;
        public void Awake()
        {
            BlackImage.rectTransform.localScale = Vector3.one;
            BlackImage.gameObject.SetActive(true);
            instance = this;
            
        }

        public void Start()
        {
            BlackImage.rectTransform.DOKill();
            BlackImage.rectTransform.localScale = new Vector3(1, 1, 1);
            BlackImage.gameObject.SetActive(true);
            
            BlackImage.rectTransform.DOScaleX(0, 0.3f).SetEase(Ease.InOutQuint);
            
            
        }

        public void MoveBlack(Action OnEnd = null)
        {
            BlackImage.rectTransform.DOKill();
            BlackImage.rectTransform.localScale = new Vector3(0, 1, 1);
            BlackImage.gameObject.SetActive(true);

            BlackImage.rectTransform.DOScaleX(1, 0.3f).SetEase(Ease.InOutQuint).OnComplete(()=>OnEnd?.Invoke());
            
            AdofaiAPI.PlaySfx("ScreenWipeIn");
        }
        
        public void MoveBlackToClear(Action OnEnd = null)
        {
            BlackImage.rectTransform.DOKill();
            BlackImage.rectTransform.localScale = new Vector3(1, 1, 1);
            BlackImage.gameObject.SetActive(true);

            BlackImage.rectTransform.DOScaleX(0, 1f).SetEase(Ease.OutQuint).OnComplete(()=>OnEnd?.Invoke());
            
            AdofaiAPI.PlaySfx("ScreenWipeOut");
        }
    }
}