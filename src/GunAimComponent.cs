using BepInEx;
using CasGunMod.Assets;
using System.Collections.Generic;
using UnityEngine;
using static GunScript;

namespace CasGunMod {
    public class GunAimComponent : MonoBehaviour {

        public PlayerCamera Camera;
        public Body Body;
        public Vector3 AimAt => _lastAimAt;

        private bool _cursorOverridden = false;
        private bool _sefetyOn = false;
        private GameObject _gunMenu;

        private GameObject _crosshairGo;
        private SpriteRenderer _crosshair;

        private ActionButton _ejectMagazineButton;
        private ActionButton _toggleSefetyButton;
        private ActionButton _toggleFiremodeButton; 

        private List<ActionButton> _buttons = new List<ActionButton>();

        private float _spread;
        private Vector3 _lastAimAt = Vector3.zero;

        private const string CROSSHAIR_SPRITE = "crosshair";
        private const string BUTTON_EJECT_SPRITE = "button_eject";
        private const string BUTTON_SEFETY_SPRITE = "button_sefety";
        private const string BUTTON_FIREMODE_SPRITE = "button_firemode";

        private readonly Vector2 BUTTON_SIZE = new Vector2(64f, 64f);

        private const float BASE_SIZE = 0.5f;
        private const float SPREAD_MULTIPLIER = 2f;
        private const float GLOBAL_SCALE = 3f;

        private const float POSITION_SMOOTH_SPEED = 12f;
        private const float SIZE_SMOOTH_SPEED = 10f;
        private const float ROTATION_SMOOTH_SPEED = 10f;
        private const float MAX_ROTATION_ANGLE = 15f; 
        private const float DISTANCE_TO_UI = 280f;

        private const float HEAD_HORIZONTAL_RADIUS = 0.9f;
        private const float HEAD_HEIGHT_THRESHOLD = 0.6f;
        private const float BACK_DOT_THRESHOLD = 0f;
        private static readonly Color VALID_COLOR = Color.white;
        private static readonly Color INVALID_COLOR = Color.red;

        public void Attach(PlayerCamera camera, Body body) {
            Camera = camera;
            Body = body;        

            if (_gunMenu != null) {
                Camera = camera;
                Body = body;
                return;
            }

            _gunMenu = new GameObject("GunMenu");

            if (Camera != null && Camera.gunMenu != null) {
                _gunMenu.transform.SetParent(Camera.gunMenu.transform.parent, false);
            }

            if(Body.TryGetHoldingItem(out var item) && GunHelpers.IsGun(item)) {
                var gun = item.GetComponent<GunScript>();
                if (gun != null) {
                    UpdateSefety();
                }
            }

            _crosshairGo = new GameObject("CrosshairSprite");
            _crosshairGo.transform.SetParent(_gunMenu.transform, false);
            _crosshair = _crosshairGo.AddComponent<SpriteRenderer>();

            var sprite = AssetStorage.GetSpriteByName(CROSSHAIR_SPRITE);
            if (sprite == null) {
                Debug.LogError($"[GunRender] Failed to load sprite: {CROSSHAIR_SPRITE}");
                Destroy(_gunMenu);
                return;
            }

            _crosshair.sprite = sprite;
            _crosshair.enabled = false;
            _crosshair.sortingOrder = 1000;

            if (Camera != null && Camera.radialMenu != null && Camera.radialMenu.parent != null) {
                _crosshair.transform.SetParent(Camera.radialMenu.parent, false);
            }

            _crosshair.transform.localPosition = Vector3.zero;
            _crosshair.transform.localRotation = Quaternion.identity;

            _buttons = new List<ActionButton>();

            /*
            _toggleFiremodeButton = ActionButton.Create(_gunMenu.transform, BUTTON_SIZE, KeyCode.U)
                .SetButtonSprite(AssetStorage.GetSpriteByName(BUTTON_FIREMODE_SPRITE))
                .SetOnClick(() => {
                    Camera.GunRack();
                });
            _buttons.Add(_toggleFiremodeButton);
            */

            _ejectMagazineButton = ActionButton.Create(_gunMenu.transform, BUTTON_SIZE, KeyCode.T)
                .SetButtonSprite(AssetStorage.GetSpriteByName(BUTTON_EJECT_SPRITE))
                .SetAvailabilityCheck(() => CanEjectMagazine())
                .SetOnClick(() => {
                    Camera.GunEjectMag();
                });
            _ejectMagazineButton.SetVisualSetter((available) => {
                if (available) {
                    _ejectMagazineButton.SetColor(Color.white);
                } else {
                    _ejectMagazineButton.SetColor(Color.red);
                }
            });

            _buttons.Add(_ejectMagazineButton);

            _toggleSefetyButton = ActionButton.Create(_gunMenu.transform, BUTTON_SIZE, KeyCode.Y)
                .SetButtonSprite(AssetStorage.GetSpriteByName(BUTTON_SEFETY_SPRITE + "_d"))
                .SetOnClick(() => {
                    ToggleSefety();
                });
            _buttons.Add(_toggleSefetyButton);

            UpdateSefety();
            Debug.Log("[GunRender] Attached");
        }

