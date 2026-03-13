using System.ComponentModel;
using System.Windows.Threading;

namespace QSBT1Plugin
{
    public class QSBT1Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        [Newtonsoft.Json.JsonIgnore]
        public Dispatcher UiDispatcher { get; set; }

        private void Notify(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            var args = new PropertyChangedEventArgs(name);
            if (UiDispatcher == null || UiDispatcher.CheckAccess())
                handler(this, args);
            else
                UiDispatcher.Invoke(() => handler(this, args));
        }

        public void NotifyAll()
        {
            Notify(nameof(IpAddress)); Notify(nameof(Port));
            Notify(nameof(Braking_ProfileId)); Notify(nameof(Braking_TuneGroup));
            // Base
            Notify(nameof(Braking_Gain));          Notify(nameof(Braking_Sharpness));         Notify(nameof(Braking_Deadzone));         Notify(nameof(Braking_Enabled));
            Notify(nameof(Acceleration_Gain));     Notify(nameof(Acceleration_Sharpness));    Notify(nameof(Acceleration_Deadzone));    Notify(nameof(Acceleration_Enabled));
            Notify(nameof(Sideways_Gain));         Notify(nameof(Sideways_Sharpness));        Notify(nameof(Sideways_Deadzone));        Notify(nameof(Sideways_Enabled));
            Notify(nameof(Centrifugal_Gain));      Notify(nameof(Centrifugal_Sharpness));     Notify(nameof(Centrifugal_Deadzone));     Notify(nameof(Centrifugal_Enabled));
            Notify(nameof(Bounds_Neutral));        Notify(nameof(Bounds_Maximum));            Notify(nameof(Bounds_Enabled));
            Notify(nameof(VerticalG_Gain));        Notify(nameof(VerticalG_Sharpness));       Notify(nameof(VerticalG_Enabled));
            Notify(nameof(SideSlip_Threshold));    Notify(nameof(SideSlip_Frequency));        Notify(nameof(SideSlip_Intensity));       Notify(nameof(SideSlip_Enabled));
            Notify(nameof(RoadHarshness_Gain));    Notify(nameof(RoadHarshness_Sharpness));   Notify(nameof(RoadHarshness_Enabled));
            Notify(nameof(PreImpact_Long));        Notify(nameof(PreImpact_Lateral));         Notify(nameof(PreImpact_Duration));       Notify(nameof(PreImpact_Enabled));
            // Motion Primary SFX
            Notify(nameof(VMS_Threshold));         Notify(nameof(VMS_Duration));              Notify(nameof(VMS_Enabled));
            // Vehicle Parameters
            Notify(nameof(VP_RevLimiter_MaxGear)); Notify(nameof(VP_RevLimiter_Offset));      Notify(nameof(VP_RevLimiter_Enabled));
            Notify(nameof(VP_WheelFwdSlip_Front)); Notify(nameof(VP_WheelFwdSlip_Rear));      Notify(nameof(VP_WheelFwdSlip_Enabled));
            Notify(nameof(VP_SlipAngle_Front));    Notify(nameof(VP_SlipAngle_Rear));         Notify(nameof(VP_SlipAngle_Escalation));  Notify(nameof(VP_SlipAngle_Enabled));
            Notify(nameof(VP_WheelBase));          Notify(nameof(VP_TurningCircle));          Notify(nameof(VP_CoGBias));
            // SFX
            Notify(nameof(SFX_RevLimiter_Freq));   Notify(nameof(SFX_RevLimiter_Intensity));  Notify(nameof(SFX_RevLimiter_Enabled));
            Notify(nameof(SFX_GearChange_Duration)); Notify(nameof(SFX_GearChange_Downshift)); Notify(nameof(SFX_GearChange_Upshift));  Notify(nameof(SFX_GearChange_Enabled));
            Notify(nameof(SFX_WheelFwdSlip_Freq)); Notify(nameof(SFX_WheelFwdSlip_Intensity)); Notify(nameof(SFX_WheelFwdSlip_Enabled));
            Notify(nameof(SFX_WheelSlipAngle_Freq)); Notify(nameof(SFX_WheelSlipAngle_Intensity)); Notify(nameof(SFX_WheelSlipAngle_Enabled));
            Notify(nameof(SFX_RumbleStrips_FreqLow)); Notify(nameof(SFX_RumbleStrips_FreqHigh)); Notify(nameof(SFX_RumbleStrips_IntLow)); Notify(nameof(SFX_RumbleStrips_IntHigh)); Notify(nameof(SFX_RumbleStrips_Enabled));
            Notify(nameof(SFX_ABS_Freq));          Notify(nameof(SFX_ABS_Intensity));         Notify(nameof(SFX_ABS_Enabled));
            Notify(nameof(SFX_EngVib_Extra_Phase)); Notify(nameof(SFX_EngVib_Extra_Alone));   Notify(nameof(SFX_EngVib_Extra_InGroup));
            Notify(nameof(SFX_LFE_Val0));          Notify(nameof(SFX_LFE_Val1));              Notify(nameof(SFX_LFE_Enabled));
        }

