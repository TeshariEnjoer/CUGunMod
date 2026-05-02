using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace CasGunMod.Assets {
    public class ActionButton : AdaptiveButton {
        public GameObject Root { get; private set; }
        public RectTransform RootRect { get; private set; }

        public GameObject ButtonGO { get; private set; }
        public Image ButtonImage { get; private set; }
        public Button UIButton { get; private set; }

        public Sprite ButtonSprite { get; private set; }

        public KeyCode Hotkey { get; private set; } = KeyCode.None;

        public GameObject HotkeyGO { get; private set; }
        public Text HotkeyText { get; private set; }

        private Func<bool> availabilityCheck;
        private Action<bool> visualSetter;

        public event Action<bool> OnAvailabilityChanged;
        public event Action OnClick;

        public bool IsAvailableState { get; private set; } = true;
        private bool built = false;
        private bool builted = false;

        public static ActionButton Create(
            Transform parent = null,
            Vector2? size = null,
            KeyCode? hotkey = null,
            Action<ActionButton> init = null) {
            var btn = new ActionButton();

            if (hotkey.HasValue)
                btn.Hotkey = hotkey.Value;

            init?.Invoke(btn);

            btn.Build(parent, size ?? new Vector2(32f, 32f));
            btn.EvaluateAvailability();

            return btn;
        }


        public class HotkeyListener : MonoBehaviour {
            public ActionButton Owner;
            public KeyCode Key;

            private void Update() {
                if (Owner == null) return;
                if (Key == KeyCode.None) return;

                if (UnityInput.Current.GetKeyDown(Key))
                    Owner.TriggerClick();
            }
        }


        public void Build(Transform parent = null, Vector2 size = default) {
            if (built) return;
            built = true;

            if (size == Vector2.zero)
                size = new Vector2(32f, 32f);

            Root = new GameObject("ActionButton", typeof(RectTransform));
            Root.transform.SetParent(parent, false);

            RootRect = Root.GetComponent<RectTransform>();
            RootRect.sizeDelta = size;
            RootRect.anchorMin = new Vector2(0.5f, 0.5f);
            RootRect.anchorMax = new Vector2(0.5f, 0.5f);
            RootRect.pivot = new Vector2(0.5f, 0.5f);

            ButtonGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Button));
            ButtonGO.transform.SetParent(Root.transform, false);

            ButtonImage = ButtonGO.GetComponent<Image>();

            UIButton = ButtonGO.GetComponent<Button>();

            var btnRect = ButtonGO.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            if (ButtonSprite != null)
                ButtonImage.sprite = ButtonSprite;

            UIButton.onClick.AddListener(TriggerClick);

  
            visualSetter = (isAvail) => {
                if (ButtonImage != null)
                    ButtonImage.color = isAvail ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

                if (UIButton != null)
                    UIButton.interactable = isAvail;
            };

 
            /*
            if (Hotkey != KeyCode.None) {
                HotkeyGO = new GameObject("Hotkey", typeof(RectTransform));
                HotkeyGO.transform.SetParent(Root.transform, false);

                HotkeyText = HotkeyGO.AddComponent<Text>();
                HotkeyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                HotkeyText.text = Hotkey.ToString();
                HotkeyText.fontSize = Mathf.Max(10, Mathf.RoundToInt(size.y * 0.4f));
                HotkeyText.color = Color.white;
                HotkeyText.alignment = TextAnchor.LowerRight;
                HotkeyText.raycastTarget = false;

                var hkRect = HotkeyGO.GetComponent<RectTransform>();
                hkRect.anchorMin = new Vector2(1f, 0f);
                hkRect.anchorMax = new Vector2(1f, 0f);
                hkRect.pivot = new Vector2(1f, 0f);
                hkRect.sizeDelta = new Vector2(40f, 18f);
                hkRect.anchoredPosition = new Vector2(-2f, 2f);

                var hkListener = Root.AddComponent<HotkeyListener>();
                hkListener.Owner = this;
                hkListener.Key = Hotkey;
            }
            */ 

            visualSetter?.Invoke(IsAvailableState);
            builted = true;
        }

        private void Update() {
            if(availabilityCheck != null && builted)
                EvaluateAvailability();
        }
   

        public ActionButton SetButtonSprite(Sprite sprite) {
            ButtonSprite = sprite;

            if (ButtonImage != null)
                ButtonImage.sprite = sprite;
                ButtonImage.SetNativeSize();

            return this;
        }

        public ActionButton SetAvailabilityCheck(Func<bool> check) {
            availabilityCheck = check;
            return this;
        }

        public ActionButton SetVisualSetter(Action<bool> setter) {
            visualSetter = setter;

            if (built)
                visualSetter?.Invoke(IsAvailableState);

            return this;
        }

        public ActionButton SetOnClick(Action onClick) {
            OnClick = onClick;

            return this;
        }

        public ActionButton SetSize(Vector2 size) {
            if (RootRect != null)
                RootRect.sizeDelta = size;

            return this;
        }

        public ActionButton SetPosition(Vector2 anchoredPosition) {
            if (RootRect != null)
                RootRect.position = anchoredPosition;
            return this;
        }

        public void EvaluateAvailability() {
            if(!builted) return;

            bool newState = true;


            if (availabilityCheck != null)
                newState = availabilityCheck();
    

            if (newState != IsAvailableState) {
                IsAvailableState = newState;
                visualSetter?.Invoke(IsAvailableState);
                OnAvailabilityChanged?.Invoke(IsAvailableState);
            } else {
                visualSetter?.Invoke(IsAvailableState);
            }
        }

        public void TriggerClick() {
            Debug.LogWarning("ActionButton clicked");
            if (!IsAvailableState || !builted) return;
            OnClick?.Invoke();
        }

        public void SetColor(Color red) {
            if (ButtonImage != null) {
                ButtonImage.color = red;
            }
        }
    }
}