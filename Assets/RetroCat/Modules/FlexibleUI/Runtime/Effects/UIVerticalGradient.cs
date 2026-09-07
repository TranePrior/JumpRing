using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.FlexibleUI.Runtime.Effects
{
    /// <summary>
    /// Tints a graphic with a vertical gradient by multiplying its vertex colors.
    /// </summary>
    /// <remarks>
    /// The alternative — a separate gradient graphic clipped to shape by a stencil <see cref="Mask"/> —
    /// produces hard, aliased edges, because stencil clipping is a binary per-pixel test. Modulating the
    /// vertex colors of the shaped graphic itself keeps the sprite's own antialiased silhouette intact.
    /// </remarks>
    [AddComponentMenu("UI/Effects/Vertical Gradient")]
    [DisallowMultipleComponent]
    public sealed class UIVerticalGradient : BaseMeshEffect
    {
        [SerializeField] private Color _topColor = Color.white;
        [SerializeField] private Color _bottomColor = Color.white;

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (IsActive() == false)
            {
                return;
            }

            Rect rect = graphic.rectTransform.rect;
            UIVertex vertex = default;

            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);

                float verticalPosition = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                Color tint = Color.Lerp(_bottomColor, _topColor, verticalPosition);

                vertex.color = (Color)vertex.color * tint;
                vertexHelper.SetUIVertex(vertex, i);
            }
        }
    }
}
