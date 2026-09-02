using Qoooo.VJ.Composition;
using UnityEngine;
using UnityEngine.UIElements;

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class VjPreviewPresenter : MonoBehaviour
    {
        private const float PreviewSortingOrder = 0f;

        [SerializeField] private FinalCompositeRenderer source;
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField, Range(0f, 0.25f)] private float margin = 0.03f;

        private UIDocument _document;
        private Image _previewImage;
        private RenderTexture _displayedTexture;

        public FinalCompositeRenderer Source
        {
            get => source;
            set => source = value;
        }

        public PanelSettings PanelSettings
        {
            get => panelSettings;
            set => panelSettings = value;
        }

        private void Start()
        {
            _document = VjRuntimePanelFactory.CreateDocument(
                transform,
                "VJ Preview",
                panelSettings,
                PreviewSortingOrder);

            var root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _previewImage = new Image
            {
                name = "vj-preview-image",
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            _previewImage.style.position = Position.Absolute;
            _previewImage.style.left = Length.Percent(margin * 100f);
            _previewImage.style.right = Length.Percent(margin * 100f);
            _previewImage.style.top = Length.Percent(margin * 100f);
            _previewImage.style.bottom = Length.Percent(margin * 100f);
            root.Add(_previewImage);
        }

        private void LateUpdate()
        {
            var texture = source != null ? source.FinalTexture : null;
            if (texture == _displayedTexture || _previewImage == null) return;
            _displayedTexture = texture;
            _previewImage.image = texture;
        }

        private void OnDestroy()
        {
            VjRuntimePanelFactory.DestroyOwned(
                _document != null ? _document.gameObject : null);
        }
    }
}
