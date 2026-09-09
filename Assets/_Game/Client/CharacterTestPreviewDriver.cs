using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Client
{
    [DisallowMultipleComponent]
    public sealed class CharacterTestPreviewDriver : MonoBehaviour
    {
        [SerializeField]
        private string[] stateNames =
        {
            "Idle_Breathing",
            "Walk_Wide_Clean",
            "Walk_Back",
            "Walk_Left",
            "Walk_Right",
            "Run_SideArms",
            "Run_Back",
            "Run_Left",
            "Run_Right",
            "Jump_Cute",
            "Fall_Flutter",
            "Land_Matched",
            "Crouch_Idle_KneesUp",
            "Crouch_Walk_Forward_KneesUp",
            "Stand_To_Crouch_KneesUp",
            "Crouch_To_Stand_KneesUp",
            "Crouch_Walk_Left_KneesUp",
            "Crouch_Walk_Right_KneesUp",
            "Crouch_Walk_Back_KneesUp",
            "Pickup_Low",
            "Carry_Idle",
            "Carry_Walk",
            "Carry_Walk_Back",
            "Carry_Walk_Left",
            "Carry_Walk_Right",
            "Carry_Run",
            "Carry_Run_Back",
            "Carry_Run_Left",
            "Carry_Run_Right",
            "Carry_Crouch",
            "Carry_Crouch_Walk_Forward",
            "Carry_Crouch_Walk_Back",
            "Carry_Crouch_Walk_Left",
            "Carry_Crouch_Walk_Right",
            "Carry_Prone",
            "Carry_Crawl_Forward",
            "Carry_Crawl_Back",
            "Carry_Crawl_Left",
            "Carry_Crawl_Right",
            "PutDown_Low",
            "Throw",
            "Prone_Start",
            "Prone_Idle",
            "Crawl_Forward",
            "Crawl_Back",
            "Crawl_Left",
            "Crawl_Right",
            "Prone_End",
            "Crouch_To_Prone",
            "Prone_To_Crouch",
        };

        private Vector2 listScroll;

        private static readonly string[] FootNames =
        {
            "FootToe1.L",
            "FootToe1.R",
            "Foot.L",
            "Foot.R",
        };

        private readonly List<Transform> feet = new();
        private Animator animator;
        private SkinnedMeshRenderer body;
        private int crouchGroinIndex = -1;
        private Transform orbitCamera;
        private int index;
        private Vector3 standPosition;
        private Quaternion standRotation;
        private float plantedFootY = 0.02f;
        private bool planted;
        private float yaw;
        private float pitch;
        private float distance = 5f;
        private float startYaw;
        private float startPitch;
        private float startDistance = 5f;

        public void ConfigureMotions(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                throw new System.ArgumentException("At least one preview motion is required.", nameof(names));
            }

            stateNames = (string[])names.Clone();
            index = 0;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            CacheFeet(transform);
            CacheCrouchGroin();
            if (stateNames == null || stateNames.Length < 50)
            {
                stateNames = new[]
                {
                    "Idle_Breathing",
                    "Walk_Wide_Clean",
                    "Walk_Back",
                    "Walk_Left",
                    "Walk_Right",
                    "Run_SideArms",
                    "Run_Back",
                    "Run_Left",
                    "Run_Right",
                    "Jump_Cute",
                    "Fall_Flutter",
                    "Land_Matched",
                    "Crouch_Idle_KneesUp",
                    "Crouch_Walk_Forward_KneesUp",
                    "Stand_To_Crouch_KneesUp",
                    "Crouch_To_Stand_KneesUp",
                    "Crouch_Walk_Left_KneesUp",
                    "Crouch_Walk_Right_KneesUp",
                    "Crouch_Walk_Back_KneesUp",
                    "Pickup_Low",
                    "Carry_Idle",
                    "Carry_Walk",
                    "Carry_Walk_Back",
                    "Carry_Walk_Left",
                    "Carry_Walk_Right",
                    "Carry_Run",
                    "Carry_Run_Back",
                    "Carry_Run_Left",
                    "Carry_Run_Right",
                    "Carry_Crouch",
                    "Carry_Crouch_Walk_Forward",
                    "Carry_Crouch_Walk_Back",
                    "Carry_Crouch_Walk_Left",
                    "Carry_Crouch_Walk_Right",
                    "Carry_Prone",
                    "Carry_Crawl_Forward",
                    "Carry_Crawl_Back",
                    "Carry_Crawl_Left",
                    "Carry_Crawl_Right",
                    "PutDown_Low",
                    "Throw",
                    "Prone_Start",
                    "Prone_Idle",
                    "Crawl_Forward",
                    "Crawl_Back",
                    "Crawl_Left",
                    "Crawl_Right",
                    "Prone_End",
                    "Crouch_To_Prone",
                    "Prone_To_Crouch",
                };
            }
            standRotation = transform.rotation;
            standPosition = transform.position;
            CaptureOrbit();
            Play(0);
        }

        private void Start()
        {
            PlantIdleFeet();
            CaptureOrbit();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressed(keyboard.rightArrowKey) || WasPressed(keyboard.periodKey))
            {
                Play((index + 1) % stateNames.Length);
                return;
            }

            if (WasPressed(keyboard.leftArrowKey) || WasPressed(keyboard.commaKey))
            {
                Play((index + stateNames.Length - 1) % stateNames.Length);
                return;
            }

            if (TryDigit(keyboard, out var digit))
            {
                Play(digit);
            }

            UpdateOrbit(keyboard);
        }

        private void LateUpdate()
        {
            var pos = transform.position;
            pos.x = standPosition.x;
            pos.z = standPosition.z;
            transform.SetPositionAndRotation(pos, standRotation);

            if (TryMinFootY(out var footY))
            {
                if (!planted)
                {
                    PlantIdleFeet();
                }
                else if (IsAirborne(stateNames[index]))
                {
                    if (footY < plantedFootY)
                    {
                        transform.position += Vector3.up * (plantedFootY - footY);
                    }
                }
                else if (!IsProne(stateNames[index]))
                {
                    transform.position += Vector3.up * (plantedFootY - footY);
                }
            }

            ApplyOrbit();
            ApplyStandingGroin();
        }

        private void Play(int next)
        {
            index = Mathf.Clamp(next, 0, stateNames.Length - 1);
            if (animator == null)
            {
                return;
            }

            animator.Play(stateNames[index], 0, 0f);
            ApplyStandingGroin();
        }

        private void PlantIdleFeet()
        {
            if (!TryMinFootY(out var footY))
            {
                return;
            }

            transform.position += Vector3.up * (0.02f - footY);
            if (!TryMinFootY(out plantedFootY))
            {
                plantedFootY = 0.02f;
            }

            standPosition = transform.position;
            planted = true;
        }

        private void CacheCrouchGroin()
        {
            body = GetComponentInChildren<SkinnedMeshRenderer>(true);
            crouchGroinIndex = -1;
            if (body == null || body.sharedMesh == null)
            {
                return;
            }

            for (var i = 0; i < body.sharedMesh.blendShapeCount; i++)
            {
                if (body.sharedMesh.GetBlendShapeName(i) == "Crouch_Groin_Flat")
                {
                    crouchGroinIndex = i;
                    return;
                }
            }
        }

        private void ApplyStandingGroin()
        {
            if (body == null || crouchGroinIndex < 0)
            {
                return;
            }

            if (stateNames[index].IndexOf("Crouch") < 0)
            {
                body.SetBlendShapeWeight(crouchGroinIndex, 0f);
            }
        }

        private void CacheFeet(Transform current)
        {
            for (var i = 0; i < FootNames.Length; i++)
            {
                if (current.name == FootNames[i])
                {
                    feet.Add(current);
                    break;
                }
            }

            for (var i = 0; i < current.childCount; i++)
            {
                CacheFeet(current.GetChild(i));
            }
        }

        private bool TryMinFootY(out float footY)
        {
            footY = float.PositiveInfinity;
            var found = false;
            for (var i = 0; i < feet.Count; i++)
            {
                if (feet[i] == null)
                {
                    continue;
                }

                footY = Mathf.Min(footY, feet[i].position.y);
                found = true;
            }

            return found;
        }

        private void CaptureOrbit()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            orbitCamera = camera.transform;
            var lookAt = LookAtPoint();
            var offset = orbitCamera.position - lookAt;
            distance = Mathf.Clamp(offset.magnitude, 1.5f, 12f);
            if (distance < 0.01f)
            {
                distance = 5f;
            }

            yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(offset.y / distance, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -25f, 89f);
            startYaw = yaw;
            startPitch = pitch;
            startDistance = distance;
        }

        private void UpdateOrbit(Keyboard keyboard)
        {
            if (WasPressed(keyboard.rKey))
            {
                yaw = startYaw;
                pitch = startPitch;
                distance = startDistance;
            }

            var step = 90f * Time.deltaTime;
            if (keyboard.qKey.isPressed)
            {
                yaw -= step;
            }

            if (keyboard.eKey.isPressed)
            {
                yaw += step;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.15f;
                pitch = Mathf.Clamp(pitch - delta.y * 0.15f, -25f, 89f);
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance * (1f - scroll * 0.001f), 1.5f, 12f);
            }
        }

        private void ApplyOrbit()
        {
            if (orbitCamera == null)
            {
                return;
            }

            var lookAt = LookAtPoint();
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var position = lookAt + rotation * (Vector3.forward * distance);
            orbitCamera.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }

        private Vector3 LookAtPoint()
        {
            return standPosition + Vector3.up * 1.15f;
        }

        private static bool IsAirborne(string state)
        {
            return state == "Jump_Cute" || state == "Fall_Flutter";
        }

        private static bool IsProne(string state)
        {
            return state.IndexOf("Prone", System.StringComparison.Ordinal) >= 0
                || state.IndexOf("Crawl", System.StringComparison.Ordinal) >= 0;
        }

        private static bool TryDigit(Keyboard keyboard, out int digit)
        {
            if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key)) { digit = 0; return true; }
            if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key)) { digit = 1; return true; }
            if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key)) { digit = 2; return true; }
            if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key)) { digit = 3; return true; }
            if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key)) { digit = 4; return true; }
            if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key)) { digit = 5; return true; }
            if (WasPressed(keyboard.digit7Key) || WasPressed(keyboard.numpad7Key)) { digit = 6; return true; }
            if (WasPressed(keyboard.digit8Key) || WasPressed(keyboard.numpad8Key)) { digit = 7; return true; }
            if (WasPressed(keyboard.digit9Key) || WasPressed(keyboard.numpad9Key)) { digit = 8; return true; }
            if (WasPressed(keyboard.digit0Key) || WasPressed(keyboard.numpad0Key)) { digit = 9; return true; }
            digit = -1;
            return false;
        }

        private static bool WasPressed(KeyControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }

        private void OnGUI()
        {
            const int pad = 16;
            const int width = 420;
            const int header = 62;
            const int row = 28;
            var maxHeight = Mathf.Max(220, Screen.height - pad * 2);
            var needed = header + 8 + stateNames.Length * row;
            var boxHeight = Mathf.Min(needed, maxHeight);
            GUI.Box(new Rect(pad, pad, width, boxHeight), string.Empty);
            var ready = animator != null && animator.runtimeAnimatorController != null;
            GUI.Label(
                new Rect(pad + 8, pad + 6, width - 16, 50),
                $"지금: {stateNames[index]}  |  {gameObject.name} {(ready ? "OK" : "없음")}\n" +
                "우클릭 드래그 / Q E 회전  |  휠 줌  |  R 리셋");

            var scrollRect = new Rect(pad + 4, pad + header, width - 8, boxHeight - header - 8);
            var content = new Rect(0, 0, width - 36, stateNames.Length * row);
            listScroll = GUI.BeginScrollView(scrollRect, listScroll, content);
            for (var i = 0; i < stateNames.Length; i++)
            {
                if (GUI.Button(new Rect(4, i * row, width - 44, 26), $"{i + 1}. {stateNames[i]}"))
                {
                    Play(i);
                }
            }

            GUI.EndScrollView();
        }
    }
}
