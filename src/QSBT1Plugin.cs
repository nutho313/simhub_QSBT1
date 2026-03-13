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
            // Vehicle Parameters (tuneGroup 13)
            this.AttachDelegate("VP_RevLimiter_MaxGear",      () => Math.Round(Settings.VP_RevLimiter_MaxGear, 2));
            this.AttachDelegate("VP_RevLimiter_Offset",       () => Math.Round(Settings.VP_RevLimiter_Offset, 0));
            this.AttachDelegate("VP_RevLimiter_Enabled",      () => Settings.VP_RevLimiter_Enabled);
            this.AttachDelegate("VP_WheelFwdSlip_Front",      () => Math.Round(Settings.VP_WheelFwdSlip_Front, 0));
            this.AttachDelegate("VP_WheelFwdSlip_Rear",       () => Math.Round(Settings.VP_WheelFwdSlip_Rear, 0));
            this.AttachDelegate("VP_WheelFwdSlip_Enabled",    () => Settings.VP_WheelFwdSlip_Enabled);
            this.AttachDelegate("VP_SlipAngle_Front",         () => Math.Round(Settings.VP_SlipAngle_Front, 2));
            this.AttachDelegate("VP_SlipAngle_Rear",          () => Math.Round(Settings.VP_SlipAngle_Rear, 2));
            this.AttachDelegate("VP_SlipAngle_Escalation",    () => Math.Round(Settings.VP_SlipAngle_Escalation, 2));
            this.AttachDelegate("VP_SlipAngle_Enabled",       () => Settings.VP_SlipAngle_Enabled);
            this.AttachDelegate("VP_WheelBase",               () => Math.Round(Settings.VP_WheelBase, 2));
            this.AttachDelegate("VP_TurningCircle",           () => Math.Round(Settings.VP_TurningCircle, 2));
            this.AttachDelegate("VP_CoGBias",                 () => Math.Round(Settings.VP_CoGBias, 2));
            this.AddAction("VP_RevLimiter_MaxGear_Up",        (a, b) => Adjust("vp_revlimiter", "maxgear", +1));
            this.AddAction("VP_RevLimiter_MaxGear_Down",      (a, b) => Adjust("vp_revlimiter", "maxgear", -1));
            this.AddAction("VP_RevLimiter_Offset_Up",         (a, b) => Adjust("vp_revlimiter", "offset", +1));
            this.AddAction("VP_RevLimiter_Offset_Down",       (a, b) => Adjust("vp_revlimiter", "offset", -1));
            this.AddAction("VP_RevLimiter_Toggle",            (a, b) => ToggleEnabled("vp_revlimiter"));
            this.AddAction("VP_WheelFwdSlip_Front_Up",        (a, b) => Adjust("vp_wheelfwdslip", "front", +1));
            this.AddAction("VP_WheelFwdSlip_Front_Down",      (a, b) => Adjust("vp_wheelfwdslip", "front", -1));
            this.AddAction("VP_WheelFwdSlip_Rear_Up",         (a, b) => Adjust("vp_wheelfwdslip", "rear", +1));
            this.AddAction("VP_WheelFwdSlip_Rear_Down",       (a, b) => Adjust("vp_wheelfwdslip", "rear", -1));
            this.AddAction("VP_WheelFwdSlip_Toggle",          (a, b) => ToggleEnabled("vp_wheelfwdslip"));
            this.AddAction("VP_SlipAngle_Front_Up",           (a, b) => Adjust("vp_slipangle", "front", +1));
            this.AddAction("VP_SlipAngle_Front_Down",         (a, b) => Adjust("vp_slipangle", "front", -1));
            this.AddAction("VP_SlipAngle_Rear_Up",            (a, b) => Adjust("vp_slipangle", "rear", +1));
            this.AddAction("VP_SlipAngle_Rear_Down",          (a, b) => Adjust("vp_slipangle", "rear", -1));
            this.AddAction("VP_SlipAngle_Escalation_Up",      (a, b) => Adjust("vp_slipangle", "escalation", +1));
            this.AddAction("VP_SlipAngle_Escalation_Down",    (a, b) => Adjust("vp_slipangle", "escalation", -1));
            this.AddAction("VP_SlipAngle_Toggle",             (a, b) => ToggleEnabled("vp_slipangle"));
            this.AddAction("VP_WheelBase_Up",                 (a, b) => Adjust("vp_wheelbase", "", +1));
            this.AddAction("VP_WheelBase_Down",               (a, b) => Adjust("vp_wheelbase", "", -1));
            this.AddAction("VP_TurningCircle_Up",             (a, b) => Adjust("vp_turningcircle", "", +1));
            this.AddAction("VP_TurningCircle_Down",           (a, b) => Adjust("vp_turningcircle", "", -1));
            this.AddAction("VP_CoGBias_Up",                   (a, b) => Adjust("vp_cogbias", "", +1));
            this.AddAction("VP_CoGBias_Down",                 (a, b) => Adjust("vp_cogbias", "", -1));
            // Seat Belt Tensioner | SFX (tuneGroup 17)
            this.AttachDelegate("SFX_RevLimiter_Freq",        () => Math.Round(Settings.SFX_RevLimiter_Freq, 0));
            this.AttachDelegate("SFX_RevLimiter_Intensity",   () => Math.Round(Settings.SFX_RevLimiter_Intensity, 2));
            this.AttachDelegate("SFX_RevLimiter_Enabled",     () => Settings.SFX_RevLimiter_Enabled);
            this.AttachDelegate("SFX_GearChange_Duration",    () => Math.Round(Settings.SFX_GearChange_Duration, 0));
            this.AttachDelegate("SFX_GearChange_Downshift",   () => Math.Round(Settings.SFX_GearChange_Downshift, 2));
            this.AttachDelegate("SFX_GearChange_Upshift",     () => Math.Round(Settings.SFX_GearChange_Upshift, 2));
            this.AttachDelegate("SFX_GearChange_Enabled",     () => Settings.SFX_GearChange_Enabled);
            this.AttachDelegate("SFX_WheelFwdSlip_Freq",      () => Math.Round(Settings.SFX_WheelFwdSlip_Freq, 0));
            this.AttachDelegate("SFX_WheelFwdSlip_Intensity", () => Math.Round(Settings.SFX_WheelFwdSlip_Intensity, 2));
            this.AttachDelegate("SFX_WheelFwdSlip_Enabled",   () => Settings.SFX_WheelFwdSlip_Enabled);
            this.AttachDelegate("SFX_WheelSlipAngle_Freq",    () => Math.Round(Settings.SFX_WheelSlipAngle_Freq, 0));
            this.AttachDelegate("SFX_WheelSlipAngle_Intensity",() => Math.Round(Settings.SFX_WheelSlipAngle_Intensity, 2));
            this.AttachDelegate("SFX_WheelSlipAngle_Enabled", () => Settings.SFX_WheelSlipAngle_Enabled);
            this.AttachDelegate("SFX_RumbleStrips_FreqLow",   () => Math.Round(Settings.SFX_RumbleStrips_FreqLow, 2));
            this.AttachDelegate("SFX_RumbleStrips_FreqHigh",  () => Math.Round(Settings.SFX_RumbleStrips_FreqHigh, 2));
            this.AttachDelegate("SFX_RumbleStrips_IntLow",    () => Math.Round(Settings.SFX_RumbleStrips_IntLow, 2));
            this.AttachDelegate("SFX_RumbleStrips_IntHigh",   () => Math.Round(Settings.SFX_RumbleStrips_IntHigh, 2));
            this.AttachDelegate("SFX_RumbleStrips_Enabled",   () => Settings.SFX_RumbleStrips_Enabled);
            this.AttachDelegate("SFX_ABS_Freq",               () => Math.Round(Settings.SFX_ABS_Freq, 0));
            this.AttachDelegate("SFX_ABS_Intensity",          () => Math.Round(Settings.SFX_ABS_Intensity, 2));
            this.AttachDelegate("SFX_ABS_Enabled",            () => Settings.SFX_ABS_Enabled);
            this.AttachDelegate("SFX_EngVib_Extra_Phase",     () => Math.Round(Settings.SFX_EngVib_Extra_Phase, 0));
            this.AttachDelegate("SFX_EngVib_Extra_Alone",     () => Math.Round(Settings.SFX_EngVib_Extra_Alone, 2));
            this.AttachDelegate("SFX_EngVib_Extra_InGroup",   () => Math.Round(Settings.SFX_EngVib_Extra_InGroup, 2));
            this.AttachDelegate("SFX_LFE_Val0",               () => Math.Round(Settings.SFX_LFE_Val0, 2));
            this.AttachDelegate("SFX_LFE_Val1",               () => Math.Round(Settings.SFX_LFE_Val1, 2));
            this.AttachDelegate("SFX_LFE_Enabled",            () => Settings.SFX_LFE_Enabled);
            this.AddAction("SFX_RevLimiter_Freq_Up",          (a, b) => Adjust("sfx_revlimiter", "freq", +1));
            this.AddAction("SFX_RevLimiter_Freq_Down",        (a, b) => Adjust("sfx_revlimiter", "freq", -1));
            this.AddAction("SFX_RevLimiter_Intensity_Up",     (a, b) => Adjust("sfx_revlimiter", "intensity", +1));
            this.AddAction("SFX_RevLimiter_Intensity_Down",   (a, b) => Adjust("sfx_revlimiter", "intensity", -1));
            this.AddAction("SFX_RevLimiter_Toggle",           (a, b) => ToggleEnabled("sfx_revlimiter"));
            this.AddAction("SFX_GearChange_Duration_Up",      (a, b) => Adjust("sfx_gearchange", "duration", +1));
            this.AddAction("SFX_GearChange_Duration_Down",    (a, b) => Adjust("sfx_gearchange", "duration", -1));
            this.AddAction("SFX_GearChange_Downshift_Up",     (a, b) => Adjust("sfx_gearchange", "downshift", +1));
            this.AddAction("SFX_GearChange_Downshift_Down",   (a, b) => Adjust("sfx_gearchange", "downshift", -1));
            this.AddAction("SFX_GearChange_Upshift_Up",       (a, b) => Adjust("sfx_gearchange", "upshift", +1));
            this.AddAction("SFX_GearChange_Upshift_Down",     (a, b) => Adjust("sfx_gearchange", "upshift", -1));
            this.AddAction("SFX_GearChange_Toggle",           (a, b) => ToggleEnabled("sfx_gearchange"));
            this.AddAction("SFX_WheelFwdSlip_Freq_Up",        (a, b) => Adjust("sfx_wheelfwdslip", "freq", +1));
            this.AddAction("SFX_WheelFwdSlip_Freq_Down",      (a, b) => Adjust("sfx_wheelfwdslip", "freq", -1));
            this.AddAction("SFX_WheelFwdSlip_Intensity_Up",   (a, b) => Adjust("sfx_wheelfwdslip", "intensity", +1));
            this.AddAction("SFX_WheelFwdSlip_Intensity_Down", (a, b) => Adjust("sfx_wheelfwdslip", "intensity", -1));
            this.AddAction("SFX_WheelFwdSlip_Toggle",         (a, b) => ToggleEnabled("sfx_wheelfwdslip"));
            this.AddAction("SFX_WheelSlipAngle_Freq_Up",      (a, b) => Adjust("sfx_wheelslipangle", "freq", +1));
            this.AddAction("SFX_WheelSlipAngle_Freq_Down",    (a, b) => Adjust("sfx_wheelslipangle", "freq", -1));
            this.AddAction("SFX_WheelSlipAngle_Intensity_Up", (a, b) => Adjust("sfx_wheelslipangle", "intensity", +1));
            this.AddAction("SFX_WheelSlipAngle_Intensity_Down",(a, b) => Adjust("sfx_wheelslipangle", "intensity", -1));
            this.AddAction("SFX_WheelSlipAngle_Toggle",       (a, b) => ToggleEnabled("sfx_wheelslipangle"));
            this.AddAction("SFX_RumbleStrips_FreqLow_Up",     (a, b) => Adjust("sfx_rumblestrips", "freqlow", +1));
            this.AddAction("SFX_RumbleStrips_FreqLow_Down",   (a, b) => Adjust("sfx_rumblestrips", "freqlow", -1));
            this.AddAction("SFX_RumbleStrips_FreqHigh_Up",    (a, b) => Adjust("sfx_rumblestrips", "freqhigh", +1));
            this.AddAction("SFX_RumbleStrips_FreqHigh_Down",  (a, b) => Adjust("sfx_rumblestrips", "freqhigh", -1));
            this.AddAction("SFX_RumbleStrips_IntLow_Up",      (a, b) => Adjust("sfx_rumblestrips", "intlow", +1));
            this.AddAction("SFX_RumbleStrips_IntLow_Down",    (a, b) => Adjust("sfx_rumblestrips", "intlow", -1));
            this.AddAction("SFX_RumbleStrips_IntHigh_Up",     (a, b) => Adjust("sfx_rumblestrips", "inthigh", +1));
            this.AddAction("SFX_RumbleStrips_IntHigh_Down",   (a, b) => Adjust("sfx_rumblestrips", "inthigh", -1));
            this.AddAction("SFX_RumbleStrips_Toggle",         (a, b) => ToggleEnabled("sfx_rumblestrips"));
            this.AddAction("SFX_ABS_Freq_Up",                 (a, b) => Adjust("sfx_abs", "freq", +1));
            this.AddAction("SFX_ABS_Freq_Down",               (a, b) => Adjust("sfx_abs", "freq", -1));
            this.AddAction("SFX_ABS_Intensity_Up",            (a, b) => Adjust("sfx_abs", "intensity", +1));
            this.AddAction("SFX_ABS_Intensity_Down",          (a, b) => Adjust("sfx_abs", "intensity", -1));
            this.AddAction("SFX_ABS_Toggle",                  (a, b) => ToggleEnabled("sfx_abs"));
            this.AddAction("SFX_EngVib_Phase_Up",             (a, b) => Adjust("sfx_engvib_extra", "phase", +1));
            this.AddAction("SFX_EngVib_Phase_Down",           (a, b) => Adjust("sfx_engvib_extra", "phase", -1));
            this.AddAction("SFX_EngVib_Alone_Up",             (a, b) => Adjust("sfx_engvib_extra", "alone", +1));
            this.AddAction("SFX_EngVib_Alone_Down",           (a, b) => Adjust("sfx_engvib_extra", "alone", -1));
            this.AddAction("SFX_EngVib_InGroup_Up",           (a, b) => Adjust("sfx_engvib_extra", "ingroup", +1));
            this.AddAction("SFX_EngVib_InGroup_Down",         (a, b) => Adjust("sfx_engvib_extra", "ingroup", -1));
            this.AddAction("SFX_LFE_Val0_Up",                 (a, b) => Adjust("sfx_lfe", "val0", +1));
            this.AddAction("SFX_LFE_Val0_Down",               (a, b) => Adjust("sfx_lfe", "val0", -1));
            this.AddAction("SFX_LFE_Val1_Up",                 (a, b) => Adjust("sfx_lfe", "val1", +1));
            this.AddAction("SFX_LFE_Val1_Down",               (a, b) => Adjust("sfx_lfe", "val1", -1));
            this.AddAction("SFX_LFE_Toggle",                  (a, b) => ToggleEnabled("sfx_lfe"));
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
                        case "threshold": Settings.VMS_Threshold = Clamp(Round2(Settings.VMS_Threshold + dir * 1.0), 4, 100);
                            SendTune("Violent Movement Threshold", pid, 2, Settings.VMS_Threshold, 0, 0); break;
                        case "duration":  Settings.VMS_Duration  = Clamp(Round2(Settings.VMS_Duration  + dir * 1.0), 1, 7);
                            SendTune("Violent Movement Suppression Time", pid, 2, Settings.VMS_Duration, 0, 0); break;
                    }
                    break;
                // Vehicle Parameters (tuneGroup 13)
                case "vp_revlimiter":
                    switch (param)
                    {
                        case "maxgear": Settings.VP_RevLimiter_MaxGear = Clamp(Round2(Settings.VP_RevLimiter_MaxGear + dir * 1.0), 1, 20); break;
                        case "offset":  Settings.VP_RevLimiter_Offset  = Clamp(Round2(Settings.VP_RevLimiter_Offset  + dir * 10.0), 0, 1200); break;
                    }
                    SendTune("Rev Limiter", pid, 13, Settings.VP_RevLimiter_MaxGear, Settings.VP_RevLimiter_Offset, 0);
                    break;
                case "vp_wheelfwdslip":
                    switch (param)
                    {
                        case "front": Settings.VP_WheelFwdSlip_Front = Clamp(Round2(Settings.VP_WheelFwdSlip_Front + dir * 1.0), 0, 100); break;
                        case "rear":  Settings.VP_WheelFwdSlip_Rear  = Clamp(Round2(Settings.VP_WheelFwdSlip_Rear  + dir * 1.0), 0, 100); break;
                    }
                    SendTune("Wheel Forward Slip/Lock Threshold", pid, 13, Settings.VP_WheelFwdSlip_Front, Settings.VP_WheelFwdSlip_Rear, 0);
                    break;
                case "vp_slipangle":
                    switch (param)
                    {
                        case "front":      Settings.VP_SlipAngle_Front      = Clamp(Round2(Settings.VP_SlipAngle_Front      + dir * STEP), 0, 20); break;
                        case "rear":       Settings.VP_SlipAngle_Rear       = Clamp(Round2(Settings.VP_SlipAngle_Rear       + dir * STEP), 0, 20); break;
                        case "escalation": Settings.VP_SlipAngle_Escalation = Clamp(Round2(Settings.VP_SlipAngle_Escalation + dir * STEP), 0, 3);  break;
                    }
                    SendTune("Wheel Slip Angle Threshold", pid, 13, Settings.VP_SlipAngle_Front, Settings.VP_SlipAngle_Rear, Settings.VP_SlipAngle_Escalation);
                    break;
                case "vp_wheelbase":
                    Settings.VP_WheelBase = Clamp(Round2(Settings.VP_WheelBase + dir * 0.1), 1, 4);
                    SendTune("Wheel Base", pid, 13, Settings.VP_WheelBase, 0, 0);
                    break;
                case "vp_turningcircle":
                    Settings.VP_TurningCircle = Clamp(Round2(Settings.VP_TurningCircle + dir * 0.1), 5, 20);
                    SendTune("Turning Circle", pid, 13, Settings.VP_TurningCircle, 0, 0);
                    break;
                case "vp_cogbias":
                    Settings.VP_CoGBias = Clamp(Round2(Settings.VP_CoGBias + dir * 0.01), 0, 1);
                    SendTune("CoG Bias", pid, 13, Settings.VP_CoGBias, 0, 0);
                    break;
                // Seat Belt Tensioner | SFX (tuneGroup 17)
                case "sfx_revlimiter":
                    switch (param)
                    {
                        case "freq":      Settings.SFX_RevLimiter_Freq      = Clamp(Round2(Settings.SFX_RevLimiter_Freq      + dir * 1.0), 2, 50);  break;
                        case "intensity": Settings.SFX_RevLimiter_Intensity = Clamp(Round2(Settings.SFX_RevLimiter_Intensity + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Rev Limiter", pid, 17, Settings.SFX_RevLimiter_Freq, Settings.SFX_RevLimiter_Intensity, 0);
                    break;
                case "sfx_gearchange":
                    switch (param)
                    {
                        case "duration":  Settings.SFX_GearChange_Duration  = Clamp(Round2(Settings.SFX_GearChange_Duration  + dir * 5.0),  20, 250); break;
                        case "downshift": Settings.SFX_GearChange_Downshift = Clamp(Round2(Settings.SFX_GearChange_Downshift + dir * STEP), 0, 2.5);  break;
                        case "upshift":   Settings.SFX_GearChange_Upshift   = Clamp(Round2(Settings.SFX_GearChange_Upshift   + dir * STEP), 0, 2.5);  break;
                    }
                    SendTune("Gear Change Effect", pid, 17, Settings.SFX_GearChange_Duration, Settings.SFX_GearChange_Downshift, Settings.SFX_GearChange_Upshift);
                    break;
                case "sfx_wheelfwdslip":
                    switch (param)
                    {
                        case "freq":      Settings.SFX_WheelFwdSlip_Freq      = Clamp(Round2(Settings.SFX_WheelFwdSlip_Freq      + dir * 1.0),  2, 50);  break;
                        case "intensity": Settings.SFX_WheelFwdSlip_Intensity = Clamp(Round2(Settings.SFX_WheelFwdSlip_Intensity + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Wheel Forward Slip/Lock", pid, 17, Settings.SFX_WheelFwdSlip_Freq, Settings.SFX_WheelFwdSlip_Intensity, 0);
                    break;
                case "sfx_wheelslipangle":
                    switch (param)
                    {
                        case "freq":      Settings.SFX_WheelSlipAngle_Freq      = Clamp(Round2(Settings.SFX_WheelSlipAngle_Freq      + dir * 1.0),  2, 50);  break;
                        case "intensity": Settings.SFX_WheelSlipAngle_Intensity = Clamp(Round2(Settings.SFX_WheelSlipAngle_Intensity + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Wheel Slip Angle", pid, 17, Settings.SFX_WheelSlipAngle_Freq, Settings.SFX_WheelSlipAngle_Intensity, 0);
                    break;
                case "sfx_rumblestrips":
                    switch (param)
                    {
                        case "freqlow":  Settings.SFX_RumbleStrips_FreqLow  = Clamp(Round2(Settings.SFX_RumbleStrips_FreqLow  + dir * 1.0),  2, 50);  break;
                        case "freqhigh": Settings.SFX_RumbleStrips_FreqHigh = Clamp(Round2(Settings.SFX_RumbleStrips_FreqHigh + dir * 1.0),  2, 50);  break;
                        case "intlow":   Settings.SFX_RumbleStrips_IntLow   = Clamp(Round2(Settings.SFX_RumbleStrips_IntLow   + dir * STEP), 0, 2.5); break;
                        case "inthigh":  Settings.SFX_RumbleStrips_IntHigh  = Clamp(Round2(Settings.SFX_RumbleStrips_IntHigh  + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("Rumble Strips Frequency", pid, 17, Settings.SFX_RumbleStrips_FreqLow, Settings.SFX_RumbleStrips_FreqHigh, 0);
                    SendTune("Rumble Strips Intensity", pid, 17, Settings.SFX_RumbleStrips_IntLow,  Settings.SFX_RumbleStrips_IntHigh,  0);
                    break;
                case "sfx_abs":
                    switch (param)
                    {
                        case "freq":      Settings.SFX_ABS_Freq      = Clamp(Round2(Settings.SFX_ABS_Freq      + dir * 1.0),  2, 50);  break;
                        case "intensity": Settings.SFX_ABS_Intensity = Clamp(Round2(Settings.SFX_ABS_Intensity + dir * STEP), 0, 2.5); break;
                    }
                    SendTune("ABS Active", pid, 17, Settings.SFX_ABS_Freq, Settings.SFX_ABS_Intensity, 0);
                    break;
                case "sfx_engvib_extra":
                    switch (param)
                    {
                        case "phase":   Settings.SFX_EngVib_Extra_Phase   = Clamp(Round2(Settings.SFX_EngVib_Extra_Phase   + dir * 5.0),  0, 250); break;
                        case "alone":   Settings.SFX_EngVib_Extra_Alone   = Clamp(Round2(Settings.SFX_EngVib_Extra_Alone   + dir * STEP), 0, 2.5); break;
                        case "ingroup": Settings.SFX_EngVib_Extra_InGroup = Clamp(Round2(Settings.SFX_EngVib_Extra_InGroup + dir * STEP), 0, 1.0); break;
                    }
                    SendTune("Engine Vibration Extra", pid, 17, Settings.SFX_EngVib_Extra_Phase, Settings.SFX_EngVib_Extra_Alone, Settings.SFX_EngVib_Extra_InGroup);
                    break;
                case "sfx_lfe":
                    switch (param)
                    {
                        case "val0": Settings.SFX_LFE_Val0 = Clamp(Round2(Settings.SFX_LFE_Val0 + dir * STEP), 0, 2.5); break;
                        case "val1": Settings.SFX_LFE_Val1 = Clamp(Round2(Settings.SFX_LFE_Val1 + dir * STEP), 0.1, 2.5); break;
                    }
                    SendTune("LFE Enhancement", pid, 17, Settings.SFX_LFE_Val0, Settings.SFX_LFE_Val1, 0);
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
                case "vms":           Settings.VMS_Enabled           = !Settings.VMS_Enabled;
                    SendTuneEnabled("Violent Movement Threshold",       pid, 2,  Settings.VMS_Enabled);
                    SendTuneEnabled("Violent Movement Suppression Time", pid, 2, Settings.VMS_Enabled); break;
                case "vp_revlimiter":    Settings.VP_RevLimiter_Enabled    = !Settings.VP_RevLimiter_Enabled;    SendTuneEnabled("Rev Limiter",                   pid, 13, Settings.VP_RevLimiter_Enabled);    break;
                case "vp_wheelfwdslip":  Settings.VP_WheelFwdSlip_Enabled  = !Settings.VP_WheelFwdSlip_Enabled;  SendTuneEnabled("Wheel Forward Slip/Lock Threshold", pid, 13, Settings.VP_WheelFwdSlip_Enabled); break;
                case "vp_slipangle":     Settings.VP_SlipAngle_Enabled     = !Settings.VP_SlipAngle_Enabled;     SendTuneEnabled("Wheel Slip Angle Threshold",    pid, 13, Settings.VP_SlipAngle_Enabled);     break;
                case "sfx_revlimiter":   Settings.SFX_RevLimiter_Enabled   = !Settings.SFX_RevLimiter_Enabled;   SendTuneEnabled("Rev Limiter",                   pid, 17, Settings.SFX_RevLimiter_Enabled);   break;
                case "sfx_gearchange":   Settings.SFX_GearChange_Enabled   = !Settings.SFX_GearChange_Enabled;   SendTuneEnabled("Gear Change Effect",            pid, 17, Settings.SFX_GearChange_Enabled);   break;
                case "sfx_wheelfwdslip": Settings.SFX_WheelFwdSlip_Enabled = !Settings.SFX_WheelFwdSlip_Enabled; SendTuneEnabled("Wheel Forward Slip/Lock",       pid, 17, Settings.SFX_WheelFwdSlip_Enabled); break;
                case "sfx_wheelslipangle": Settings.SFX_WheelSlipAngle_Enabled = !Settings.SFX_WheelSlipAngle_Enabled; SendTuneEnabled("Wheel Slip Angle",         pid, 17, Settings.SFX_WheelSlipAngle_Enabled); break;
                case "sfx_rumblestrips": Settings.SFX_RumbleStrips_Enabled = !Settings.SFX_RumbleStrips_Enabled;
                    SendTuneEnabled("Rumble Strips Frequency", pid, 17, Settings.SFX_RumbleStrips_Enabled);
                    SendTuneEnabled("Rumble Strips Intensity",  pid, 17, Settings.SFX_RumbleStrips_Enabled); break;
                case "sfx_abs":          Settings.SFX_ABS_Enabled          = !Settings.SFX_ABS_Enabled;          SendTuneEnabled("ABS Active",                    pid, 17, Settings.SFX_ABS_Enabled);          break;
                case "sfx_lfe":          Settings.SFX_LFE_Enabled          = !Settings.SFX_LFE_Enabled;          SendTuneEnabled("LFE Enhancement",               pid, 17, Settings.SFX_LFE_Enabled);          break;
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
                Settings.VMS_Threshold = Round2(GetTuneValue(json, "Violent Movement Threshold", 0));
                Settings.VMS_Duration  = Round2(GetTuneValue(json, "Violent Movement Suppression Time", 0));
                Settings.VMS_Enabled   = GetTuneEnabled(json, "Violent Movement Threshold");
                // Vehicle Parameters (tuneGroup 13)
                Settings.VP_RevLimiter_MaxGear    = Round2(GetTuneValue(json, "Rev Limiter", 0));
                Settings.VP_RevLimiter_Offset     = Round2(GetTuneValue(json, "Rev Limiter", 1));
                Settings.VP_RevLimiter_Enabled    = GetTuneEnabled(json, "Rev Limiter");
                Settings.VP_WheelFwdSlip_Front    = Round2(GetTuneValue(json, "Wheel Forward Slip/Lock Threshold", 0));
                Settings.VP_WheelFwdSlip_Rear     = Round2(GetTuneValue(json, "Wheel Forward Slip/Lock Threshold", 1));
                Settings.VP_WheelFwdSlip_Enabled  = GetTuneEnabled(json, "Wheel Forward Slip/Lock Threshold");
                Settings.VP_SlipAngle_Front       = Round2(GetTuneValue(json, "Wheel Slip Angle Threshold", 0));
                Settings.VP_SlipAngle_Rear        = Round2(GetTuneValue(json, "Wheel Slip Angle Threshold", 1));
                Settings.VP_SlipAngle_Escalation  = Round2(GetTuneValue(json, "Wheel Slip Angle Threshold", 2));
                Settings.VP_SlipAngle_Enabled     = GetTuneEnabled(json, "Wheel Slip Angle Threshold");
                Settings.VP_WheelBase             = Round2(GetTuneValue(json, "Wheel Base", 0));
                Settings.VP_TurningCircle         = Round2(GetTuneValue(json, "Turning Circle", 0));
                Settings.VP_CoGBias               = Round2(GetTuneValue(json, "CoG Bias", 0));
                // Seat Belt Tensioner | SFX (tuneGroup 17)
                Settings.SFX_RevLimiter_Freq        = Round2(GetTuneValue(json, "Rev Limiter", 0));
                Settings.SFX_RevLimiter_Intensity   = Round2(GetTuneValue(json, "Rev Limiter", 1));
                Settings.SFX_RevLimiter_Enabled     = GetTuneEnabled(json, "Rev Limiter");
                Settings.SFX_GearChange_Duration    = Round2(GetTuneValue(json, "Gear Change Effect", 0));
                Settings.SFX_GearChange_Downshift   = Round2(GetTuneValue(json, "Gear Change Effect", 1));
                Settings.SFX_GearChange_Upshift     = Round2(GetTuneValue(json, "Gear Change Effect", 2));
                Settings.SFX_GearChange_Enabled     = GetTuneEnabled(json, "Gear Change Effect");
                Settings.SFX_WheelFwdSlip_Freq      = Round2(GetTuneValue(json, "Wheel Forward Slip/Lock", 0));
                Settings.SFX_WheelFwdSlip_Intensity = Round2(GetTuneValue(json, "Wheel Forward Slip/Lock", 1));
                Settings.SFX_WheelFwdSlip_Enabled   = GetTuneEnabled(json, "Wheel Forward Slip/Lock");
                Settings.SFX_WheelSlipAngle_Freq      = Round2(GetTuneValue(json, "Wheel Slip Angle", 0));
                Settings.SFX_WheelSlipAngle_Intensity = Round2(GetTuneValue(json, "Wheel Slip Angle", 1));
                Settings.SFX_WheelSlipAngle_Enabled   = GetTuneEnabled(json, "Wheel Slip Angle");
                Settings.SFX_RumbleStrips_FreqLow   = Round2(GetTuneValue(json, "Rumble Strips Frequency", 0));
                Settings.SFX_RumbleStrips_FreqHigh  = Round2(GetTuneValue(json, "Rumble Strips Frequency", 1));
                Settings.SFX_RumbleStrips_IntLow    = Round2(GetTuneValue(json, "Rumble Strips Intensity", 0));
                Settings.SFX_RumbleStrips_IntHigh   = Round2(GetTuneValue(json, "Rumble Strips Intensity", 1));
                Settings.SFX_RumbleStrips_Enabled   = GetTuneEnabled(json, "Rumble Strips Intensity");
                Settings.SFX_ABS_Freq               = Round2(GetTuneValue(json, "ABS Active", 0));
                Settings.SFX_ABS_Intensity          = Round2(GetTuneValue(json, "ABS Active", 1));
                Settings.SFX_ABS_Enabled            = GetTuneEnabled(json, "ABS Active");
                Settings.SFX_EngVib_Extra_Phase     = Round2(GetTuneValue(json, "Engine Vibration Extra", 0));
                Settings.SFX_EngVib_Extra_Alone     = Round2(GetTuneValue(json, "Engine Vibration Extra", 1));
                Settings.SFX_EngVib_Extra_InGroup   = Round2(GetTuneValue(json, "Engine Vibration Extra", 2));
                Settings.SFX_LFE_Val0               = Round2(GetTuneValue(json, "LFE Enhancement", 0));
                Settings.SFX_LFE_Val1               = Round2(GetTuneValue(json, "LFE Enhancement", 1));
                Settings.SFX_LFE_Enabled            = GetTuneEnabled(json, "LFE Enhancement");

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
