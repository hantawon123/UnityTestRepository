using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.Home
{
    /// <summary>
    /// Repaints a fill and shows a hairline while the pointer is over it.
    /// </summary>
    /// <remarks>
    /// The profile chip and the two icon buttons change two things at once on
    /// hover, and a <see cref="Selectable"/> colour tint only changes one: it
    /// multiplies a single graphic. Rather than tint the fill and leave the
    /// stroke to some second mechanism, both live here.
    /// <para>
    /// Exit is also raised when the object is disabled underneath the pointer,
    /// so a chip hidden while hovered does not come back lit.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HomeHoverHighlight : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private Image fill;
        private Image stroke;
        private Color normalColor;
        private Color hoverColor;

        public void Bind(Image fillImage, Image strokeImage, Color normal, Color hover)
        {
            fill = fillImage;
            stroke = strokeImage;
            normalColor = normal;
            hoverColor = hover;
            Apply(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Apply(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Apply(false);
        }

        private void OnDisable()
        {
            Apply(false);
        }

        private void Apply(bool hovered)
        {
            if (fill != null)
            {
                fill.color = hovered ? hoverColor : normalColor;
            }

            if (stroke != null)
            {
                stroke.enabled = hovered;
            }
        }
    }
}
