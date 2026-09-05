using Game.Client.Match;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class MatchTimerViewTests
    {
        [Test]
        public void SetRemainingSeconds_KeepsHidingTimerLookBeforeThirtySeconds()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = CreateView(canvas.transform);
                view.SetRemainingSeconds(185d);

                var timer = view.GetComponent<TMP_Text>();
                Assert.That(timer.text, Is.EqualTo("03:05"));
                Assert.That(timer.fontSize, Is.EqualTo(HidingActiveHudView.TimerFontSize));
                Assert.That(timer.color, Is.EqualTo(Color.white));

                var hint = view.transform.Find("Hint");
                Assert.That(hint, Is.Not.Null);
                Assert.That(hint.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetRemainingSeconds_UsesBlackSixtyFourAndHintFromThirtySeconds()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = CreateView(canvas.transform);

                view.SetRemainingSeconds(31d);
                var timer = view.GetComponent<TMP_Text>();
                Assert.That(timer.text, Is.EqualTo("00:31"));
                Assert.That(timer.fontSize, Is.EqualTo(HidingActiveHudView.TimerFontSize));
                Assert.That(timer.color, Is.EqualTo(Color.white));
                Assert.That(view.transform.Find("Hint").gameObject.activeSelf, Is.False);
                Assert.That(MatchTimerView.IsWarning(31d), Is.False);

                view.SetRemainingSeconds(30d);
                Assert.That(timer.text, Is.EqualTo("00:30"));
                Assert.That(timer.fontSize, Is.EqualTo(MatchTimerView.TimerFontSize));
                Assert.That(timer.color, Is.EqualTo(MatchTimerView.TimerColor));

                var hint = view.transform.Find("Hint")?.GetComponent<TMP_Text>();
                Assert.That(hint, Is.Not.Null);
                Assert.That(hint.gameObject.activeSelf, Is.True);
                Assert.That(hint.text, Is.EqualTo(MatchTimerView.HintText));
                Assert.That(hint.fontSize, Is.EqualTo(MatchTimerView.HintFontSize));
                Assert.That(hint.color, Is.EqualTo(MatchTimerView.WarningColor));
                Assert.That(MatchTimerView.IsWarning(30d), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SetHintVisible_HidesThePromptEvenInTheLastThirtySeconds()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = CreateView(canvas.transform);
                view.SetRemainingSeconds(20d);
                view.SetHintVisible(false);

                Assert.That(view.transform.Find("Hint").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        private static MatchTimerView CreateView(Transform parent)
        {
            var textObject = new GameObject(
                "TimerText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.AddComponent<MatchTimerView>();
        }
    }
}