        private void OnDestroy() {
            if (_gunMenu != null) {
                Destroy(_gunMenu);
                _gunMenu = null;
                _crosshair = null;

                if (_crosshair != null) {
                    Destroy(_crosshair.gameObject);
                    _crosshair = null;
                }
            }

            if (_cursorOverridden) {
                Cursor.visible = true;
                _cursorOverridden = false;
            }

            Debug.Log("[GunRender] Destroyed");
        }

        private void LateUpdate() {
            if (Camera == null || Body == null) return;

            if (!Body.TryGetHoldingItem(out var item) || !GunHelpers.IsGun(item)) { 
                DisableAim();
                Destroy(this);
                return;
            }

            if (!CanAim()) {
                DisableAim();
                return;
            }

            EnableAim();

            _spread = GetSpread();
            UpdateUiPosition(_spread);
            UpdateAim();
        }


        private void EnableCrosshair() {
            if (_crosshair != null)
                _crosshair.enabled = true;
                Cursor.visible = false;
        }

        private void DisableCrosshair() {
            if (_crosshair != null)
                _crosshair.enabled = false;
                Cursor.visible = true;
        }

        private void EnableAim() {
            if(!_sefetyOn)
                EnableCrosshair();
            
            _gunMenu.SetActive(true);
        }

        private void DisableAim() {
             DisableCrosshair();
             _gunMenu.SetActive(false);
        }

        private void UpdateUiPosition(float spread) {
            if (_crosshair == null) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 mouseScreen = UnityInput.Current.mousePosition;
            mouseScreen.z = Mathf.Abs(cam.transform.position.z);

            Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            _lastAimAt = mouseWorld;

            Vector3 currentPos = _crosshair.transform.position;
            Vector3 targetPos = mouseWorld;
            _crosshair.transform.position = Vector3.Lerp(currentPos, targetPos, Mathf.Clamp01(Time.deltaTime * POSITION_SMOOTH_SPEED));

            float size = BASE_SIZE + spread * SPREAD_MULTIPLIER;
            Vector3 targetScale = Vector3.one * size * GLOBAL_SCALE;
            _crosshair.transform.localScale = Vector3.Lerp(_crosshair.transform.localScale, targetScale, Mathf.Clamp01(Time.deltaTime * SIZE_SMOOTH_SPEED));


            Vector3 movement = targetPos - currentPos;
            Quaternion currentRot = _crosshair.transform.rotation;
            Quaternion targetRot = Quaternion.identity;
            if (movement.sqrMagnitude > 0.0001f) {
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                float limited = Mathf.Clamp(angle, -MAX_ROTATION_ANGLE, MAX_ROTATION_ANGLE);
                targetRot = Quaternion.Euler(0f, 0f, limited);
            }

            _crosshair.transform.rotation = Quaternion.Slerp(currentRot, targetRot, Mathf.Clamp01(Time.deltaTime * ROTATION_SMOOTH_SPEED));
            bool available = IsAimPositionAvailable(targetPos);
            Color targetColor = available ? VALID_COLOR : INVALID_COLOR;
            _crosshair.color = Color.Lerp(_crosshair.color, targetColor, Mathf.Clamp01(Time.deltaTime * 20f));


            Vector3 anchor = cam.WorldToScreenPoint(Body.transform.position);
            for (var i = 0; i < _buttons.Count; i++) {
                var button = _buttons[i];
                Vector2 offset = anchor + (Vector3.right * DISTANCE_TO_UI) + Vector3.up * (-45f - (i * BUTTON_SIZE.y + 5f));
                
                button.SetPosition(Vector3.Lerp(button.RootRect.transform.position, offset, Mathf.Clamp01(Time.deltaTime * POSITION_SMOOTH_SPEED)));
            }
        }


