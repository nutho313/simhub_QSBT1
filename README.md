# nutho313 QSBT1 ControlMapper

> SimHub plugin for the **Qubic System QS-BT1** seat belt tensioner.  
> Control and monitor all tune parameters directly from SimHub — no browser required.

---

## Features

- 🎛️ Adjust all **9 tunes** of the Seat Belt Tensioner | Base group with nudge buttons
- 🔄 **Live sync** from device every 3 seconds
- ✅ **Profile activation** directly from SimHub
- 📊 All values exposed as **SimHub properties** for dashboards and overlays
- 🎮 Full **Control Mapper actions** (Up / Down / Toggle per parameter)
- 🔍 Read-only view of Motion Primary | SFX, Vehicle Parameters, Seat Belt Tensioner | SFX

---

## Supported Tunes — Seat Belt Tensioner | Base

| Tune | Param 1 | Param 2 | Param 3 |
|---|---|---|---|
| Braking | Gain | Sharpness | Deadzone |
| Acceleration | Gain | Sharpness | Deadzone |
| Sideways Acceleration | Gain | Sharpness | Deadzone |
| Centrifugal Force | Gain | Sharpness | Deadzone |
| Vertical G-Force | Gain | Sharpness | — |
| Road Harshness | Gain | Sharpness | — |
| Side Slip | Threshold | Frequency | Intensity |
| Bounds % | Neutral | Maximum | — |
| Pre-Impact Protection | Long. | Lateral | Duration |

---

## SimHub Properties

All values are available in SimHub dashboards, overlays and NCalc formulas:

```
[QSBT1Plugin.Braking_Gain]          [QSBT1Plugin.Braking_Sharpness]      [QSBT1Plugin.Braking_Deadzone]
[QSBT1Plugin.Acceleration_Gain]     [QSBT1Plugin.Acceleration_Sharpness] [QSBT1Plugin.Acceleration_Deadzone]
[QSBT1Plugin.Sideways_Gain]         [QSBT1Plugin.Sideways_Sharpness]     [QSBT1Plugin.Sideways_Deadzone]
[QSBT1Plugin.Centrifugal_Gain]      [QSBT1Plugin.Centrifugal_Sharpness]  [QSBT1Plugin.Centrifugal_Deadzone]
[QSBT1Plugin.VerticalG_Gain]        [QSBT1Plugin.VerticalG_Sharpness]
[QSBT1Plugin.RoadHarshness_Gain]    [QSBT1Plugin.RoadHarshness_Sharpness]
[QSBT1Plugin.SideSlip_Threshold]    [QSBT1Plugin.SideSlip_Frequency]     [QSBT1Plugin.SideSlip_Intensity]
[QSBT1Plugin.Bounds_Neutral]        [QSBT1Plugin.Bounds_Maximum]
[QSBT1Plugin.PreImpact_Long]        [QSBT1Plugin.PreImpact_Lateral]      [QSBT1Plugin.PreImpact_Duration]
```

Plus `_Enabled` (bool) for each tune.

---

## Control Mapper Actions

Bind any button / wheel encoder to:

```
Braking_Gain_Up / Down          Braking_Sharpness_Up / Down     Braking_Deadzone_Up / Down
Acceleration_Gain_Up / Down     Acceleration_Sharpness_Up / Down
Sideways_Gain_Up / Down         Sideways_Sharpness_Up / Down
Centrifugal_Gain_Up / Down      Centrifugal_Sharpness_Up / Down
VerticalG_Gain_Up / Down        VerticalG_Sharpness_Up / Down
RoadHarshness_Gain_Up / Down    RoadHarshness_Sharpness_Up / Down
SideSlip_Threshold_Up / Down    SideSlip_Frequency_Up / Down    SideSlip_Intensity_Up / Down
Bounds_Neutral_Up / Down        Bounds_Maximum_Up / Down
PreImpact_Long_Up / Down        PreImpact_Lateral_Up / Down     PreImpact_Duration_Up / Down

<Tune>_Toggle     — enable / disable the tune
Braking_Reset     — reset Braking to defaults
Centrifugal_Reset — reset Centrifugal Force to defaults
```

---

## Installation

1. Download **`simhub_QSBT1-vX.X.zip`** from [Releases](https://github.com/nutho313/simhub_QSBT1/releases)
2. Extract `User.QSBT1Plugin.dll` to:
   ```
   Documents\SimHub\Plugins\
   ```
3. Restart SimHub
4. Go to **Additional Plugins** → enable **nutho313.ch QS-BT1 ControlMapper**
5. Open the plugin settings → enter your QS-BT1 IP address → **Test Connection**

---

## Requirements

| | |
|---|---|
| SimHub | 9.x or later |
| .NET Framework | 4.8 |
| QS-BT1 firmware | Web API enabled (default) |
| Network | PC and QS-BT1 on same LAN |

---

## Building from source

### Prerequisites
- Visual Studio 2022 (or Rider)
- SimHub installed at `Documents\SimHub\`

### Steps
```bash
git clone https://github.com/nutho313/simhub_QSBT1.git
cd simhub_QSBT1/src
# Open QSBT1Plugin.csproj in Visual Studio
# Build → DLL is automatically copied to Documents\SimHub\Plugins\
```

> The `.csproj` uses `%USERPROFILE%\Documents\SimHub` as the default SDK path.  
> Override with environment variable `SIMHUB_PATH` if SimHub is installed elsewhere.

---

## Changelog

### v0.1 — Initial Release
- All 9 Base tunes with nudge buttons
- Live device polling every 3 seconds
- Profile activation
- 33 SimHub properties
- 59 Control Mapper actions
- Read-only sections: Motion Primary | SFX, Vehicle Parameters, Seat Belt Tensioner | SFX

---

## License

MIT — feel free to fork and adapt for other QS devices.

---

*Made by nutho313 · Discord: nutho313*
