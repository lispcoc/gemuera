// Gemuera — In-game settings screen
// Attach to Settings.tscn root CanvasLayer node.
// Opened/closed by ConsoleNode via ShowSettings() / HideSettings().
using Godot;
using System;
using EraConfig = MinorShift.Emuera.Runtime.Config.Config;
using EraConfigData = MinorShift.Emuera.Runtime.Config.ConfigData;
using EraConfigCode = MinorShift.Emuera.Runtime.Config.ConfigCode;
using EraColorVal = MinorShift.Emuera.EraColor;

namespace Gemuera.UI;

/// <summary>
/// Settings overlay.  All ERA config reads/writes go through EraConfigData.Instance.
/// Gemuera-native settings (BGM/SE volume) are stored in user://gemuera_settings.cfg.
/// After closing, saves config and notifies ConsoleNode to re-apply font / volume.
/// </summary>
public partial class SettingsNode : CanvasLayer
{
    // ----------------------------------------------------------------
    // Emitted when the player closes the panel (ConsoleNode listens to re-apply settings).
    // ----------------------------------------------------------------
    [Signal] public delegate void SettingsClosedEventHandler();

    // ----------------------------------------------------------------
    // Reference to ConsoleNode (set by ConsoleNode.ShowSettings)
    // ----------------------------------------------------------------
    private ConsoleNode _console;

    // ----------------------------------------------------------------
    // Child references (matched by name in Settings.tscn)
    // ----------------------------------------------------------------
    private TabContainer      _tabs;
    private Button            _closeBtn;

    // Display tab
    private SpinBox           _fontSizeSpin;
    private LineEdit          _fontNameEdit;
    private ColorPickerButton _foreColorBtn;
    private ColorPickerButton _backColorBtn;
    private ColorPickerButton _focusColorBtn;

    // Sound tab
    private HSlider           _bgmSlider;
    private HSlider           _seSlider;
    private Label             _bgmLabel;
    private Label             _seLabel;

    // System tab
    private SpinBox           _maxLogSpin;
    private CheckBox          _displayReportCheck;

    // ----------------------------------------------------------------
    // Persistent Gemuera-native settings path
    // ----------------------------------------------------------------
    private const string GemueraSettingsCfg = "user://gemuera_settings.cfg";

    // ----------------------------------------------------------------
    // Godot lifecycle
    // ----------------------------------------------------------------

