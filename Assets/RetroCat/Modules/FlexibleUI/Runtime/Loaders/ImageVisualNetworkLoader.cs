using System.Threading.Tasks;
using RetroCat.Modules.FlexibleUI.Runtime.Indicators;
using RetroCat.Modules.Network;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.FlexibleUI.Runtime.Loaders
{
    public class ImageVisualNetworkLoader : MonoBehaviour
    {
        [SerializeField] private LoadIndicatorBase _loadIndicator;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Color _unloadedImageColor = Color.white;
        [SerializeField] private Color _loadedImageColor = Color.white;

        public async Task Load(string url)
        {
            _iconImage.color = _unloadedImageColor;
            _loadIndicator?.SetProgress(0f);
            _loadIndicator?.StartLoading();
            Sprite sprite = await SpriteNetworkLoader.LoadSpriteAsync(url);
            _iconImage.color = _loadedImageColor;
            _iconImage.sprite = sprite;
            _loadIndicator?.SetProgress(1f);
            _loadIndicator?.StopLoading();
        }
    }
}