        // ── Network ──────────────────────────────────────────────────────────
        private string _ipAddress = "192.168.8.131";
        public string IpAddress { get => _ipAddress; set { _ipAddress = value; Notify(nameof(IpAddress)); } }
        private int _port = 8081;
        public int Port { get => _port; set { _port = value; Notify(nameof(Port)); } }

        // ── Profile ───────────────────────────────────────────────────────────
        private int _braking_ProfileId = 276;
        public int Braking_ProfileId { get => _braking_ProfileId; set { _braking_ProfileId = value; Notify(nameof(Braking_ProfileId)); } }
        private int _braking_TuneGroup = 12;
        public int Braking_TuneGroup { get => _braking_TuneGroup; set { _braking_TuneGroup = value; Notify(nameof(Braking_TuneGroup)); } }
        private int _centrifugal_ProfileId = 276;
        public int Centrifugal_ProfileId { get => _centrifugal_ProfileId; set { _centrifugal_ProfileId = value; Notify(nameof(Centrifugal_ProfileId)); } }
        private int _centrifugal_TuneGroup = 12;
        public int Centrifugal_TuneGroup { get => _centrifugal_TuneGroup; set { _centrifugal_TuneGroup = value; Notify(nameof(Centrifugal_TuneGroup)); } }

        // ── Seat Belt Tensioner | Base (tuneGroup 12) ────────────────────────
        private double _braking_Gain = 1.7;
        public double Braking_Gain { get => _braking_Gain; set { _braking_Gain = value; Notify(nameof(Braking_Gain)); } }
        private double _braking_Sharpness = 2.5;
        public double Braking_Sharpness { get => _braking_Sharpness; set { _braking_Sharpness = value; Notify(nameof(Braking_Sharpness)); } }
        private double _braking_Deadzone = 0.0;
        public double Braking_Deadzone { get => _braking_Deadzone; set { _braking_Deadzone = value; Notify(nameof(Braking_Deadzone)); } }
        private bool _braking_Enabled = true;
        public bool Braking_Enabled { get => _braking_Enabled; set { _braking_Enabled = value; Notify(nameof(Braking_Enabled)); } }

        private double _acceleration_Gain = 0.9;
        public double Acceleration_Gain { get => _acceleration_Gain; set { _acceleration_Gain = value; Notify(nameof(Acceleration_Gain)); } }
        private double _acceleration_Sharpness = 1.0;
        public double Acceleration_Sharpness { get => _acceleration_Sharpness; set { _acceleration_Sharpness = value; Notify(nameof(Acceleration_Sharpness)); } }
        private double _acceleration_Deadzone = 0.2;
        public double Acceleration_Deadzone { get => _acceleration_Deadzone; set { _acceleration_Deadzone = value; Notify(nameof(Acceleration_Deadzone)); } }
        private bool _acceleration_Enabled = false;
        public bool Acceleration_Enabled { get => _acceleration_Enabled; set { _acceleration_Enabled = value; Notify(nameof(Acceleration_Enabled)); } }

