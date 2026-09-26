using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.UI
{
    // Base for full-screen popups. Every time the GameObject is enabled, the backdrop fades in while the popup scales up.
    // Show/Hide just toggle the GameObject, so switching popups replays the open animation.
    public abstract class PopupView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] RectTransform popup;
        [SerializeField] Image backdrop;
        [SerializeField, Min(0f)] float openDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] float startScale = 0.8f;

        CanvasGroup popupGroup;
        float backdropAlpha;
        Sequence openSequence;

        protected virtual void Awake()
        {
            if (!popup.TryGetComponent(out popupGroup))
                popupGroup = popup.gameObject.AddComponent<CanvasGroup>();
            if (backdrop != null)
                backdropAlpha = backdrop.color.a;
        }

        protected virtual void OnEnable() => PlayOpen();

        protected virtual void OnDisable() => openSequence?.Kill(true);

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public Tween PlayOpen()
        {
            openSequence?.Kill();

            popup.localScale = Vector3.one * startScale;
            popupGroup.alpha = 0f;
            popupGroup.blocksRaycasts = false;

            openSequence = DOTween.Sequence()
                .Join(popup.DOScale(1f, openDuration).SetEase(Ease.OutBack))
                .Join(popupGroup.DOFade(1f, openDuration * 0.6f))
                .OnComplete(() => popupGroup.blocksRaycasts = true)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (backdrop != null)
            {
                backdrop.color = new Color(backdrop.color.r, backdrop.color.g, backdrop.color.b, 0f);
                openSequence.Join(backdrop.DOFade(backdropAlpha, openDuration * 0.6f));
            }

            return openSequence;
        }
    }
}
