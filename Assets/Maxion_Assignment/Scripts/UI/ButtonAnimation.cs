using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.UI
{
    public static class ButtonAnimation
    {
        // Squash-and-bounce on click: the button shrinks by `strength` first, then wobbles back to its original scale.
        // Clicking again mid-animation snaps the previous punch to its end first, so the scale never drifts.
        public static Tween PlayClick(Component target, float strength = 0.1f, float duration = 0.25f)
        {
            var t = target.transform;
            t.DOKill(true);
            return t.DOPunchScale(Vector3.one * -strength, duration, 6, 0.5f)
                .SetUpdate(true)
                .SetLink(t.gameObject);
        }

        // Plays PlayClick every time the button is clicked.
        public static void AddClickAnimation(this Button button, float strength = 0.1f, float duration = 0.25f)
        {
            button.onClick.AddListener(() => PlayClick(button, strength, duration));
        }
    }
}