        private double _sideways_Gain = 2.5;
        public double Sideways_Gain { get => _sideways_Gain; set { _sideways_Gain = value; Notify(nameof(Sideways_Gain)); } }
        private double _sideways_Sharpness = 2.5;
        public double Sideways_Sharpness { get => _sideways_Sharpness; set { _sideways_Sharpness = value; Notify(nameof(Sideways_Sharpness)); } }
        private double _sideways_Deadzone = 0.0;
        public double Sideways_Deadzone { get => _sideways_Deadzone; set { _sideways_Deadzone = value; Notify(nameof(Sideways_Deadzone)); } }
        private bool _sideways_Enabled = false;
        public bool Sideways_Enabled { get => _sideways_Enabled; set { _sideways_Enabled = value; Notify(nameof(Sideways_Enabled)); } }

        private double _centrifugal_Gain = 0.25;
        public double Centrifugal_Gain { get => _centrifugal_Gain; set { _centrifugal_Gain = value; Notify(nameof(Centrifugal_Gain)); } }
        private double _centrifugal_Sharpness = 2.5;
        public double Centrifugal_Sharpness { get => _centrifugal_Sharpness; set { _centrifugal_Sharpness = value; Notify(nameof(Centrifugal_Sharpness)); } }
        private double _centrifugal_Deadzone = 0.0;
        public double Centrifugal_Deadzone { get => _centrifugal_Deadzone; set { _centrifugal_Deadzone = value; Notify(nameof(Centrifugal_Deadzone)); } }
        private bool _centrifugal_Enabled = true;
        public bool Centrifugal_Enabled { get => _centrifugal_Enabled; set { _centrifugal_Enabled = value; Notify(nameof(Centrifugal_Enabled)); } }

        private double _bounds_Neutral = 1.0;
        public double Bounds_Neutral { get => _bounds_Neutral; set { _bounds_Neutral = value; Notify(nameof(Bounds_Neutral)); } }
        private double _bounds_Maximum = 100.0;
        public double Bounds_Maximum { get => _bounds_Maximum; set { _bounds_Maximum = value; Notify(nameof(Bounds_Maximum)); } }
        private bool _bounds_Enabled = false;
        public bool Bounds_Enabled { get => _bounds_Enabled; set { _bounds_Enabled = value; Notify(nameof(Bounds_Enabled)); } }

        private double _verticalG_Gain = 0.6;
        public double VerticalG_Gain { get => _verticalG_Gain; set { _verticalG_Gain = value; Notify(nameof(VerticalG_Gain)); } }
        private double _verticalG_Sharpness = 2.5;
        public double VerticalG_Sharpness { get => _verticalG_Sharpness; set { _verticalG_Sharpness = value; Notify(nameof(VerticalG_Sharpness)); } }
        private bool _verticalG_Enabled = false;
        public bool VerticalG_Enabled { get => _verticalG_Enabled; set { _verticalG_Enabled = value; Notify(nameof(VerticalG_Enabled)); } }

        private double _sideSlip_Threshold = 3.25;
        public double SideSlip_Threshold { get => _sideSlip_Threshold; set { _sideSlip_Threshold = value; Notify(nameof(SideSlip_Threshold)); } }
        private double _sideSlip_Frequency = 30.0;
        public double SideSlip_Frequency { get => _sideSlip_Frequency; set { _sideSlip_Frequency = value; Notify(nameof(SideSlip_Frequency)); } }
        private double _sideSlip_Intensity = 2.5;
        public double SideSlip_Intensity { get => _sideSlip_Intensity; set { _sideSlip_Intensity = value; Notify(nameof(SideSlip_Intensity)); } }
        private bool _sideSlip_Enabled = true;
        public bool SideSlip_Enabled { get => _sideSlip_Enabled; set { _sideSlip_Enabled = value; Notify(nameof(SideSlip_Enabled)); } }

        private double _roadHarshness_Gain = 0.24;
        public double RoadHarshness_Gain { get => _roadHarshness_Gain; set { _roadHarshness_Gain = value; Notify(nameof(RoadHarshness_Gain)); } }
        private double _roadHarshness_Sharpness = 1.0;
        public double RoadHarshness_Sharpness { get => _roadHarshness_Sharpness; set { _roadHarshness_Sharpness = value; Notify(nameof(RoadHarshness_Sharpness)); } }
        private bool _roadHarshness_Enabled = true;
        public bool RoadHarshness_Enabled { get => _roadHarshness_Enabled; set { _roadHarshness_Enabled = value; Notify(nameof(RoadHarshness_Enabled)); } }

