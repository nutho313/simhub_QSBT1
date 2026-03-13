using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace QSBT1Plugin
{
    [PluginDescription("Controls Qubic System QS-BT1 Gain, Sharpness and Deadzone via HTTP API")]
    [PluginAuthor("nutho313.ch")]
    [PluginName("nutho313.ch QS-BT1 ControlMapper")]
    public class QSBT1PluginMain : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private const double STEP              = 0.1;
        private const double BRAKING_GAIN_MIN  = 0.0,  BRAKING_GAIN_MAX  = 2.5;
        private const double BRAKING_SHARP_MIN = 0.0,  BRAKING_SHARP_MAX = 2.5;
        private const double BRAKING_DEAD_MIN  = 0.0,  BRAKING_DEAD_MAX  = 2.5;
        private const double CENT_GAIN_MIN     = -2.5, CENT_GAIN_MAX     = 2.5;
        private const double CENT_SHARP_MIN    = 0.0,  CENT_SHARP_MAX    = 2.5;
        private const double CENT_DEAD_MIN     = 0.0,  CENT_DEAD_MAX     = 2.5;

        public QSBT1Settings Settings;
        private static readonly HttpClient _http = new HttpClient();
        private System.Threading.CancellationTokenSource _pollCts;

        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => null;
        public string LeftMenuTitle => "nutho313 QS-BT1";

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("[QS-BT1] Plugin starting");
            Settings = this.ReadCommonSettings<QSBT1Settings>("QSBT1PluginMain.QSBT1Settings", () => new QSBT1Settings());
            SimHub.Logging.Current.Info("[QS-BT1] Loaded from JSON: B_Gain=" + Settings.Braking_Gain + " C_Gain=" + Settings.Centrifugal_Gain);

            // Read actual values from device
            _pollCts = new System.Threading.CancellationTokenSource();
            Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(2000);
                await ReadFromDeviceAsync();
                await PollDeviceLoopAsync(_pollCts.Token);
            });

            // ── SimHub Properties (visible in dashboards / overlays) ──────────
            // Braking
            this.AttachDelegate("Braking_Gain",               () => Math.Round(Settings.Braking_Gain, 2));
            this.AttachDelegate("Braking_Sharpness",          () => Math.Round(Settings.Braking_Sharpness, 2));
            this.AttachDelegate("Braking_Deadzone",           () => Math.Round(Settings.Braking_Deadzone, 2));
            this.AttachDelegate("Braking_Enabled",            () => Settings.Braking_Enabled);
            // Acceleration
            this.AttachDelegate("Acceleration_Gain",          () => Math.Round(Settings.Acceleration_Gain, 2));
            this.AttachDelegate("Acceleration_Sharpness",     () => Math.Round(Settings.Acceleration_Sharpness, 2));
            this.AttachDelegate("Acceleration_Deadzone",      () => Math.Round(Settings.Acceleration_Deadzone, 2));
            this.AttachDelegate("Acceleration_Enabled",       () => Settings.Acceleration_Enabled);
            // Sideways Acceleration
            this.AttachDelegate("Sideways_Gain",              () => Math.Round(Settings.Sideways_Gain, 2));
            this.AttachDelegate("Sideways_Sharpness",         () => Math.Round(Settings.Sideways_Sharpness, 2));
            this.AttachDelegate("Sideways_Deadzone",          () => Math.Round(Settings.Sideways_Deadzone, 2));
            this.AttachDelegate("Sideways_Enabled",           () => Settings.Sideways_Enabled);
            // Centrifugal Force
            this.AttachDelegate("Centrifugal_Gain",           () => Math.Round(Settings.Centrifugal_Gain, 2));
            this.AttachDelegate("Centrifugal_Sharpness",      () => Math.Round(Settings.Centrifugal_Sharpness, 2));
            this.AttachDelegate("Centrifugal_Deadzone",       () => Math.Round(Settings.Centrifugal_Deadzone, 2));
            this.AttachDelegate("Centrifugal_Enabled",        () => Settings.Centrifugal_Enabled);
            // Bounds
            this.AttachDelegate("Bounds_Neutral",             () => Math.Round(Settings.Bounds_Neutral, 0));
            this.AttachDelegate("Bounds_Maximum",             () => Math.Round(Settings.Bounds_Maximum, 0));
            this.AttachDelegate("Bounds_Enabled",             () => Settings.Bounds_Enabled);
            // Vertical G-Force
            this.AttachDelegate("VerticalG_Gain",             () => Math.Round(Settings.VerticalG_Gain, 2));
            this.AttachDelegate("VerticalG_Sharpness",        () => Math.Round(Settings.VerticalG_Sharpness, 2));
            this.AttachDelegate("VerticalG_Enabled",          () => Settings.VerticalG_Enabled);
            // Side Slip
            this.AttachDelegate("SideSlip_Threshold",         () => Math.Round(Settings.SideSlip_Threshold, 2));
            this.AttachDelegate("SideSlip_Frequency",         () => Math.Round(Settings.SideSlip_Frequency, 0));
            this.AttachDelegate("SideSlip_Intensity",         () => Math.Round(Settings.SideSlip_Intensity, 2));
            this.AttachDelegate("SideSlip_Enabled",           () => Settings.SideSlip_Enabled);
            // Road Harshness
            this.AttachDelegate("RoadHarshness_Gain",         () => Math.Round(Settings.RoadHarshness_Gain, 2));
            this.AttachDelegate("RoadHarshness_Sharpness",    () => Math.Round(Settings.RoadHarshness_Sharpness, 2));
            this.AttachDelegate("RoadHarshness_Enabled",      () => Settings.RoadHarshness_Enabled);
            // Pre-Impact Protection
            this.AttachDelegate("PreImpact_Long",             () => Math.Round(Settings.PreImpact_Long, 0));
            this.AttachDelegate("PreImpact_Lateral",          () => Math.Round(Settings.PreImpact_Lateral, 0));
            this.AttachDelegate("PreImpact_Duration",         () => Math.Round(Settings.PreImpact_Duration, 0));
            this.AttachDelegate("PreImpact_Enabled",          () => Settings.PreImpact_Enabled);

            // ── Control Mapper Actions ────────────────────────────────────────
            // Braking
            this.AddAction("Braking_Gain_Up",                 (a, b) => Adjust("braking", "gain", +1));
            this.AddAction("Braking_Gain_Down",               (a, b) => Adjust("braking", "gain", -1));
            this.AddAction("Braking_Sharpness_Up",            (a, b) => Adjust("braking", "sharpness", +1));
            this.AddAction("Braking_Sharpness_Down",          (a, b) => Adjust("braking", "sharpness", -1));
            this.AddAction("Braking_Deadzone_Up",             (a, b) => Adjust("braking", "deadzone", +1));
            this.AddAction("Braking_Deadzone_Down",           (a, b) => Adjust("braking", "deadzone", -1));
            this.AddAction("Braking_Toggle",                  (a, b) => ToggleEnabled("braking"));
            this.AddAction("Braking_Reset",                   (a, b) => ResetBraking());
            // Acceleration
            this.AddAction("Acceleration_Gain_Up",            (a, b) => Adjust("acceleration", "gain", +1));
            this.AddAction("Acceleration_Gain_Down",          (a, b) => Adjust("acceleration", "gain", -1));
            this.AddAction("Acceleration_Sharpness_Up",       (a, b) => Adjust("acceleration", "sharpness", +1));
            this.AddAction("Acceleration_Sharpness_Down",     (a, b) => Adjust("acceleration", "sharpness", -1));
            this.AddAction("Acceleration_Deadzone_Up",        (a, b) => Adjust("acceleration", "deadzone", +1));
            this.AddAction("Acceleration_Deadzone_Down",      (a, b) => Adjust("acceleration", "deadzone", -1));
            this.AddAction("Acceleration_Toggle",             (a, b) => ToggleEnabled("acceleration"));
            // Sideways Acceleration
            this.AddAction("Sideways_Gain_Up",                (a, b) => Adjust("sideways", "gain", +1));
            this.AddAction("Sideways_Gain_Down",              (a, b) => Adjust("sideways", "gain", -1));
            this.AddAction("Sideways_Sharpness_Up",           (a, b) => Adjust("sideways", "sharpness", +1));
            this.AddAction("Sideways_Sharpness_Down",         (a, b) => Adjust("sideways", "sharpness", -1));
            this.AddAction("Sideways_Deadzone_Up",            (a, b) => Adjust("sideways", "deadzone", +1));
            this.AddAction("Sideways_Deadzone_Down",          (a, b) => Adjust("sideways", "deadzone", -1));
            this.AddAction("Sideways_Toggle",                 (a, b) => ToggleEnabled("sideways"));
            // Centrifugal Force
            this.AddAction("Centrifugal_Gain_Up",             (a, b) => Adjust("centrifugal", "gain", +1));
            this.AddAction("Centrifugal_Gain_Down",           (a, b) => Adjust("centrifugal", "gain", -1));
            this.AddAction("Centrifugal_Sharpness_Up",        (a, b) => Adjust("centrifugal", "sharpness", +1));
            this.AddAction("Centrifugal_Sharpness_Down",      (a, b) => Adjust("centrifugal", "sharpness", -1));
            this.AddAction("Centrifugal_Deadzone_Up",         (a, b) => Adjust("centrifugal", "deadzone", +1));
            this.AddAction("Centrifugal_Deadzone_Down",       (a, b) => Adjust("centrifugal", "deadzone", -1));
            this.AddAction("Centrifugal_Toggle",              (a, b) => ToggleEnabled("centrifugal"));
            this.AddAction("Centrifugal_Reset",               (a, b) => ResetCentrifugal());
            // Bounds
            this.AddAction("Bounds_Neutral_Up",               (a, b) => Adjust("bounds", "neutral", +1));
            this.AddAction("Bounds_Neutral_Down",             (a, b) => Adjust("bounds", "neutral", -1));
            this.AddAction("Bounds_Maximum_Up",               (a, b) => Adjust("bounds", "maximum", +1));
            this.AddAction("Bounds_Maximum_Down",             (a, b) => Adjust("bounds", "maximum", -1));
            this.AddAction("Bounds_Toggle",                   (a, b) => ToggleEnabled("bounds"));
            // Vertical G-Force
            this.AddAction("VerticalG_Gain_Up",               (a, b) => Adjust("verticalg", "gain", +1));
            this.AddAction("VerticalG_Gain_Down",             (a, b) => Adjust("verticalg", "gain", -1));
            this.AddAction("VerticalG_Sharpness_Up",          (a, b) => Adjust("verticalg", "sharpness", +1));
            this.AddAction("VerticalG_Sharpness_Down",        (a, b) => Adjust("verticalg", "sharpness", -1));
            this.AddAction("VerticalG_Toggle",                (a, b) => ToggleEnabled("verticalg"));
            // Side Slip
            this.AddAction("SideSlip_Threshold_Up",           (a, b) => Adjust("sideslip", "threshold", +1));
            this.AddAction("SideSlip_Threshold_Down",         (a, b) => Adjust("sideslip", "threshold", -1));
            this.AddAction("SideSlip_Frequency_Up",           (a, b) => Adjust("sideslip", "frequency", +1));
            this.AddAction("SideSlip_Frequency_Down",         (a, b) => Adjust("sideslip", "frequency", -1));
            this.AddAction("SideSlip_Intensity_Up",           (a, b) => Adjust("sideslip", "intensity", +1));
            this.AddAction("SideSlip_Intensity_Down",         (a, b) => Adjust("sideslip", "intensity", -1));
            this.AddAction("SideSlip_Toggle",                 (a, b) => ToggleEnabled("sideslip"));
            // Road Harshness
            this.AddAction("RoadHarshness_Gain_Up",           (a, b) => Adjust("roadharshness", "gain", +1));
            this.AddAction("RoadHarshness_Gain_Down",         (a, b) => Adjust("roadharshness", "gain", -1));
            this.AddAction("RoadHarshness_Sharpness_Up",      (a, b) => Adjust("roadharshness", "sharpness", +1));
            this.AddAction("RoadHarshness_Sharpness_Down",    (a, b) => Adjust("roadharshness", "sharpness", -1));
            this.AddAction("RoadHarshness_Toggle",            (a, b) => ToggleEnabled("roadharshness"));
            // Pre-Impact Protection
            this.AddAction("PreImpact_Long_Up",               (a, b) => Adjust("preimpact", "long", +1));
            this.AddAction("PreImpact_Long_Down",             (a, b) => Adjust("preimpact", "long", -1));
            this.AddAction("PreImpact_Lateral_Up",            (a, b) => Adjust("preimpact", "lateral", +1));
            this.AddAction("PreImpact_Lateral_Down",          (a, b) => Adjust("preimpact", "lateral", -1));
            this.AddAction("PreImpact_Duration_Up",           (a, b) => Adjust("preimpact", "duration", +1));
            this.AddAction("PreImpact_Duration_Down",         (a, b) => Adjust("preimpact", "duration", -1));
            this.AddAction("PreImpact_Toggle",                (a, b) => ToggleEnabled("preimpact"));
            // Violent Movement Suppressor (Motion Primary | SFX)
            this.AttachDelegate("VMS_Threshold",              () => Math.Round(Settings.VMS_Threshold, 0));
            this.AttachDelegate("VMS_Duration",               () => Math.Round(Settings.VMS_Duration, 2));
            this.AttachDelegate("VMS_Enabled",                () => Settings.VMS_Enabled);
            this.AddAction("VMS_Threshold_Up",                (a, b) => Adjust("vms", "threshold", +1));
            this.AddAction("VMS_Threshold_Down",              (a, b) => Adjust("vms", "threshold", -1));
            this.AddAction("VMS_Duration_Up",                 (a, b) => Adjust("vms", "duration", +1));
            this.AddAction("VMS_Duration_Down",               (a, b) => Adjust("vms", "duration", -1));
            this.AddAction("VMS_Toggle",                      (a, b) => ToggleEnabled("vms"));
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data) { }

        public void End(PluginManager pluginManager)
        {
            _pollCts?.Cancel();
            this.SaveCommonSettings("QSBT1PluginMain.QSBT1Settings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        // ── Adjust ────────────────────────────────────────────────────────────
        public void Adjust(string profile, string param, int dir)
        {
            int pid = Settings.Braking_ProfileId;
            int tg  = Settings.Braking_TuneGroup;
            switch (profile)
            {
                case "braking":
                    switch (param)
                    {
                        case "gain":      Settings.Braking_Gain      = Clamp(Round2(Settings.Braking_Gain      + dir * STEP), BRAKING_GAIN_MIN,  BRAKING_GAIN_MAX);  break;
                        case "sharpness": Settings.Braking_Sharpness = Clamp(Round2(Settings.Braking_Sharpness + dir * STEP), BRAKING_SHARP_MIN, BRAKING_SHARP_MAX); break;
                        case "deadzone":  Settings.Braking_Deadzone  = Clamp(Round2(Settings.Braking_Deadzone  + dir * STEP), BRAKING_DEAD_MIN,  BRAKING_DEAD_MAX);  break;
                    }
                    SendTune("Braking", pid, tg, Settings.Braking_Gain, Settings.Braking_Sharpness, Settings.Braking_Deadzone);
                    break;
                case "acceleration":
                    switch (param)
                    {
                        case "gain":      Settings.Acceleration_Gain      = Clamp(Round2(Settings.Acceleration_Gain      + dir * STEP), BRAKING_GAIN_MIN, BRAKING_GAIN_MAX); break;
                        case "sharpness": Settings.Acceleration_Sharpness = Clamp(Round2(Settings.Acceleration_Sharpness + dir * STEP), BRAKING_SHARP_MIN, BRAKING_SHARP_MAX); break;
                        case "deadzone":  Settings.Acceleration_Deadzone  = Clamp(Round2(Settings.Acceleration_Deadzone  + dir * STEP), BRAKING_DEAD_MIN, BRAKING_DEAD_MAX); break;
                    }
                    SendTune("Acceleration", pid, tg, Settings.Acceleration_Gain, Settings.Acceleration_Sharpness, Settings.Acceleration_Deadzone);
                    break;
                case "sideways":
                    switch (param)
                    {
                        case "gain":      Settings.Sideways_Gain      = Clamp(Round2(Settings.Sideways_Gain      + dir * STEP), CENT_GAIN_MIN, CENT_GAIN_MAX); break;
                        case "sharpness": Settings.Sideways_Sharpness = Clamp(Round2(Settings.Sideways_Sharpness + dir * STEP), BRAKING_SHARP_MIN, BRAKING_SHARP_MAX); break;
                        case "deadzone":  Settings.Sideways_Deadzone  = Clamp(Round2(Settings.Sideways_Deadzone  + dir * STEP), BRAKING_DEAD_MIN, BRAKING_DEAD_MAX); break;
                    }
                    SendTune("Sideways Acceleration", pid, tg, Settings.Sideways_Gain, Settings.Sideways_Sharpness, Settings.Sideways_Deadzone);
                    break;
                case "centrifugal":
                    switch (param)
                    {
                        case "gain":      Settings.Centrifugal_Gain      = Clamp(Round2(Settings.Centrifugal_Gain      + dir * STEP), CENT_GAIN_MIN,  CENT_GAIN_MAX);  break;
                        case "sharpness": Settings.Centrifugal_Sharpness = Clamp(Round2(Settings.Centrifugal_Sharpness + dir * STEP), BRAKING_SHARP_MIN, BRAKING_SHARP_MAX); break;
                        case "deadzone":  Settings.Centrifugal_Deadzone  = Clamp(Round2(Settings.Centrifugal_Deadzone  + dir * STEP), BRAKING_DEAD_MIN, BRAKING_DEAD_MAX); break;
                    }
                    SendTune("Centrifugal Force", pid, tg, Settings.Centrifugal_Gain, Settings.Centrifugal_Sharpness, Settings.Centrifugal_Deadzone);
                    break;
                case "bounds":
                    switch (param)
                    {
                        case "neutral": Settings.Bounds_Neutral = Clamp(Round2(Settings.Bounds_Neutral + dir * 1.0), 0, 30); break;
                        case "maximum": Settings.Bounds_Maximum = Clamp(Round2(Settings.Bounds_Maximum + dir * 1.0), 50, 100); break;
                    }
                    SendTune("Bounds", pid, tg, Settings.Bounds_Neutral, Settings.Bounds_Maximum, 0);
                    break;
                case "verticalg":
                    switch (param)
                    {
                        case "gain":      Settings.VerticalG_Gain      = Clamp(Round2(Settings.VerticalG_Gain      + dir * STEP), CENT_GAIN_MIN, CENT_GAIN_MAX); break;
                        case "sharpness": Settings.VerticalG_Sharpness = Clamp(Round2(Settings.VerticalG_Sharpness + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Vertical G-Force", pid, tg, Settings.VerticalG_Gain, Settings.VerticalG_Sharpness, 0);
                    break;
                case "sideslip":
                    switch (param)
                    {
                        case "threshold": Settings.SideSlip_Threshold = Clamp(Round2(Settings.SideSlip_Threshold + dir * STEP), 0, 30); break;
                        case "frequency": Settings.SideSlip_Frequency = Clamp(Round2(Settings.SideSlip_Frequency + dir * 1.0), 0, 50); break;
                        case "intensity": Settings.SideSlip_Intensity = Clamp(Round2(Settings.SideSlip_Intensity + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Side Slip", pid, tg, Settings.SideSlip_Threshold, Settings.SideSlip_Frequency, Settings.SideSlip_Intensity);
                    break;
                case "roadharshness":
                    switch (param)
                    {
                        case "gain":      Settings.RoadHarshness_Gain      = Clamp(Round2(Settings.RoadHarshness_Gain      + dir * STEP), 0, 2.5); break;
                        case "sharpness": Settings.RoadHarshness_Sharpness = Clamp(Round2(Settings.RoadHarshness_Sharpness + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Road Harshness", pid, tg, Settings.RoadHarshness_Gain, Settings.RoadHarshness_Sharpness, 0);
                    break;
                case "preimpact":
                    switch (param)
                    {
                        case "long":     Settings.PreImpact_Long     = Clamp(Round2(Settings.PreImpact_Long     + dir * 1.0), 4, 100); break;
                        case "lateral":  Settings.PreImpact_Lateral  = Clamp(Round2(Settings.PreImpact_Lateral  + dir * 1.0), 4, 100); break;
                        case "duration": Settings.PreImpact_Duration = Clamp(Round2(Settings.PreImpact_Duration + dir * 10.0), 0, 2000); break;
                    }
                    SendTune("Pre-Impact Protection", pid, tg, Settings.PreImpact_Long, Settings.PreImpact_Lateral, Settings.PreImpact_Duration);
                    break;
                case "vms":
                    switch (param)
                    {
                        case "threshold": Settings.VMS_Threshold = Clamp(Round2(Settings.VMS_Threshold + dir * 1.0), 4, 100); break;
                        case "duration":  Settings.VMS_Duration  = Clamp(Round2(Settings.VMS_Duration  + dir * 1.0), 1, 7);   break;
                    }
                    SendTune("Violent Movement Suppressor", pid, 2, Settings.VMS_Threshold, Settings.VMS_Duration, 0);
                    break;
            }
            this.SaveCommonSettings("QSBT1PluginMain.QSBT1Settings", Settings);
        }

        // ── Reset ─────────────────────────────────────────────────────────────
        private void ResetBraking()
        {
            Settings.Braking_Gain = 1.7; Settings.Braking_Sharpness = 2.5; Settings.Braking_Deadzone = 0.0;
            SendTune("Braking", Settings.Braking_ProfileId, Settings.Braking_TuneGroup,
                Settings.Braking_Gain, Settings.Braking_Sharpness, Settings.Braking_Deadzone);
            this.SaveCommonSettings("QSBT1PluginMain.QSBT1Settings", Settings);
        }

        private void ResetCentrifugal()
        {
            Settings.Centrifugal_Gain = 0.25; Settings.Centrifugal_Sharpness = 2.5; Settings.Centrifugal_Deadzone = 0.0;
            SendTune("Centrifugal Force", Settings.Centrifugal_ProfileId, Settings.Centrifugal_TuneGroup,
                Settings.Centrifugal_Gain, Settings.Centrifugal_Sharpness, Settings.Centrifugal_Deadzone);
            this.SaveCommonSettings("QSBT1PluginMain.QSBT1Settings", Settings);
        }

        // ── Toggle Enabled ────────────────────────────────────────────────────
        private void ToggleEnabled(string profile)
        {
            int pid = Settings.Braking_ProfileId;
            int tg  = Settings.Braking_TuneGroup;
            switch (profile)
            {
                case "braking":       Settings.Braking_Enabled       = !Settings.Braking_Enabled;       SendTuneEnabled("Braking",              pid, tg, Settings.Braking_Enabled);       break;
                case "acceleration":  Settings.Acceleration_Enabled  = !Settings.Acceleration_Enabled;  SendTuneEnabled("Acceleration",          pid, tg, Settings.Acceleration_Enabled);  break;
                case "sideways":      Settings.Sideways_Enabled      = !Settings.Sideways_Enabled;      SendTuneEnabled("Sideways Acceleration", pid, tg, Settings.Sideways_Enabled);      break;
                case "centrifugal":   Settings.Centrifugal_Enabled   = !Settings.Centrifugal_Enabled;   SendTuneEnabled("Centrifugal Force",     pid, tg, Settings.Centrifugal_Enabled);   break;
                case "bounds":        Settings.Bounds_Enabled        = !Settings.Bounds_Enabled;        SendTuneEnabled("Bounds",                pid, tg, Settings.Bounds_Enabled);        break;
                case "verticalg":     Settings.VerticalG_Enabled     = !Settings.VerticalG_Enabled;     SendTuneEnabled("Vertical G-Force",      pid, tg, Settings.VerticalG_Enabled);     break;
                case "sideslip":      Settings.SideSlip_Enabled      = !Settings.SideSlip_Enabled;      SendTuneEnabled("Side Slip",             pid, tg, Settings.SideSlip_Enabled);      break;
                case "roadharshness": Settings.RoadHarshness_Enabled = !Settings.RoadHarshness_Enabled; SendTuneEnabled("Road Harshness",        pid, tg, Settings.RoadHarshness_Enabled); break;
                case "preimpact":     Settings.PreImpact_Enabled     = !Settings.PreImpact_Enabled;     SendTuneEnabled("Pre-Impact Protection", pid, tg, Settings.PreImpact_Enabled);     break;
                case "vms":           Settings.VMS_Enabled           = !Settings.VMS_Enabled;           SendTuneEnabled("Violent Movement Suppressor", pid, 2, Settings.VMS_Enabled);     break;
            }
            this.SaveCommonSettings("QSBT1PluginMain.QSBT1Settings", Settings);
        }

        // ── HTTP SendTune ─────────────────────────────────────────────────────
        private void SendTune(string tuneName, int profileId, int tuneGroup, double gain, double sharpness, double deadzone)
        {
            var ic   = System.Globalization.CultureInfo.InvariantCulture;
            var url  = "http://" + Settings.IpAddress + ":" + Settings.Port + "/api/editTune";
            var json = "{" +
                "\"profileId\":"   + profileId + "," +
                "\"tuneGroup\":"   + tuneGroup + "," +
                "\"tuneName\":\""  + tuneName  + "\"," +
                "\"tuneValue\":["  + gain.ToString(ic) + "," + sharpness.ToString(ic) + "," + deadzone.ToString(ic) + ",0,0,0]" +
                "}";
            Task.Run(async () =>
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await _http.PostAsync(url, content);
                    SimHub.Logging.Current.Info("[QS-BT1] Sent " + tuneName + ": gain=" + gain + " sharp=" + sharpness + " dead=" + deadzone);
                }
                catch (Exception ex) { SimHub.Logging.Current.Error("[QS-BT1] HTTP error: " + ex.Message); }
            });
        }

        public void SendTuneEnabled(string tuneName, int profileId, int tuneGroup, bool enabled)
        {
            var enabledStr = enabled ? "true" : "false";
            var url  = "http://" + Settings.IpAddress + ":" + Settings.Port + "/api/editTuneEnabled";
            var json = "{" +
                "\"profileId\":"   + profileId  + "," +
                "\"tuneGroup\":"   + tuneGroup  + "," +
                "\"tuneName\":\""  + tuneName   + "\"," +
                "\"tuneEnabled\":" + enabledStr +
                "}";
            Task.Run(async () =>
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await _http.PostAsync(url, content);
                    SimHub.Logging.Current.Info("[QS-BT1] TuneEnabled " + tuneName + ": " + enabled);
                }
                catch (Exception ex) { SimHub.Logging.Current.Error("[QS-BT1] HTTP error (enabled): " + ex.Message); }
            });
        }

        public void ActivateProfile(int profileId)
        {
            var url  = "http://" + Settings.IpAddress + ":" + Settings.Port + "/api/activateProfile";
            var json = "{\"profileId\":" + profileId + "}";
            Task.Run(async () =>
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _http.PostAsync(url, content);
                    SimHub.Logging.Current.Info("[QS-BT1] ActivateProfile " + profileId + " → " + (int)response.StatusCode);
                    // After activating, re-read device so UI reflects the new profile's values
                    await System.Threading.Tasks.Task.Delay(600);
                    await ReadFromDeviceAsync();
                }
                catch (Exception ex) { SimHub.Logging.Current.Error("[QS-BT1] ActivateProfile error: " + ex.Message); }
            });
        }

        // ── Read from device at startup ───────────────────────────────────────
        private async Task PollDeviceLoopAsync(System.Threading.CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await System.Threading.Tasks.Task.Delay(3000, ct); } catch { break; }
                if (ct.IsCancellationRequested) break;
                try { await ReadFromDeviceAsync(); }
                catch (Exception ex) { SimHub.Logging.Current.Error("[QS-BT1] Poll error: " + ex.Message); }
            }
        }

        private async Task ReadFromDeviceAsync()
        {
            try
            {
                var urlProfiles = "http://" + Settings.IpAddress + ":" + Settings.Port + "/api/profilesInstalled";
                var jsonProfiles = await _http.GetStringAsync(urlProfiles);

                int activeId = Settings.Braking_ProfileId;
                var idxActive = jsonProfiles.IndexOf("\"is_active\":true");
                if (idxActive > 0)
                {
                    var idxId = jsonProfiles.LastIndexOf("\"id\":", idxActive);
                    if (idxId >= 0)
                    {
                        var start = idxId + 5;
                        var end   = jsonProfiles.IndexOf(',', start);
                        if (int.TryParse(jsonProfiles.Substring(start, end - start).Trim(), out int pid))
                            activeId = pid;
                    }
                }

                var urlDetails  = "http://" + Settings.IpAddress + ":" + Settings.Port + "/api/profile/" + activeId + "/details";
                var jsonDetails = await _http.GetStringAsync(urlDetails);
                ParseAndApplyTunes(jsonDetails, activeId);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("[QS-BT1] ReadFromDevice error: " + ex.Message);
            }
        }

        private void ParseAndApplyTunes(string json, int profileId)
        {
            try
            {
                Settings.Braking_ProfileId     = profileId;
                Settings.Centrifugal_ProfileId = profileId;
                // Braking
                Settings.Braking_Gain          = Round2(GetTuneValue(json, "Braking", 0));
                Settings.Braking_Sharpness     = Round2(GetTuneValue(json, "Braking", 1));
                Settings.Braking_Deadzone      = Round2(GetTuneValue(json, "Braking", 2));
                Settings.Braking_Enabled       = GetTuneEnabled(json, "Braking");
                // Acceleration
                Settings.Acceleration_Gain      = Round2(GetTuneValue(json, "Acceleration", 0));
                Settings.Acceleration_Sharpness = Round2(GetTuneValue(json, "Acceleration", 1));
                Settings.Acceleration_Deadzone  = Round2(GetTuneValue(json, "Acceleration", 2));
                Settings.Acceleration_Enabled   = GetTuneEnabled(json, "Acceleration");
                // Sideways Acceleration
                Settings.Sideways_Gain      = Round2(GetTuneValue(json, "Sideways Acceleration", 0));
                Settings.Sideways_Sharpness = Round2(GetTuneValue(json, "Sideways Acceleration", 1));
                Settings.Sideways_Deadzone  = Round2(GetTuneValue(json, "Sideways Acceleration", 2));
                Settings.Sideways_Enabled   = GetTuneEnabled(json, "Sideways Acceleration");
                // Centrifugal Force
                Settings.Centrifugal_Gain      = Round2(GetTuneValue(json, "Centrifugal Force", 0));
                Settings.Centrifugal_Sharpness = Round2(GetTuneValue(json, "Centrifugal Force", 1));
                Settings.Centrifugal_Deadzone  = Round2(GetTuneValue(json, "Centrifugal Force", 2));
                Settings.Centrifugal_Enabled   = GetTuneEnabled(json, "Centrifugal Force");
                // Bounds
                Settings.Bounds_Neutral = Round2(GetTuneValue(json, "Bounds", 0));
                Settings.Bounds_Maximum = Round2(GetTuneValue(json, "Bounds", 1));
                Settings.Bounds_Enabled = GetTuneEnabled(json, "Bounds");
                // Vertical G-Force
                Settings.VerticalG_Gain      = Round2(GetTuneValue(json, "Vertical G-Force", 0));
                Settings.VerticalG_Sharpness = Round2(GetTuneValue(json, "Vertical G-Force", 1));
                Settings.VerticalG_Enabled   = GetTuneEnabled(json, "Vertical G-Force");
                // Side Slip
                Settings.SideSlip_Threshold = Round2(GetTuneValue(json, "Side Slip", 0));
                Settings.SideSlip_Frequency = Round2(GetTuneValue(json, "Side Slip", 1));
                Settings.SideSlip_Intensity = Round2(GetTuneValue(json, "Side Slip", 2));
                Settings.SideSlip_Enabled   = GetTuneEnabled(json, "Side Slip");
                // Road Harshness
                Settings.RoadHarshness_Gain      = Round2(GetTuneValue(json, "Road Harshness", 0));
                Settings.RoadHarshness_Sharpness = Round2(GetTuneValue(json, "Road Harshness", 1));
                Settings.RoadHarshness_Enabled   = GetTuneEnabled(json, "Road Harshness");
                // Pre-Impact Protection
                Settings.PreImpact_Long     = Round2(GetTuneValue(json, "Pre-Impact Protection", 0));
                Settings.PreImpact_Lateral  = Round2(GetTuneValue(json, "Pre-Impact Protection", 1));
                Settings.PreImpact_Duration = Round2(GetTuneValue(json, "Pre-Impact Protection", 2));
                Settings.PreImpact_Enabled  = GetTuneEnabled(json, "Pre-Impact Protection");
                // Violent Movement Suppressor (Motion Primary | SFX — tuneGroup 2)
                Settings.VMS_Threshold = Round2(GetTuneValue(json, "Violent Movement Suppressor", 0));
                Settings.VMS_Duration  = Round2(GetTuneValue(json, "Violent Movement Suppressor", 1));
                Settings.VMS_Enabled   = GetTuneEnabled(json, "Violent Movement Suppressor");

                SimHub.Logging.Current.Info("[QS-BT1] Read from device: B_Gain=" + Settings.Braking_Gain + " C_Gain=" + Settings.Centrifugal_Gain);
                Settings.NotifyAll();
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("[QS-BT1] ParseTunes error: " + ex.Message);
            }
        }

        // Scan tune objects (each starts with {"canBeDisabledByUser") and find the one
        // whose "name" field exactly matches tuneName — safe against partial matches in descriptions.
        private int FindTuneObjectStart(string json, string tuneName)
        {
            var boundary = "{\"canBeDisabledByUser\"";
            var nameTag  = "\"name\":\"" + tuneName + "\"";
            int pos = 0;
            while (pos < json.Length)
            {
                int objStart = json.IndexOf(boundary, pos);
                if (objStart < 0) break;
                int searchLen = Math.Min(800, json.Length - objStart);
                if (json.IndexOf(nameTag, objStart, searchLen) >= 0)
                    return objStart;
                pos = objStart + 1;
            }
            return -1;
        }

        private double GetTuneValue(string json, string tuneName, int index)
        {
            var ic       = System.Globalization.CultureInfo.InvariantCulture;
            int objStart = FindTuneObjectStart(json, tuneName);
            if (objStart < 0) return 0;
            var cvIdx = json.IndexOf("\"currentValue\":[", objStart, Math.Min(600, json.Length - objStart));
            if (cvIdx < 0) return 0;
            var start  = cvIdx + 16;
            var end    = json.IndexOf(']', start);
            if (end < 0) return 0;
            var values = json.Substring(start, end - start).Split(',');
            if (index >= values.Length) return 0;
            return double.TryParse(values[index].Trim(), System.Globalization.NumberStyles.Float, ic, out double v) ? v : 0;
        }

        private bool GetTuneEnabled(string json, string tuneName)
        {
            int objStart = FindTuneObjectStart(json, tuneName);
            if (objStart < 0) return true;
            var enIdx = json.IndexOf("\"enabled\":", objStart, Math.Min(600, json.Length - objStart));
            if (enIdx < 0) return true;
            return json.Substring(enIdx + 10, 5).TrimEnd(',', ' ', '}').StartsWith("true");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static double Round1(double v) => Math.Round(v, 1);
        private static double Round2(double v) => Math.Round(v, 2);
        private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
    }
}
