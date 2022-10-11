using UnityEngine.UI;
using UnityEngine;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.UI;

namespace DeveloperConsole.UI
{
    internal class MessageCell : ICell
    {
        private Image _image;
        public InputFieldRef TextHolder;

        public Color BackgroundColor
        {
            get => _image.color;
            set => _image.color = value;
        }

        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public float DefaultHeight => 25;

        public bool Enabled => UIRoot.activeInHierarchy;
        public void Enable() => UIRoot.SetActive(true);
        public void Disable() => UIRoot.SetActive(false);

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("MessageCell", parent, new Vector2(DefaultHeight, DefaultHeight));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 3);
            UIFactory.SetLayoutElement(UIRoot, minHeight: (int)DefaultHeight, minWidth: (int)DefaultHeight, flexibleWidth: 9999);

            TextHolder = UIFactory.CreateInputField(UIRoot, "Message", "");
            UIFactory.SetLayoutElement(TextHolder.GameObject, minHeight: (int)DefaultHeight, minWidth: (int)DefaultHeight, flexibleWidth: 9999);

            _image = TextHolder.Component.GetComponent<Image>();
            _image.color = new Color(0.4f, 0.4f, 0.4f);

            TextHolder.Component.readOnly = true;
            TextHolder.Component.textComponent.supportRichText = true;
            TextHolder.Component.lineType = InputField.LineType.MultiLineNewline;
            TextHolder.Component.textComponent.font = UniversalUI.ConsoleFont;
            TextHolder.PlaceholderText.font = UniversalUI.ConsoleFont;

            return UIRoot;
        }
    }
}
