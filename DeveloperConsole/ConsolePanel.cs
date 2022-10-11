using UniverseLib.UI;
using UniverseLib.UI.Panels;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Config;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using UniverseLib;

namespace DeveloperConsole
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
            _image.color = new Color(0.2f, 0.2f, 0.2f);

            TextHolder.Component.readOnly = true;
            TextHolder.Component.textComponent.supportRichText = true;
            TextHolder.Component.lineType = InputField.LineType.MultiLineNewline;
            TextHolder.Component.textComponent.font = UniversalUI.ConsoleFont;
            TextHolder.PlaceholderText.font = UniversalUI.ConsoleFont;

            return UIRoot;
        }
    }

    internal class ConsolePanel : PanelBase, ICellPoolDataSource<MessageCell>
    {
        public ConsolePanel(UIBase owner) : base(owner) { }

        public override string Name => "Developer Console";
        public override int MinWidth => 750;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        private InputFieldRef _inputField;
        private ScrollPool<MessageCell> _consoleScrollPool;

        public bool InputFocused => _inputField?.Component?.isFocused ?? false;
        public int ItemCount => ConsoleManager.NumLogs;

        private bool _pressedEnter = false;
        private string _lastCommand = null;

        protected override void ConstructPanelContent()
        {
            UIFactory.CreatePanel("TextPanel", ContentRoot, out var panelContent, new Color(0.5f, 0.5f, 0.5f));

            _consoleScrollPool = UIFactory.CreateScrollPool<MessageCell>(panelContent, "LogCells", out var scrollObj, out var scrollContent, new Color(0.03f, 0.03f, 0.03f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);

            _consoleScrollPool.Initialize(this);

            DeveloperConsole.Manager.LogChanged += (object sender, EventArgs args) =>
            {
                _consoleScrollPool.Refresh(true, false);
                _consoleScrollPool.JumpToIndex(ItemCount - 1, null);
            };

            var inputRow = UIFactory.CreateHorizontalGroup(ContentRoot, "InputRow", false, false, true, true, 8, new Vector4(3, 3, 5, 5),
                default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(inputRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 99999);

            _inputField = UIFactory.CreateInputField(inputRow, "CommandInput", "Enter Command...");
            UIFactory.SetLayoutElement(_inputField.Component.gameObject, minHeight: 28, minWidth: 100, flexibleHeight: 0, flexibleWidth: 99999);

            _inputField.Component.textComponent.fontSize = 16;
            _inputField.PlaceholderText.fontSize = 16;

            _inputField.Component.textComponent.font = UniversalUI.ConsoleFont;
            _inputField.PlaceholderText.font = UniversalUI.ConsoleFont;

            var submitButton = UIFactory.CreateButton(inputRow, "SubmitButton", "Submit", new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(submitButton.Component.gameObject, minHeight: 28, minWidth: 120, flexibleHeight: 0);
            submitButton.ButtonText.fontSize = 15;
            submitButton.ButtonText.font = UniversalUI.ConsoleFont;

            submitButton.OnClick += SubmitCommand;
        }

        private readonly Color logEvenColor = new(0.3f, 0.3f, 0.3f);
        private readonly Color logOddColor = new(0.2f, 0.2f, 0.2f);
        public void OnCellBorrowed(MessageCell cell) { }
        
        public void SetCell(MessageCell cell, int index)
        {
            if (index >= ItemCount)
            {
                cell.Disable();
                return;
            }

            cell.TextHolder.Text = ConsoleManager.GetLog(index);

            Color color = index % 2 == 0 ? logEvenColor : logOddColor;
            RuntimeHelper.SetColorBlock(cell.TextHolder.Component, color);
            cell.BackgroundColor = color;
        }

        public void ProcessInput()
        {
            if (InputFocused)
            {
                bool keysDown = UniverseLib.Input.InputManager.GetKey(KeyCode.Return) || UniverseLib.Input.InputManager.GetKey(KeyCode.KeypadEnter);

                if (!keysDown)
                {
                    _pressedEnter = false;
                }
                else if (!_pressedEnter)
                {
                    SubmitCommand();
                    _pressedEnter = true;
                }

                if (_lastCommand != null && UniverseLib.Input.InputManager.GetKey(KeyCode.UpArrow))
                {
                    _inputField.Text = _lastCommand;
                    _inputField.Component.caretPosition = _inputField.Text.Length;
                }
            }
            else if (UniverseLib.Input.InputManager.GetKeyDown(KeyCode.BackQuote))
            {
                OnToggled();
            }
        }

        private void OnToggled()
        {
            Owner.Enabled = Enabled = !Owner.Enabled;
            ConsoleInputPatch.InConsole = Enabled;

            ConfigManager.Force_Unlock_Mouse = Enabled;                
        }

        protected override void OnClosePanelClicked()
        {
            OnToggled();
        }

        public void SubmitCommand()
        {
            void OnSuccess()
            {
                DeveloperConsole.Manager.Log("> " + _inputField.Text);
                _lastCommand = _inputField.Text;
                _inputField.Text = "";
            }

            string[] allArgs = _inputField.Text.Trim().Split(' ');

            string name = allArgs.First();
            string[] args = allArgs.Skip(1).ToArray();

            if (args.Length == 0)
            {
                var getResult = (ValueResult)DeveloperConsole.Manager.GetStringValue(name, out var value, true);
                if (getResult == ValueResult.Success)
                {
                    OnSuccess();
                    DeveloperConsole.Manager.Log($"{name} = {value}");
                    return;
                }

                // If we know the convar, we can leave
                if (getResult != ValueResult.UnknownValue)
                    return;
            }
            else if (args.Length == 1)
            {
                var setResult = (ValueResult)DeveloperConsole.Manager.SetValue(name, args[0], true);
                if (setResult == ValueResult.Success)
                {
                    OnSuccess();
                    return;
                }

                // If we know the convar, we can leave
                if (setResult != ValueResult.UnknownValue)
                    return;
            }

            var result = (RunCommandResult)DeveloperConsole.Manager.RunCommand(name, args, false);

            // Clear if valid command/args
            if (result == RunCommandResult.Success)
            {
                OnSuccess();
            } 
        }
    }
}