        private bool CanEjectMagazine() {
            if (Body == null) return false;
            if (!Body.TryGetHoldingItem(out var item) || !GunHelpers.IsGun(item)) return false;
            var gun = item.GetComponent<GunScript>();
            
            if (gun == null) 
                return false;
         
            return (gun.hasMag && gun.feedType != FeedType.Direct);
        }


        private void ToggleSefety() {
            if (Body == null) 
                return;
            if (!Body.TryGetHoldingItem(out var item) || !GunHelpers.IsGun(item)) return;
            var gun = item.GetComponent<GunScript>();
            if (gun == null) return;
            gun.safe = !gun.safe;
            
            if(gun.safe) {
                DisableCrosshair();
            } else {
                EnableCrosshair();
            }

            UpdateSefety();
        }


        private void UpdateSefety() {
            if (Body == null) return;
            if (!Body.TryGetHoldingItem(out var item) || !GunHelpers.IsGun(item)) return;
            var gun = item.GetComponent<GunScript>();
            _sefetyOn = gun.safe;

            if (gun == null || _toggleSefetyButton == null)
                return; 

            if(!_sefetyOn) {
                _toggleSefetyButton.SetColor(Color.red);
                _toggleSefetyButton.SetButtonSprite(AssetStorage.GetSpriteByName(BUTTON_SEFETY_SPRITE + "_d"));
            } else {
                _toggleSefetyButton.SetColor(Color.green);
                _toggleSefetyButton.SetButtonSprite(AssetStorage.GetSpriteByName(BUTTON_SEFETY_SPRITE + "_s"));
            }

        }

        private void UpdateAim() {

        }

        /// <summary>
        /// Returns true when the provided world-space aim position is valid (not behind the character
        /// and not directly above the character's head). Uses the body's transform to calculate
        /// relative position and orientation.
        /// </summary>
        private bool IsAimPositionAvailable(Vector3 aimPosition) {
            if (Body == null) return true;

            var bodyTransform = Body.transform;
            if (bodyTransform == null) 
                return true;

            if (!Body.TryGetHoldingItem(out var gun) || !GunHelpers.IsGun(gun)) {
                return true;
            }


            Vector3 barrelPos = gun.GetComponent<GunScript>().barrel.position;
            // Vector3 bodyPos = bodyTransform.position;
            Vector3 toAim = aimPosition - barrelPos;

            Vector2 horiz = new Vector2(toAim.x, toAim.z);
            if (horiz.magnitude <= HEAD_HORIZONTAL_RADIUS && toAim.y > HEAD_HEIGHT_THRESHOLD) {
                return false;
            }


            Vector3 forward = bodyTransform.forward;
            Vector2 f2 = new Vector2(forward.x, forward.z);
            if (f2.sqrMagnitude < 0.0001f) 
                return true;
            f2.Normalize();

            if (horiz.sqrMagnitude < 0.000001f) 
                return true; 
            Vector2 t2 = horiz.normalized;

            float dot = Vector2.Dot(f2, t2);
            if (dot < BACK_DOT_THRESHOLD) 
                return false;

            return true;
        }

        private bool CanAim() {
            if (Body == null) return false;

            if (Camera.radialOpen) return false;
            if (!Body.standing) return false;
            if (Body.consciousness < 10f) return false;
            if (Body.sleeping) return false;
            if (Body.averagePain > 90f) return false;

            return true;
        }

        /// <summary> 
        /// Returns spread value from 0 to 1, where 0 is perfectly accurate and 1 is maximum spread. 
        /// </summary>
        private float GetSpread() {
            float spread = 0f;

            if (!Body.standing)
                spread += 0.2f;

            if(Body.inWater)
                spread += 0.2f;


            spread += Mathf.InverseLerp(0, 100, Body.averagePain) * 0.3f;
            spread += Mathf.InverseLerp(0, 100, 100 - Body.consciousness) * 0.2f;
            spread += Body.brainShakeIntensity * 0.1f;

            if(Body.stamina < 20f)
                spread += Mathf.InverseLerp(20, 0, Body.stamina) * 0.3f;

            spread -= Body.crouching ? 0.3f : 0f;
            spread -= Body.usedNeuralBooster ? 0.2f : 0f;
            spread -= Body.adrenaline > 10f ? 0.2f : 0f;
            spread -= (Body.skills.STR * 0.01f + Body.skills.RES * 0.01f);

            return Mathf.Clamp01(spread);
        }
    }
}