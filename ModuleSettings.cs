using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Settings;
using Blish_HUD.Settings.UI.Views;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace BhModule.PathingMapAlignPlugin
{
    public class ModuleSettings
    {
        public SettingEntry<int> X { get; private set; }
        public SettingEntry<int> Y { get; private set; }
        public SettingEntry<int> Width { get; private set; }
        public SettingEntry<int> Height { get; private set; }
        public SettingEntry<float> Scale { get; private set; }
        public ModuleSettings(PathingMapAlignPluginModule module, SettingCollection settings)
        {
            var min = -300;
            var max = 300;
            X = settings.DefineSetting(nameof(X), 0, () => $"Mini Map X <{X.Value}>", () => "");
            X.SetRange(min, max);
            X.SettingChanged += OnSettingChanged;
            Y = settings.DefineSetting(nameof(Y), 0, () => $"Mini Map Y <{Y.Value}>", () => "");
            Y.SetRange(min, max);
            Y.SettingChanged += OnSettingChanged;
            Width = settings.DefineSetting(nameof(Width), 0, () => $"Mini Map Width <{Width.Value}>", () => "");
            Width.SetRange(min, max);
            Width.SettingChanged += OnSettingChanged;
            Height = settings.DefineSetting(nameof(Height), 0, () => $"Mini Map Height <{Height.Value}>", () => "");
            Height.SetRange(min, max);
            Height.SettingChanged += OnSettingChanged;
            Scale = settings.DefineSetting(nameof(Scale), 1f, () => $"Marker Scale <{Math.Round(Scale.Value, 2)}>", () => "Keep adjusting until the far side markers are aligned.");
            Scale.SetRange(1f, 3f);
            Scale.SettingChanged += OnSettingChanged;
        }
        void OnSettingChanged(object target, EventArgs e)
        {
            PathingMapAlignPluginSettingsView.UpdateTitles?.Invoke();
        }
        public void Reset()
        {
            X.Value = 0;
            Y.Value = 0;
            Width.Value = 0;
            Height.Value = 0;
            Scale.Value = 1;
        }
        public void Unload()
        {
            PathingMapAlignPluginSettingsView.DisposeRootFlowPanel?.Invoke();
            X.SettingChanged -= OnSettingChanged;
            Y.SettingChanged -= OnSettingChanged;
            Width.SettingChanged -= OnSettingChanged;
            Height.SettingChanged -= OnSettingChanged;
            Scale.SettingChanged -= OnSettingChanged;
        }
    }
    public class PathingMapAlignPluginSettingsView(SettingCollection settings) : View
    {
        static public Action DisposeRootFlowPanel;
        static public Action UpdateTitles;
        FlowPanel rootFlowPanel;
        readonly SettingCollection settings = settings;
        protected override void Build(Container buildPanel)
        {
            DisposeRootFlowPanel?.Invoke();
            rootFlowPanel = new FlowPanel()
            {
                Size = buildPanel.Size,
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(10, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };
            DisposeRootFlowPanel = () =>
            {
                UpdateTitles = null;
                DisposeRootFlowPanel = null;
                rootFlowPanel.Dispose();
            };
            var resetBtn = new StandardButton() { Parent = rootFlowPanel, Text = "Reset" };
            resetBtn.Click += (s, e) =>
            {
                PathingMapAlignPluginModule.Instance.Settings.Reset();
            };
            foreach (var setting in settings.Where(s => s.SessionDefined))
            {
                IView settingView;

                if ((settingView = SettingView.FromType(setting, rootFlowPanel.Width)) != null)
                {
                    ViewContainer container = new()
                    {
                        WidthSizingMode = SizingMode.Fill,
                        HeightSizingMode = SizingMode.AutoSize,
                        Parent = rootFlowPanel
                    };
                    if (setting is SettingEntry<float> settingFloat && settingView is FloatSettingView settingViewFloat)
                    {
                        UpdateTitles += () =>
                        {
                            settingViewFloat.DisplayName = settingFloat.GetDisplayNameFunc();
                        };
                    }
                    else if (setting is SettingEntry<int> settingInt && settingView is IntSettingView settingViewInt)
                    {
                        UpdateTitles += () =>
                        {
                            settingViewInt.DisplayName = settingInt.GetDisplayNameFunc();
                        };
                    }
                    container.Show(settingView);
                }
            }
        }
    }
}
