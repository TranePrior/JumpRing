using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class BackgroundTiler : MonoBehaviour
    {
        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private Texture2D tileTexture;

        [SerializeField, Range(0.1f, 5f)]
        private float tileScale = 1f;

        [SerializeField]
        private Color tintColor = new(0.1f, 0.22f, 0.16f, 0.35f);

        private MeshRenderer meshRenderer;
        private Material tileMaterial;

        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                tileMaterial = meshRenderer.material;
                if (tileTexture != null)
                {
                    tileMaterial.SetTexture(BaseMap, tileTexture);
                }
                tileMaterial.SetColor(BaseColor, tintColor);
            }
        }

        private void LateUpdate()
        {
            if (gameplayCamera == null) return;

            FollowCamera();
            UpdateScale();
        }

        private void FollowCamera()
        {
            var camPos = gameplayCamera.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }

        private void UpdateScale()
        {
            var orthoSize = gameplayCamera.orthographicSize;
            var aspect = gameplayCamera.aspect;

            var worldHeight = orthoSize * 2f + 2f;
            var worldWidth = worldHeight * aspect + 2f;

            transform.localScale = new Vector3(worldWidth, worldHeight, 1f);

            if (tileMaterial != null && tileTexture != null)
            {
                var texAspect = (float)tileTexture.width / tileTexture.height;
                var tilesX = worldWidth / (tileScale * texAspect);
                var tilesY = worldHeight / tileScale;
                tileMaterial.SetTextureScale(BaseMap, new Vector2(tilesX, tilesY));
            }
        }

        public void SetBackground(Texture2D texture, Color color)
        {
            if (tileMaterial == null) return;

            if (texture != null)
            {
                tileTexture = texture;
                tileMaterial.SetTexture(BaseMap, texture);
            }

            tintColor = color;
            tileMaterial.SetColor(BaseColor, color);
        }

        private void OnDestroy()
        {
            if (tileMaterial != null)
            {
                Destroy(tileMaterial);
            }
        }
    }
}