        private double _preImpact_Long = 50.0;
        public double PreImpact_Long { get => _preImpact_Long; set { _preImpact_Long = value; Notify(nameof(PreImpact_Long)); } }
        private double _preImpact_Lateral = 50.0;
        public double PreImpact_Lateral { get => _preImpact_Lateral; set { _preImpact_Lateral = value; Notify(nameof(PreImpact_Lateral)); } }
        private double _preImpact_Duration = 500.0;
        public double PreImpact_Duration { get => _preImpact_Duration; set { _preImpact_Duration = value; Notify(nameof(PreImpact_Duration)); } }
        private bool _preImpact_Enabled = false;
        public bool PreImpact_Enabled { get => _preImpact_Enabled; set { _preImpact_Enabled = value; Notify(nameof(PreImpact_Enabled)); } }

        // ── Motion Primary | SFX (tuneGroup 2) ───────────────────────────────
        private double _vms_Threshold = 50.0;
        public double VMS_Threshold { get => _vms_Threshold; set { _vms_Threshold = value; Notify(nameof(VMS_Threshold)); } }
        private double _vms_Duration = 3.0;
        public double VMS_Duration { get => _vms_Duration; set { _vms_Duration = value; Notify(nameof(VMS_Duration)); } }
        private bool _vms_Enabled = true;
        public bool VMS_Enabled { get => _vms_Enabled; set { _vms_Enabled = value; Notify(nameof(VMS_Enabled)); } }

        // ── Vehicle Parameters (tuneGroup 13) ────────────────────────────────
        private double _vp_RevLimiter_MaxGear = 8.52;
        public double VP_RevLimiter_MaxGear { get => _vp_RevLimiter_MaxGear; set { _vp_RevLimiter_MaxGear = value; Notify(nameof(VP_RevLimiter_MaxGear)); } }
        private double _vp_RevLimiter_Offset = 200.0;
        public double VP_RevLimiter_Offset { get => _vp_RevLimiter_Offset; set { _vp_RevLimiter_Offset = value; Notify(nameof(VP_RevLimiter_Offset)); } }
        private bool _vp_RevLimiter_Enabled = false;
        public bool VP_RevLimiter_Enabled { get => _vp_RevLimiter_Enabled; set { _vp_RevLimiter_Enabled = value; Notify(nameof(VP_RevLimiter_Enabled)); } }

        private double _vp_WheelFwdSlip_Front = 6.0;
        public double VP_WheelFwdSlip_Front { get => _vp_WheelFwdSlip_Front; set { _vp_WheelFwdSlip_Front = value; Notify(nameof(VP_WheelFwdSlip_Front)); } }
        private double _vp_WheelFwdSlip_Rear = 30.0;
        public double VP_WheelFwdSlip_Rear { get => _vp_WheelFwdSlip_Rear; set { _vp_WheelFwdSlip_Rear = value; Notify(nameof(VP_WheelFwdSlip_Rear)); } }
        private bool _vp_WheelFwdSlip_Enabled = false;
        public bool VP_WheelFwdSlip_Enabled { get => _vp_WheelFwdSlip_Enabled; set { _vp_WheelFwdSlip_Enabled = value; Notify(nameof(VP_WheelFwdSlip_Enabled)); } }

        private double _vp_SlipAngle_Front = 4.0;
        public double VP_SlipAngle_Front { get => _vp_SlipAngle_Front; set { _vp_SlipAngle_Front = value; Notify(nameof(VP_SlipAngle_Front)); } }
        private double _vp_SlipAngle_Rear = 1.5;
        public double VP_SlipAngle_Rear { get => _vp_SlipAngle_Rear; set { _vp_SlipAngle_Rear = value; Notify(nameof(VP_SlipAngle_Rear)); } }
        private double _vp_SlipAngle_Escalation = 1.41;
        public double VP_SlipAngle_Escalation { get => _vp_SlipAngle_Escalation; set { _vp_SlipAngle_Escalation = value; Notify(nameof(VP_SlipAngle_Escalation)); } }
        private bool _vp_SlipAngle_Enabled = false;
        public bool VP_SlipAngle_Enabled { get => _vp_SlipAngle_Enabled; set { _vp_SlipAngle_Enabled = value; Notify(nameof(VP_SlipAngle_Enabled)); } }

