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
            Notify(nameof(IpAddress));
            Notify(nameof(Port));
            Notify(nameof(Braking_ProfileId));
            Notify(nameof(Braking_TuneGroup));
            Notify(nameof(Braking_Gain));         Notify(nameof(Braking_Sharpness));         Notify(nameof(Braking_Deadzone));         Notify(nameof(Braking_Enabled));
            Notify(nameof(Centrifugal_ProfileId));
            Notify(nameof(Centrifugal_TuneGroup));
            Notify(nameof(Centrifugal_Gain));     Notify(nameof(Centrifugal_Sharpness));     Notify(nameof(Centrifugal_Deadzone));     Notify(nameof(Centrifugal_Enabled));
            Notify(nameof(Acceleration_Gain));    Notify(nameof(Acceleration_Sharpness));    Notify(nameof(Acceleration_Deadzone));    Notify(nameof(Acceleration_Enabled));
            Notify(nameof(Sideways_Gain));        Notify(nameof(Sideways_Sharpness));        Notify(nameof(Sideways_Deadzone));        Notify(nameof(Sideways_Enabled));
            Notify(nameof(Bounds_Neutral));       Notify(nameof(Bounds_Maximum));            Notify(nameof(Bounds_Enabled));
            Notify(nameof(VerticalG_Gain));       Notify(nameof(VerticalG_Sharpness));       Notify(nameof(VerticalG_Enabled));
            Notify(nameof(SideSlip_Threshold));   Notify(nameof(SideSlip_Frequency));        Notify(nameof(SideSlip_Intensity));       Notify(nameof(SideSlip_Enabled));
            Notify(nameof(RoadHarshness_Gain));   Notify(nameof(RoadHarshness_Sharpness));   Notify(nameof(RoadHarshness_Enabled));
            Notify(nameof(PreImpact_Long));       Notify(nameof(PreImpact_Lateral));         Notify(nameof(PreImpact_Duration));       Notify(nameof(PreImpact_Enabled));
            Notify(nameof(VMS_Threshold));        Notify(nameof(VMS_Duration));              Notify(nameof(VMS_Enabled));
        }

        // ── Network ──────────────────────────────────────────────────────────
        private string _ipAddress = "192.168.8.131";
        public string IpAddress { get => _ipAddress; set { _ipAddress = value; Notify(nameof(IpAddress)); } }

        private int _port = 8081;
        public int Port { get => _port; set { _port = value; Notify(nameof(Port)); } }

        // ── Braking ──────────────────────────────────────────────────────────
        private int _braking_ProfileId = 276;
        public int Braking_ProfileId { get => _braking_ProfileId; set { _braking_ProfileId = value; Notify(nameof(Braking_ProfileId)); } }

        private int _braking_TuneGroup = 12;
        public int Braking_TuneGroup { get => _braking_TuneGroup; set { _braking_TuneGroup = value; Notify(nameof(Braking_TuneGroup)); } }

        private double _braking_Gain = 1.7;
        public double Braking_Gain { get => _braking_Gain; set { _braking_Gain = value; Notify(nameof(Braking_Gain)); } }

        private double _braking_Sharpness = 2.5;
        public double Braking_Sharpness { get => _braking_Sharpness; set { _braking_Sharpness = value; Notify(nameof(Braking_Sharpness)); } }

        private double _braking_Deadzone = 0.0;
        public double Braking_Deadzone { get => _braking_Deadzone; set { _braking_Deadzone = value; Notify(nameof(Braking_Deadzone)); } }

        private bool _braking_Enabled = true;
        public bool Braking_Enabled { get => _braking_Enabled; set { _braking_Enabled = value; Notify(nameof(Braking_Enabled)); } }

        // ── Centrifugal Force ────────────────────────────────────────────────
        private int _centrifugal_ProfileId = 276;
        public int Centrifugal_ProfileId { get => _centrifugal_ProfileId; set { _centrifugal_ProfileId = value; Notify(nameof(Centrifugal_ProfileId)); } }

        private int _centrifugal_TuneGroup = 12;
        public int Centrifugal_TuneGroup { get => _centrifugal_TuneGroup; set { _centrifugal_TuneGroup = value; Notify(nameof(Centrifugal_TuneGroup)); } }

        private double _centrifugal_Gain = 0.25;
        public double Centrifugal_Gain { get => _centrifugal_Gain; set { _centrifugal_Gain = value; Notify(nameof(Centrifugal_Gain)); } }

        private double _centrifugal_Sharpness = 2.5;
        public double Centrifugal_Sharpness { get => _centrifugal_Sharpness; set { _centrifugal_Sharpness = value; Notify(nameof(Centrifugal_Sharpness)); } }

        private double _centrifugal_Deadzone = 0.0;
        public double Centrifugal_Deadzone { get => _centrifugal_Deadzone; set { _centrifugal_Deadzone = value; Notify(nameof(Centrifugal_Deadzone)); } }

        private bool _centrifugal_Enabled = true;
        public bool Centrifugal_Enabled { get => _centrifugal_Enabled; set { _centrifugal_Enabled = value; Notify(nameof(Centrifugal_Enabled)); } }

        // ── Acceleration ─────────────────────────────────────────────────────
        private double _acceleration_Gain = 0.9;
        public double Acceleration_Gain { get => _acceleration_Gain; set { _acceleration_Gain = value; Notify(nameof(Acceleration_Gain)); } }
        private double _acceleration_Sharpness = 1.0;
        public double Acceleration_Sharpness { get => _acceleration_Sharpness; set { _acceleration_Sharpness = value; Notify(nameof(Acceleration_Sharpness)); } }
        private double _acceleration_Deadzone = 0.2;
        public double Acceleration_Deadzone { get => _acceleration_Deadzone; set { _acceleration_Deadzone = value; Notify(nameof(Acceleration_Deadzone)); } }
        private bool _acceleration_Enabled = false;
        public bool Acceleration_Enabled { get => _acceleration_Enabled; set { _acceleration_Enabled = value; Notify(nameof(Acceleration_Enabled)); } }

        // ── Sideways Acceleration ─────────────────────────────────────────────
        private double _sideways_Gain = 2.5;
        public double Sideways_Gain { get => _sideways_Gain; set { _sideways_Gain = value; Notify(nameof(Sideways_Gain)); } }
        private double _sideways_Sharpness = 2.5;
        public double Sideways_Sharpness { get => _sideways_Sharpness; set { _sideways_Sharpness = value; Notify(nameof(Sideways_Sharpness)); } }
        private double _sideways_Deadzone = 0.0;
        public double Sideways_Deadzone { get => _sideways_Deadzone; set { _sideways_Deadzone = value; Notify(nameof(Sideways_Deadzone)); } }
        private bool _sideways_Enabled = false;
        public bool Sideways_Enabled { get => _sideways_Enabled; set { _sideways_Enabled = value; Notify(nameof(Sideways_Enabled)); } }

        // ── Bounds ────────────────────────────────────────────────────────────
        private double _bounds_Neutral = 1.0;
        public double Bounds_Neutral { get => _bounds_Neutral; set { _bounds_Neutral = value; Notify(nameof(Bounds_Neutral)); } }
        private double _bounds_Maximum = 100.0;
        public double Bounds_Maximum { get => _bounds_Maximum; set { _bounds_Maximum = value; Notify(nameof(Bounds_Maximum)); } }
        private bool _bounds_Enabled = false;
        public bool Bounds_Enabled { get => _bounds_Enabled; set { _bounds_Enabled = value; Notify(nameof(Bounds_Enabled)); } }

        // ── Vertical G-Force ──────────────────────────────────────────────────
        private double _verticalG_Gain = 0.6;
        public double VerticalG_Gain { get => _verticalG_Gain; set { _verticalG_Gain = value; Notify(nameof(VerticalG_Gain)); } }
        private double _verticalG_Sharpness = 2.5;
        public double VerticalG_Sharpness { get => _verticalG_Sharpness; set { _verticalG_Sharpness = value; Notify(nameof(VerticalG_Sharpness)); } }
        private bool _verticalG_Enabled = false;
        public bool VerticalG_Enabled { get => _verticalG_Enabled; set { _verticalG_Enabled = value; Notify(nameof(VerticalG_Enabled)); } }

        // ── Side Slip ─────────────────────────────────────────────────────────
        private double _sideSlip_Threshold = 3.25;
        public double SideSlip_Threshold { get => _sideSlip_Threshold; set { _sideSlip_Threshold = value; Notify(nameof(SideSlip_Threshold)); } }
        private double _sideSlip_Frequency = 30.0;
        public double SideSlip_Frequency { get => _sideSlip_Frequency; set { _sideSlip_Frequency = value; Notify(nameof(SideSlip_Frequency)); } }
        private double _sideSlip_Intensity = 2.5;
        public double SideSlip_Intensity { get => _sideSlip_Intensity; set { _sideSlip_Intensity = value; Notify(nameof(SideSlip_Intensity)); } }
        private bool _sideSlip_Enabled = true;
        public bool SideSlip_Enabled { get => _sideSlip_Enabled; set { _sideSlip_Enabled = value; Notify(nameof(SideSlip_Enabled)); } }

        // ── Road Harshness ────────────────────────────────────────────────────
        private double _roadHarshness_Gain = 0.24;
        public double RoadHarshness_Gain { get => _roadHarshness_Gain; set { _roadHarshness_Gain = value; Notify(nameof(RoadHarshness_Gain)); } }
        private double _roadHarshness_Sharpness = 1.0;
        public double RoadHarshness_Sharpness { get => _roadHarshness_Sharpness; set { _roadHarshness_Sharpness = value; Notify(nameof(RoadHarshness_Sharpness)); } }
        private bool _roadHarshness_Enabled = true;
        public bool RoadHarshness_Enabled { get => _roadHarshness_Enabled; set { _roadHarshness_Enabled = value; Notify(nameof(RoadHarshness_Enabled)); } }

        // ── Pre-Impact Protection ─────────────────────────────────────────────
        private double _preImpact_Long = 50.0;
        public double PreImpact_Long { get => _preImpact_Long; set { _preImpact_Long = value; Notify(nameof(PreImpact_Long)); } }
        private double _preImpact_Lateral = 50.0;
        public double PreImpact_Lateral { get => _preImpact_Lateral; set { _preImpact_Lateral = value; Notify(nameof(PreImpact_Lateral)); } }
        private double _preImpact_Duration = 500.0;
        public double PreImpact_Duration { get => _preImpact_Duration; set { _preImpact_Duration = value; Notify(nameof(PreImpact_Duration)); } }
        private bool _preImpact_Enabled = false;
        public bool PreImpact_Enabled { get => _preImpact_Enabled; set { _preImpact_Enabled = value; Notify(nameof(PreImpact_Enabled)); } }
        // ── Motion Primary | SFX — Violent Movement Suppressor ───────────────
        private double _vms_Threshold = 50.0;
        public double VMS_Threshold { get => _vms_Threshold; set { _vms_Threshold = value; Notify(nameof(VMS_Threshold)); } }
        private double _vms_Duration = 3.0;
        public double VMS_Duration { get => _vms_Duration; set { _vms_Duration = value; Notify(nameof(VMS_Duration)); } }
        private bool _vms_Enabled = true;
        public bool VMS_Enabled { get => _vms_Enabled; set { _vms_Enabled = value; Notify(nameof(VMS_Enabled)); } }
    }
}