    public override void _Ready()
    {
        _tabs     = GetNode<TabContainer>("Panel/VBox/Tabs");
        _closeBtn = GetNode<Button>("Panel/VBox/CloseBtn");

        var dispTab  = _tabs.GetChild(0);
        _fontSizeSpin  = dispTab.GetNode<SpinBox>("FontSizeRow/FontSizeSpin");
        _fontNameEdit  = dispTab.GetNode<LineEdit>("FontNameRow/FontNameEdit");
        _foreColorBtn  = dispTab.GetNode<ColorPickerButton>("ForeColorRow/ForeColorBtn");
        _backColorBtn  = dispTab.GetNode<ColorPickerButton>("BackColorRow/BackColorBtn");
        _focusColorBtn = dispTab.GetNode<ColorPickerButton>("FocusColorRow/FocusColorBtn");

        var soundTab = _tabs.GetChild(1);
        _bgmSlider  = soundTab.GetNode<HSlider>("BgmRow/BgmSlider");
        _seSlider   = soundTab.GetNode<HSlider>("SeRow/SeSlider");
        _bgmLabel   = soundTab.GetNode<Label>("BgmRow/BgmVal");
        _seLabel    = soundTab.GetNode<Label>("SeRow/SeVal");

        var sysTab = _tabs.GetChild(2);
        _maxLogSpin         = sysTab.GetNode<SpinBox>("MaxLogRow/MaxLogSpin");
        _displayReportCheck = sysTab.GetNode<CheckBox>("DisplayReportRow/DisplayReportCheck");

        _closeBtn.Pressed       += OnClosePressed;
        _bgmSlider.ValueChanged += v =>
        {
            _bgmLabel.Text = ((int)v).ToString();
            _console?.SetUserBgmVolume((int)v);
        };
        _seSlider.ValueChanged  += v =>
        {
            _seLabel.Text = ((int)v).ToString();
            _console?.SetUserSeVolume((int)v);
        };

        Hide();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!Visible) return;
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            OnClosePressed();
        }
    }

    // ----------------------------------------------------------------
    // Open the panel
    // ----------------------------------------------------------------

    public void ShowSettings(ConsoleNode console)
    {
        _console = console;
        PopulateFromConfig();
        Visible = true;
    }

    private void PopulateFromConfig()
    {
        // Display
        _fontSizeSpin.Value  = EraConfig.FontSize;
        _fontNameEdit.Text   = EraConfig.FontName ?? "MS Gothic";
        _foreColorBtn.Color  = EraColorToGodot(EraConfig.ForeColor);
        _backColorBtn.Color  = EraColorToGodot(EraConfig.BackColor);
        _focusColorBtn.Color = EraColorToGodot(EraConfig.FocusColor);

        // Sound — load from gemuera_settings.cfg
        int bgm = LoadVolumeSetting("bgm_volume", 80);
        int se  = LoadVolumeSetting("se_volume",  80);
        _bgmSlider.Value = bgm;
        _seSlider.Value  = se;
        _bgmLabel.Text   = bgm.ToString();
        _seLabel.Text    = se.ToString();

        // System
        _maxLogSpin.Value           = EraConfig.MaxLog;
        _displayReportCheck.ButtonPressed = EraConfig.DisplayReport;
    }

    // ----------------------------------------------------------------
    // On close: apply and save
    // ----------------------------------------------------------------

    private void OnClosePressed()
    {
        ApplyToConfig();
        Visible = false;
        EmitSignal(SignalName.SettingsClosed);
    }

    private void ApplyToConfig()
    {
        var data = EraConfigData.Instance;

        // Display
        SetConfigValue<int>   (data, EraConfigCode.FontSize,  (int)_fontSizeSpin.Value);
        SetConfigValue<string>(data, EraConfigCode.FontName,  _fontNameEdit.Text.Trim());
        SetConfigValue<EraColorVal>(data, EraConfigCode.ForeColor,  GodotToEraColor(_foreColorBtn.Color));
        SetConfigValue<EraColorVal>(data, EraConfigCode.BackColor,  GodotToEraColor(_backColorBtn.Color));
        SetConfigValue<EraColorVal>(data, EraConfigCode.FocusColor, GodotToEraColor(_focusColorBtn.Color));

        // System
        SetConfigValue<int> (data, EraConfigCode.MaxLog,        (int)_maxLogSpin.Value);
        SetConfigValue<bool>(data, EraConfigCode.DisplayReport, _displayReportCheck.ButtonPressed);

        // Refresh in-memory Config static properties
        EraConfig.SetConfig(data);

        // Persist ERA config to emuera.config
        TrySaveEraConfig();

        // Sound — save Gemuera-native settings
        int bgm = (int)_bgmSlider.Value;
        int se  = (int)_seSlider.Value;
        SaveNativeSetting("bgm_volume", bgm);
        SaveNativeSetting("se_volume",  se);

        // Notify ConsoleNode to apply live changes
        _console?.ApplyConfigFont();
        _console?.SetUserBgmVolume(bgm);
        _console?.SetUserSeVolume(se);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static void SetConfigValue<T>(EraConfigData data, EraConfigCode code, T value)
    {
        try
        {
            var item = data.GetItem(code) as MinorShift.Emuera.Runtime.Config.ConfigItem<T>;
            if (item != null) item.Value = value;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Settings] SetConfigValue {code}: {e.Message}");
        }
    }

    private static void TrySaveEraConfig()
    {
        try
        {
            // On Android/Web save to user:// (res:// is read-only).
            // Pass the target path explicitly so Program.ExeDir is not mutated.
            string platform = OS.GetName().ToLowerInvariant();
            bool mobile = platform == "android" || platform == "web" || platform == "html5";
            string targetPath = mobile
                ? ProjectSettings.GlobalizePath("user://emuera.config")
                : MinorShift.Emuera.Program.ExeDir + "emuera.config";
            EraConfigData.Instance.SaveConfig(targetPath);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Settings] TrySaveEraConfig: {e.Message}");
        }
    }

    /// <summary>Load a Gemuera native setting value (e.g. volume) from the persistent cfg file.</summary>
    public static int LoadVolumeSetting(string key, int defaultVal)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(GemueraSettingsCfg) == Error.Ok)
        {
            var variant = cfg.GetValue("gemuera", key, defaultVal);
            return variant.AsInt32();
        }
        return defaultVal;
    }

    private static void SaveNativeSetting(string key, int value)
    {
        var cfg = new ConfigFile();
        cfg.Load(GemueraSettingsCfg); // ignore error — file may not exist yet
        cfg.SetValue("gemuera", key, value);
        cfg.Save(GemueraSettingsCfg);
    }

    private static Color EraColorToGodot(EraColorVal c)
        => new Color(c.R / 255f, c.G / 255f, c.B / 255f, 1f);

    private static EraColorVal GodotToEraColor(Color c)
        => EraColorVal.FromArgb((int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255));
}