        private double _vp_WheelBase = 2.8;
        public double VP_WheelBase { get => _vp_WheelBase; set { _vp_WheelBase = value; Notify(nameof(VP_WheelBase)); } }
        private double _vp_TurningCircle = 11.3;
        public double VP_TurningCircle { get => _vp_TurningCircle; set { _vp_TurningCircle = value; Notify(nameof(VP_TurningCircle)); } }
        private double _vp_CoGBias = 0.5;
        public double VP_CoGBias { get => _vp_CoGBias; set { _vp_CoGBias = value; Notify(nameof(VP_CoGBias)); } }

        // ── Seat Belt Tensioner | SFX (tuneGroup 17) ─────────────────────────
        private double _sfx_RevLimiter_Freq = 15.0;
        public double SFX_RevLimiter_Freq { get => _sfx_RevLimiter_Freq; set { _sfx_RevLimiter_Freq = value; Notify(nameof(SFX_RevLimiter_Freq)); } }
        private double _sfx_RevLimiter_Intensity = 1.0;
        public double SFX_RevLimiter_Intensity { get => _sfx_RevLimiter_Intensity; set { _sfx_RevLimiter_Intensity = value; Notify(nameof(SFX_RevLimiter_Intensity)); } }
        private bool _sfx_RevLimiter_Enabled = false;
        public bool SFX_RevLimiter_Enabled { get => _sfx_RevLimiter_Enabled; set { _sfx_RevLimiter_Enabled = value; Notify(nameof(SFX_RevLimiter_Enabled)); } }

        private double _sfx_GearChange_Duration = 74.0;
        public double SFX_GearChange_Duration { get => _sfx_GearChange_Duration; set { _sfx_GearChange_Duration = value; Notify(nameof(SFX_GearChange_Duration)); } }
        private double _sfx_GearChange_Downshift = 1.9;
        public double SFX_GearChange_Downshift { get => _sfx_GearChange_Downshift; set { _sfx_GearChange_Downshift = value; Notify(nameof(SFX_GearChange_Downshift)); } }
        private double _sfx_GearChange_Upshift = 1.2;
        public double SFX_GearChange_Upshift { get => _sfx_GearChange_Upshift; set { _sfx_GearChange_Upshift = value; Notify(nameof(SFX_GearChange_Upshift)); } }
        private bool _sfx_GearChange_Enabled = false;
        public bool SFX_GearChange_Enabled { get => _sfx_GearChange_Enabled; set { _sfx_GearChange_Enabled = value; Notify(nameof(SFX_GearChange_Enabled)); } }

        private double _sfx_WheelFwdSlip_Freq = 12.0;
        public double SFX_WheelFwdSlip_Freq { get => _sfx_WheelFwdSlip_Freq; set { _sfx_WheelFwdSlip_Freq = value; Notify(nameof(SFX_WheelFwdSlip_Freq)); } }
        private double _sfx_WheelFwdSlip_Intensity = 1.0;
        public double SFX_WheelFwdSlip_Intensity { get => _sfx_WheelFwdSlip_Intensity; set { _sfx_WheelFwdSlip_Intensity = value; Notify(nameof(SFX_WheelFwdSlip_Intensity)); } }
        private bool _sfx_WheelFwdSlip_Enabled = true;
        public bool SFX_WheelFwdSlip_Enabled { get => _sfx_WheelFwdSlip_Enabled; set { _sfx_WheelFwdSlip_Enabled = value; Notify(nameof(SFX_WheelFwdSlip_Enabled)); } }

        private double _sfx_WheelSlipAngle_Freq = 18.0;
        public double SFX_WheelSlipAngle_Freq { get => _sfx_WheelSlipAngle_Freq; set { _sfx_WheelSlipAngle_Freq = value; Notify(nameof(SFX_WheelSlipAngle_Freq)); } }
        private double _sfx_WheelSlipAngle_Intensity = 1.0;
        public double SFX_WheelSlipAngle_Intensity { get => _sfx_WheelSlipAngle_Intensity; set { _sfx_WheelSlipAngle_Intensity = value; Notify(nameof(SFX_WheelSlipAngle_Intensity)); } }
        private bool _sfx_WheelSlipAngle_Enabled = false;
        public bool SFX_WheelSlipAngle_Enabled { get => _sfx_WheelSlipAngle_Enabled; set { _sfx_WheelSlipAngle_Enabled = value; Notify(nameof(SFX_WheelSlipAngle_Enabled)); } }

