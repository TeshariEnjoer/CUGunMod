using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CasGunMod.Assets {

    public class ActionButton : AdaptiveButton {
        public GameObject Root { get; private set; }
        public RectTransform RootRect { get; private set; }
        public GameObject BackgroundGO { get; private set; }
        public Image BackgroundImage { get; private set; }
        public GameObject ButtonGO { get; private set; }
        public Image ButtonImage { get; private set; }
        public Button UIButton { get; private set; }

        public Sprite BackgroundSprite { get; private set; }
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

        public static ActionButton Create(Transform parent = null, Vector2? size = null, KeyCode? hotkey = null, Action<ActionButton> init = null) {
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
        public KeyCode Key = KeyCode.None;

        private void Update() {
            if (Owner == null) return;
            if (Key == KeyCode.None) return;
            if (UnityInput.Current.GetKeyDown(Key)) {
                Owner.TriggerClick();
            }
        }
    }




        public void Build(Transform parent = null, Vector2 size = default) {
            if (built) return;
            built = true;

            Root = new GameObject("ActionButton");
            RootRect = Root.AddComponent<RectTransform>();
            Root.transform.SetParent(parent, false);


            if (size == Vector2.zero) 
                size = new Vector2(32f, 32f);
            RootRect.sizeDelta = size;

            BackgroundGO = new GameObject("Background");
            BackgroundGO.transform.SetParent(Root.transform, false);
            BackgroundImage = BackgroundGO.AddComponent<Image>();
            var bgRect = BackgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            if (BackgroundSprite != null) 
                BackgroundImage.sprite = BackgroundSprite;


            ButtonGO = new GameObject("Button");
            ButtonGO.transform.SetParent(Root.transform, false);
            ButtonImage = ButtonGO.AddComponent<Image>();
            UIButton = ButtonGO.AddComponent<Button>();
            var btnRect = ButtonGO.GetComponent<RectTransform>();
   
            btnRect.anchorMin = new Vector2(0f, 0f);
            btnRect.anchorMax = new Vector2(1f, 1f);
            float insetX = Math.Max(2f, size.x * 0.02f);
            float insetY = Math.Max(2f, size.y * 0.08f);
            btnRect.offsetMin = new Vector2(insetX, insetY);
            btnRect.offsetMax = new Vector2(-insetX, -insetY);
            if (ButtonSprite != null) 
                ButtonImage.sprite = ButtonSprite;

            UIButton.onClick.AddListener(() => TriggerClick());
            if (visualSetter == null) {
                visualSetter = (isAvail) => {
                    if (ButtonImage != null) 
                        ButtonImage.color = isAvail ? Color.white : Color.red;
                    if (BackgroundImage != null) 
                        BackgroundImage.color = isAvail ? Color.white : new Color(1f, 0.5f, 0.5f, 1f);
                    if (UIButton != null) 
                        UIButton.interactable = isAvail;
                };
            }

            if (Hotkey != KeyCode.None) {
                HotkeyGO = new GameObject("HotkeyLabel");
                HotkeyGO.transform.SetParent(Root.transform, false);
                HotkeyText = HotkeyGO.AddComponent<Text>();
                var hkRect = HotkeyGO.GetComponent<RectTransform>();
                hkRect.anchorMin = new Vector2(1f, 0f);
                hkRect.anchorMax = new Vector2(1f, 0f);
                hkRect.pivot = new Vector2(1f, 0f);
                hkRect.sizeDelta = new Vector2(40f, 18f);
                hkRect.anchoredPosition = new Vector2(-4f, 4f);
                HotkeyText.text = Hotkey.ToString();
                HotkeyText.fontSize = Mathf.Max(10, Mathf.RoundToInt(Mathf.Min(size.y, 18f)));
                HotkeyText.alignment = TextAnchor.LowerRight;
                HotkeyText.color = Color.white;
                HotkeyText.raycastTarget = false;
                HotkeyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                var hkListener = Root.AddComponent<HotkeyListener>();
                hkListener.Owner = this;
                hkListener.Key = Hotkey;
            }

            visualSetter?.Invoke(IsAvailableState);
        }


        public static void PositionButtonsAroundBody(IEnumerable<ActionButton> buttons, Transform body, Canvas canvas = null, float radius = 100f, float centerAngleDeg = -135f, float arcSpanDeg = 30f) {
            if (body == null) return;
            var btnList = buttons?.Where(b => b != null).ToArray();
            if (btnList == null || btnList.Length == 0) return;

            if (canvas == null) {
                var first = btnList[0];
                if (first != null && first.Root != null) {
                    canvas = first.Root.GetComponentInParent<Canvas>();
                }
            }
            if (canvas == null) {
                canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null) return;
            }

            var canvasRect = canvas.transform as RectTransform;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;


            var camForScreen = Camera.main != null ? Camera.main : cam;
            Vector3 screenCenter = Vector3.zero;
            if (camForScreen != null)
                screenCenter = camForScreen.WorldToScreenPoint(body.position);
            else
                screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            int n = btnList.Length;
            for (int i = 0; i < n; i++) {
                var btn = btnList[i];
                if (btn == null) continue;


                if (!btn.built) 
                    btn.Build(canvas.transform, btn.RootRect != null ? btn.RootRect.sizeDelta : new Vector2(32f, 32f));
                
                if (btn.Root.transform.parent != canvas.transform) 
                    btn.Root.transform.SetParent(canvas.transform, false);

                float angleDeg;
                if (n == 1) angleDeg = centerAngleDeg;
                else angleDeg = centerAngleDeg - arcSpanDeg / 2f + (arcSpanDeg) * i / (n - 1);

                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                Vector2 screenPos = new Vector2(screenCenter.x, screenCenter.y) + offset;

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out localPoint);


                if (btn.RootRect == null) btn.RootRect = btn.Root.GetComponent<RectTransform>();
                btn.RootRect.pivot = new Vector2(0.5f, 0.5f);
                btn.RootRect.anchoredPosition = localPoint;
            }
        }


        public ActionButton SetBackgroundSprite(Sprite sprite) {
            BackgroundSprite = sprite;
            if (BackgroundImage != null) 
                BackgroundImage.sprite = sprite;
            return this;
        }


        public ActionButton SetButtonSprite(Sprite sprite) {
            ButtonSprite = sprite;
            if (ButtonImage != null) 
                ButtonImage.sprite = sprite;
            return this;
        }


        public ActionButton SetBackgroundSprite(object spriteOrName) {
            if (spriteOrName is Sprite s) 
                return SetBackgroundSprite(s);
            if (spriteOrName is string name) {
                var sres = AssetStorage.GetSpriteByName(name);
                if (sres != null) 
                    return SetBackgroundSprite(sres);
            }
            return this;
        }

        public ActionButton SetButtonSprite(object spriteOrName) {
            if (spriteOrName is Sprite s) return 
                    SetButtonSprite(s);
            if (spriteOrName is string name) {
                var sres = AssetStorage.GetSpriteByName(name);
                if (sres != null) 
                    return SetButtonSprite(sres);
            }
            return this;
        }

        public ActionButton SetAvailabilityCheck(Func<bool> check) {
            availabilityCheck = check;
            return this;
        }

        public ActionButton IsAvaible(Func<bool> check) => SetAvailabilityCheck(check);

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

        public ActionButton SetSize(float width, float height) => SetSize(new Vector2(width, height));

        public void EvaluateAvailability() {
            bool newState = true;
            try {
                if (availabilityCheck != null) 
                    newState = availabilityCheck();
            } catch {
                newState = false;
            }

            if (newState != IsAvailableState) {
                IsAvailableState = newState;
                visualSetter?.Invoke(IsAvailableState);
                OnAvailabilityChanged?.Invoke(IsAvailableState);
            } else {
                visualSetter?.Invoke(IsAvailableState);
            }
        }

        public void TriggerClick() {
            if (!IsAvailableState) 
                return;
            OnClick?.Invoke();
        }


        public ActionButton WithTooltip(string text) {
            return this;
        }
    }
}
