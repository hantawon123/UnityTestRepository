using System;
using System.Collections.Generic;
using Game.Client.Home;
using Game.Core.Ports;
using Game.Core.Rooms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Client.Match
{
    public sealed class MatchReportView : MonoBehaviour, IMatchReportView
    {
        public static bool IsOpen { get; private set; }
        public event Action OpenRequested;
        public event Action Closed;
        public event Action<string, ReportReason, string> Submitted;
        private GameObject panel;
        private RectTransform targets;
        private TMP_Text status;
        private TMP_Text selection;
        private TMP_InputField note;
        private Button submit;
        private string targetId;
        private string targetName = "미선택";
        private string reasonName = "미선택";
        private ReportReason reason;
        private CursorLockMode previousLock;
        private bool previousVisible;

        public static MatchReportView Create(Transform parent)
        {
            var root = new GameObject("Player report", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return root.AddComponent<MatchReportView>();
        }

        private void Awake()
        {
            var entry = Rect("Open report", transform, new Vector2(1650, 50), new Vector2(220, 48));
            Button(entry, "플레이어 신고 (F8)", () => OpenRequested?.Invoke());
            var shade = Rect("Report dialog", transform, Vector2.zero, new Vector2(1920, 1080));
            shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .92f);
            panel = shade.gameObject;
            Label(Rect("Title", shade, new Vector2(420, 130), new Vector2(1080, 50)), "플레이어 신고 · 신고는 자동 차단이나 상대방 알림을 발생시키지 않습니다.");
            targets = Rect("Targets", shade, new Vector2(420, 210), new Vector2(1080, 60));
            selection = Label(Rect("Selection", shade, new Vector2(420, 290), new Vector2(1080, 50)), "참가자와 사유를 선택해 주세요.");
            var reasons = new[] { "욕설·괴롭힘", "부정행위", "광고·도배", "부적절한 닉네임", "기타" };
            for (var i = 0; i < reasons.Length; i++)
            {
                var index = i;
                Button(Rect("Reason", shade, new Vector2(420 + i * 215, 360), new Vector2(205, 50)), reasons[i], () =>
                {
                    reason = (ReportReason)index;
                    reasonName = reasons[index];
                    ShowSelection();
                });
            }
            var input = Rect("Memo", shade, new Vector2(420, 445), new Vector2(1080, 140));
            input.gameObject.AddComponent<Image>().color = new Color(.15f, .15f, .15f, 1);
            note = input.gameObject.AddComponent<TMP_InputField>();
            note.textViewport = input;
            note.textComponent = Label(Rect("Text", input, new Vector2(12, 12), new Vector2(1056, 116)), string.Empty);
            note.textComponent.richText = false;
            note.placeholder = Label(Rect("Placeholder", input, new Vector2(12, 12), new Vector2(1056, 116)), "메모 (선택, 최대 200자)");
            note.characterLimit = 200;
            note.lineType = TMP_InputField.LineType.MultiLineNewline;
            status = Label(Rect("Status", shade, new Vector2(420, 610), new Vector2(1080, 80)), string.Empty);
            submit = Button(Rect("Submit", shade, new Vector2(1060, 740), new Vector2(210, 55)), "신고 제출", () => Submitted?.Invoke(targetId, reason, note.text));
            Button(Rect("Close", shade, new Vector2(1290, 740), new Vector2(210, 55)), "닫기", Close);
            panel.SetActive(false);
        }

        public void Open(IReadOnlyList<RoomParticipant> participants)
        {
            if (!IsOpen)
            {
                previousLock = Cursor.lockState;
                previousVisible = Cursor.visible;
            }
            IsOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            panel.SetActive(true);
            foreach (Transform child in targets) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            targetId = null;
            targetName = reasonName = "미선택";
            reason = (ReportReason)(-1);
            note.SetTextWithoutNotify(string.Empty);
            selection.text = "참가자와 사유를 선택해 주세요.";
            for (var i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                Button(Rect("Target", targets, new Vector2(i * 215, 0), new Vector2(205, 55)), participant.Nickname, () =>
                {
                    targetId = participant.PlayerId;
                    targetName = participant.Nickname;
                    ShowSelection();
                });
            }
            SetStatus(participants.Count == 0 ? "신고 가능한 참가자가 없습니다. 계정 연결 후 같은 버전으로 입장해 주세요." : string.Empty, participants.Count > 0);
        }

        private void ShowSelection() => selection.text = $"대상: {targetName} · 사유: {reasonName}";

        public void SetStatus(string message, bool canSubmit)
        {
            status.text = message;
            submit.interactable = canSubmit;
        }

        private void Update()
        {
            if (Keyboard.current?.f8Key.wasPressedThisFrame == true)
            {
                if (IsOpen) Close();
                else OpenRequested?.Invoke();
            }
            else if (IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close();
        }

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            panel.SetActive(false);
            note.SetTextWithoutNotify(string.Empty);
            EventSystem.current?.SetSelectedGameObject(null);
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            Closed?.Invoke();
        }

        private void OnDisable() { if (panel != null) Close(); }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Label(RectTransform rect, string text)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = HomeUiFonts.Apply();
            label.fontSize = 24;
            label.text = text;
            label.richText = false;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static Button Button(RectTransform rect, string text, Action clicked)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.22f, .25f, .32f, 1);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => clicked());
            Label(Rect("Label", rect, Vector2.zero, rect.sizeDelta), text);
            return button;
        }
    }
}
