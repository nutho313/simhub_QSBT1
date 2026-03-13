using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QSBT1Plugin
{
    public class ProfileItem
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public bool   IsActive { get; set; }
        public string Display  => IsActive ? "★ " + Name + "  [ACTIVE]" : Name;
    }

    public partial class SettingsControl : UserControl
    {
        private readonly QSBT1PluginMain _plugin;
        private bool _isLoading = true;

        public SettingsControl(QSBT1PluginMain plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            DataContext = plugin.Settings;
            plugin.Settings.UiDispatcher = Dispatcher;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoading = false;
            _plugin.Settings.NotifyAll();
            LoadLogo();
            UpdateWebLink();
            _ = LoadProfilesAsync();
        }

        // ── Logo ─────────────────────────────────────────────────────────────
        private void LoadLogo()
        {
            try
            {
                var asm      = System.Reflection.Assembly.GetExecutingAssembly();
                var resource = "QSBT1Plugin.nutho313logoblackcircle.png";
                using var stream = asm.GetManifestResourceStream(resource);
                if (stream == null) return;
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.CacheOption  = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                ImgLogo.Source = bmp;
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("[QS-BT1] Logo error: " + ex.Message);
            }
        }

        // ── Web link ─────────────────────────────────────────────────────────
        private void UpdateWebLink()
        {
            TxtWebLink.Text = "http://" + _plugin.Settings.IpAddress + ":" + _plugin.Settings.Port + "/#!/editCurrent";
        }

        private void WebLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(TxtWebLink.Text) { UseShellExecute = true }); }
            catch { }
        }

        // ── Enabled checkbox ─────────────────────────────────────────────────
        private void ChkEnabled_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (!(sender is CheckBox chk) || !chk.IsChecked.HasValue) return;
            bool en  = chk.IsChecked.Value;
            int  pid = _plugin.Settings.Braking_ProfileId;
            int  tg  = _plugin.Settings.Braking_TuneGroup;
            switch ((string)chk.Tag)
            {
                case "braking_enabled":       _plugin.Settings.Braking_Enabled       = en; _plugin.SendTuneEnabled("Braking",               pid, tg, en); break;
                case "acceleration_enabled":  _plugin.Settings.Acceleration_Enabled  = en; _plugin.SendTuneEnabled("Acceleration",           pid, tg, en); break;
                case "sideways_enabled":      _plugin.Settings.Sideways_Enabled      = en; _plugin.SendTuneEnabled("Sideways Acceleration",  pid, tg, en); break;
                case "centrifugal_enabled":   _plugin.Settings.Centrifugal_Enabled   = en; _plugin.SendTuneEnabled("Centrifugal Force",      pid, tg, en); break;
                case "bounds_enabled":        _plugin.Settings.Bounds_Enabled        = en; _plugin.SendTuneEnabled("Bounds",                 pid, tg, en); break;
                case "verticalg_enabled":     _plugin.Settings.VerticalG_Enabled     = en; _plugin.SendTuneEnabled("Vertical G-Force",       pid, tg, en); break;
                case "sideslip_enabled":      _plugin.Settings.SideSlip_Enabled      = en; _plugin.SendTuneEnabled("Side Slip",              pid, tg, en); break;
                case "roadharshness_enabled": _plugin.Settings.RoadHarshness_Enabled = en; _plugin.SendTuneEnabled("Road Harshness",         pid, tg, en); break;
                case "preimpact_enabled":     _plugin.Settings.PreImpact_Enabled     = en; _plugin.SendTuneEnabled("Pre-Impact Protection",  pid, tg, en); break;
            }
        }

        // ── Nudge buttons  < / > ─────────────────────────────────────────────
        private void Nudge_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split('|');
                if (parts.Length != 3) return;
                string profile = parts[0];   // braking / centrifugal
                string param   = parts[1];   // gain / sharpness / deadzone
                int    dir     = parts[2] == "+1" ? +1 : -1;
                _plugin.Adjust(profile, param, dir);
            }
        }

        // ── Auto-detect IP ───────────────────────────────────────────────────
        private async Task DetectIpAsync()
        {
            TxtStatus.Text       = "Scanning network for QS-BT1...";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;

            string localIp = GetLocalIp();
            if (string.IsNullOrEmpty(localIp))
            {
                TxtStatus.Text       = "✘ Could not determine local IP";
                TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            if (await TryIpAsync(_plugin.Settings.IpAddress))
            {
                SetDetectedIp(_plugin.Settings.IpAddress);
                return;
            }

            var prefix = localIp.Substring(0, localIp.LastIndexOf('.') + 1);
            var tasks  = new List<Task<string>>();
            for (int i = 1; i <= 254; i++)
            {
                var ip = prefix + i;
                tasks.Add(Task.Run(async () => await TryIpAsync(ip) ? ip : null));
            }

            var results = await Task.WhenAll(tasks);
            string found = null;
            foreach (var r in results)
                if (r != null) { found = r; break; }

            if (found != null)
                Dispatcher.Invoke(() => SetDetectedIp(found));
            else
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text       = "✘ QS-BT1 not found on network";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                });
        }

        private void SetDetectedIp(string ip)
        {
            _plugin.Settings.IpAddress = ip;
            UpdateWebLink();
            TxtStatus.Text       = "✔ Found QS-BT1 at " + ip;
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            _ = LoadProfilesAsync();
        }

        private async Task<bool> TryIpAsync(string ip)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(600) };
                var url        = "http://" + ip + ":" + _plugin.Settings.Port + "/api/profilesInstalled";
                var response   = await http.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private string GetLocalIp()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                            return ua.Address.ToString();
                }
            }
            catch { }
            return null;
        }

        // ── Profiles ─────────────────────────────────────────────────────────
        private async Task LoadProfilesAsync()
        {
            TxtStatus.Text       = "Loading profiles...";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var url        = "http://" + _plugin.Settings.IpAddress + ":" + _plugin.Settings.Port + "/api/profilesInstalled";
                var json       = await http.GetStringAsync(url);
                var profiles   = ParseProfiles(json);

                Dispatcher.Invoke(() =>
                {
                    var active = profiles.Find(p => p.IsActive);
                    CboProfile.ItemsSource  = profiles;
                    CboProfile.SelectedItem = active ?? profiles.Find(p => p.Id == _plugin.Settings.Braking_ProfileId);

                    var note = active != null
                        ? "✔ " + profiles.Count + " profiles — " + active.Name + " is active  (ID " + active.Id + ")"
                        : "✔ " + profiles.Count + " profiles";
                    TxtStatus.Text       = note;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    UpdateWebLink();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text       = "✘ " + ex.Message;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                });
            }
        }

        private List<ProfileItem> ParseProfiles(string json)
        {
            var list  = new List<ProfileItem>();
            var parts = json.Split(new[] { "{\"id\":" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                try
                {
                    var idEnd = part.IndexOf(',');
                    if (idEnd < 0) continue;
                    if (!int.TryParse(part.Substring(0, idEnd).Trim(), out int id)) continue;

                    var activeIdx = part.IndexOf("\"is_active\":");
                    if (activeIdx < 0) continue;
                    bool isActive = part.Substring(activeIdx + 12, 5).TrimEnd(',', ' ', '}').StartsWith("true");

                    var nameIdx = part.IndexOf("\"name\":\"");
                    if (nameIdx < 0) continue;
                    var nameStart = nameIdx + 8;
                    var nameEnd   = part.IndexOf('"', nameStart);
                    if (nameEnd < 0) continue;

                    list.Add(new ProfileItem { Id = id, Name = part.Substring(nameStart, nameEnd - nameStart), IsActive = isActive });
                }
                catch { }
            }
            list.Sort((a, b) => a.IsActive != b.IsActive ? (a.IsActive ? -1 : 1)
                : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        // ── Profile combo ─────────────────────────────────────────────────────
        private void CboProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (CboProfile.SelectedItem is ProfileItem p)
            {
                _plugin.Settings.Braking_ProfileId     = p.Id;
                _plugin.Settings.Centrifugal_ProfileId = p.Id;
            }
        }

        private async Task ActivateSelectedProfileAsync()
        {
            if (!(CboProfile.SelectedItem is ProfileItem p))
            {
                TxtStatus.Text       = "✘ No profile selected";
                TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }
            TxtStatus.Text       = "Activating " + p.Name + "...";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var url     = "http://" + _plugin.Settings.IpAddress + ":" + _plugin.Settings.Port + "/api/activateProfile";
                var json    = "{\"profileId\":" + p.Id + "}";
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp    = await http.PostAsync(url, content);
                if (resp.IsSuccessStatusCode)
                {
                    TxtStatus.Text       = "✔ Activated — " + p.Name + "  (ID " + p.Id + ")";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    // Refresh dropdown to reflect new active state, then re-read tune values
                    await LoadProfilesAsync();
                }
                else
                {
                    TxtStatus.Text       = "✘ HTTP " + (int)resp.StatusCode;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text       = "✘ " + ex.Message;
                TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        // ── Buttons ───────────────────────────────────────────────────────────
        private async void Btn_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString();
            switch (tag)
            {
                case "detect":   await DetectIpAsync();     break;
                case "activate": await ActivateSelectedProfileAsync(); break;
                case "test":
                    TxtStatus.Text       = "Testing...";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
                    try
                    {
                        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                        var url        = "http://" + _plugin.Settings.IpAddress + ":" + _plugin.Settings.Port + "/api/profilesInstalled";
                        var resp       = await http.GetAsync(url);
                        TxtStatus.Text       = resp.IsSuccessStatusCode ? "✔ Connected (" + (int)resp.StatusCode + ")" : "⚠ HTTP " + (int)resp.StatusCode;
                        TxtStatus.Foreground = resp.IsSuccessStatusCode ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Orange;
                    }
                    catch (Exception ex)
                    {
                        TxtStatus.Text       = "✘ " + ex.Message;
                        TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    }
                    break;
            }
        }
    }
}