        private double _sfx_RumbleStrips_FreqLow = 4.58;
        public double SFX_RumbleStrips_FreqLow { get => _sfx_RumbleStrips_FreqLow; set { _sfx_RumbleStrips_FreqLow = value; Notify(nameof(SFX_RumbleStrips_FreqLow)); } }
        private double _sfx_RumbleStrips_FreqHigh = 37.42;
        public double SFX_RumbleStrips_FreqHigh { get => _sfx_RumbleStrips_FreqHigh; set { _sfx_RumbleStrips_FreqHigh = value; Notify(nameof(SFX_RumbleStrips_FreqHigh)); } }
        private double _sfx_RumbleStrips_IntLow = 1.0;
        public double SFX_RumbleStrips_IntLow { get => _sfx_RumbleStrips_IntLow; set { _sfx_RumbleStrips_IntLow = value; Notify(nameof(SFX_RumbleStrips_IntLow)); } }
        private double _sfx_RumbleStrips_IntHigh = 1.0;
        public double SFX_RumbleStrips_IntHigh { get => _sfx_RumbleStrips_IntHigh; set { _sfx_RumbleStrips_IntHigh = value; Notify(nameof(SFX_RumbleStrips_IntHigh)); } }
        private bool _sfx_RumbleStrips_Enabled = false;
        public bool SFX_RumbleStrips_Enabled { get => _sfx_RumbleStrips_Enabled; set { _sfx_RumbleStrips_Enabled = value; Notify(nameof(SFX_RumbleStrips_Enabled)); } }

        private double _sfx_ABS_Freq = 12.0;
        public double SFX_ABS_Freq { get => _sfx_ABS_Freq; set { _sfx_ABS_Freq = value; Notify(nameof(SFX_ABS_Freq)); } }
        private double _sfx_ABS_Intensity = 1.0;
        public double SFX_ABS_Intensity { get => _sfx_ABS_Intensity; set { _sfx_ABS_Intensity = value; Notify(nameof(SFX_ABS_Intensity)); } }
        private bool _sfx_ABS_Enabled = false;
        public bool SFX_ABS_Enabled { get => _sfx_ABS_Enabled; set { _sfx_ABS_Enabled = value; Notify(nameof(SFX_ABS_Enabled)); } }

        private double _sfx_EngVib_Extra_Phase = 0.0;
        public double SFX_EngVib_Extra_Phase { get => _sfx_EngVib_Extra_Phase; set { _sfx_EngVib_Extra_Phase = value; Notify(nameof(SFX_EngVib_Extra_Phase)); } }
        private double _sfx_EngVib_Extra_Alone = 1.0;
        public double SFX_EngVib_Extra_Alone { get => _sfx_EngVib_Extra_Alone; set { _sfx_EngVib_Extra_Alone = value; Notify(nameof(SFX_EngVib_Extra_Alone)); } }
        private double _sfx_EngVib_Extra_InGroup = 0.75;
        public double SFX_EngVib_Extra_InGroup { get => _sfx_EngVib_Extra_InGroup; set { _sfx_EngVib_Extra_InGroup = value; Notify(nameof(SFX_EngVib_Extra_InGroup)); } }

        private double _sfx_LFE_Val0 = 1.0;
        public double SFX_LFE_Val0 { get => _sfx_LFE_Val0; set { _sfx_LFE_Val0 = value; Notify(nameof(SFX_LFE_Val0)); } }
        private double _sfx_LFE_Val1 = 1.0;
        public double SFX_LFE_Val1 { get => _sfx_LFE_Val1; set { _sfx_LFE_Val1 = value; Notify(nameof(SFX_LFE_Val1)); } }
        private bool _sfx_LFE_Enabled = false;
        public bool SFX_LFE_Enabled { get => _sfx_LFE_Enabled; set { _sfx_LFE_Enabled = value; Notify(nameof(SFX_LFE_Enabled)); } }
    }
}
