using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCLSUnity
{
    public class FolderCard : MonoBehaviour
    {
        public Toggle Toggle;
        public Text FolderName;
        public Button RemoveButton;
        public Action OnRemove;
        public Action<bool> OnToggle;

        private void Awake()
        {
            RemoveButton.onClick.AddListener(() =>
            {
                OnRemove?.Invoke();
            });
            
            Toggle.onValueChanged.AddListener((b) =>
            {
                OnToggle?.Invoke(b);
            });
        }

        public void Init(bool toggle, string text)
        {
            Toggle.isOn = toggle;
            FolderName.text = text;
        }
    }
}