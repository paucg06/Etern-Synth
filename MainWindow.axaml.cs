using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace EternSynth
{
    public partial class MainWindow : Window
    {
        private SfxrSynth synth = new SfxrSynth();
        private SynthDatabase db;
        private SavedSound activeSound;
        private bool playOnChange = true;
        private bool isUpdatingSliders = false;

        private Dictionary<string, Slider> parameterSliders = new Dictionary<string, Slider>();
        private Button[] shapeButtons = new Button[5];
        
        private StackPanel presetsPanel;
        private StackPanel slidersPanel;
        private ListBox lstHistory;
        private Canvas waveCanvas;
        private Slider volumeSlider;
        private CheckBox chkPlayOnChange;
        private TextBlock txtSoundInfo;
        private TextBlock txtActiveSoundName;
        private TextBlock txtActiveSoundDesc;

        private Border sidebarBorder;
        private Button btnCollapse;
        private Button btnExpand;
        private Grid contentGrid;
        
        public MainWindow()
        {
            InitializeComponent();
            
            db = Storage.Load();

            // Locate elements
            presetsPanel = this.FindControl<StackPanel>("PresetsPanel");
            slidersPanel = this.FindControl<StackPanel>("SlidersPanel");
            lstHistory = this.FindControl<ListBox>("LstHistory");
            waveCanvas = this.FindControl<Canvas>("WaveCanvas");
            volumeSlider = this.FindControl<Slider>("VolumeSlider");
            chkPlayOnChange = this.FindControl<CheckBox>("ChkPlayOnChange");
            txtSoundInfo = this.FindControl<TextBlock>("TxtSoundInfo");
            txtActiveSoundName = this.FindControl<TextBlock>("TxtActiveSoundName");
            txtActiveSoundDesc = this.FindControl<TextBlock>("TxtActiveSoundDesc");
            sidebarBorder = this.FindControl<Border>("SidebarBorder");
            btnCollapse = this.FindControl<Button>("BtnCollapse");
            btnExpand = this.FindControl<Button>("BtnExpand");
            contentGrid = this.FindControl<Grid>("ContentGrid");

            // Setup Zoom / Escala Slider logic
            var zoomSlider = this.FindControl<Slider>("ZoomSlider");
            var txtZoomVal = this.FindControl<TextBlock>("TxtZoomVal");
            var workspaceContentGrid = this.FindControl<Grid>("WorkspaceContentGrid");
            if (zoomSlider != null && txtZoomVal != null && workspaceContentGrid != null)
            {
                var scaleTransform = new ScaleTransform(1, 1);
                workspaceContentGrid.RenderTransform = scaleTransform;
                zoomSlider.ValueChanged += (s, e) =>
                {
                    scaleTransform.ScaleX = zoomSlider.Value;
                    scaleTransform.ScaleY = zoomSlider.Value;
                    txtZoomVal.Text = Math.Round(zoomSlider.Value * 100) + "%";
                };
            }

            // Bind check box state
            if (chkPlayOnChange != null)
            {
                chkPlayOnChange.IsCheckedChanged += (s, e) => { playOnChange = chkPlayOnChange.IsChecked ?? false; };
            }

            // Resolve Wave shape buttons from grid
            var waveSelectorGrid = this.FindControl<Grid>("WaveSelectorGrid");
            if (waveSelectorGrid != null)
            {
                foreach (var child in waveSelectorGrid.Children)
                {
                    if (child is Button btn && btn.Tag != null)
                    {
                        int idx = int.Parse(btn.Tag.ToString());
                        shapeButtons[idx] = btn;
                    }
                }
            }

            SetupPresetsPanel();
            SetupSlidersPanel();

            if (volumeSlider != null)
            {
                volumeSlider.Value = synth.sound_vol;
                volumeSlider.ValueChanged += (s, e) => { synth.sound_vol = (float)volumeSlider.Value; OnParameterChanged(); };
            }

            RefreshHistoryList();

            if (db.Sounds.Count > 0)
            {
                SelectSound(db.Sounds[0]);
            }
            else
            {
                GenerateNewPreset("Coin");
            }

            // Redraw when Canvas bounds are resized/loaded
            if (waveCanvas != null)
            {
                waveCanvas.SizeChanged += (s, e) =>
                {
                    if (synth != null)
                    {
                        DrawWaveform(synth.SynthesizeSamples());
                    }
                };
            }
        }

        private void SetupPresetsPanel()
        {
            presetsPanel.Children.Clear();

            Action<string, string> addPresetBtn = (name, type) =>
            {
                var btn = new Button
                {
                    Content = name,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Background = SolidColorBrush.Parse("#1e1e1f"),
                    Foreground = Brushes.White,
                    Padding = new Thickness(0, 7),
                    CornerRadius = new CornerRadius(4),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                
                btn.Click += (s, e) => GenerateNewPreset(type);
                presetsPanel.Children.Add(btn);
            };

            addPresetBtn("🪙 Moneda", "Coin");
            addPresetBtn("🔫 Disparo / Laser", "Laser");
            addPresetBtn("💥 Explosión", "Explosion");
            addPresetBtn("⚡ Powerup", "Powerup");
            addPresetBtn("🤕 Daño / Golpe", "HitHurt");
            addPresetBtn("🦘 Salto", "Jump");
            addPresetBtn("👾 Blip / Selecc.", "Blip");
            
            // Separator
            presetsPanel.Children.Add(new Border { BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = SolidColorBrush.Parse("#2d2d2d"), Margin = new Thickness(0, 5) });

            var btnRnd = new Button
            {
                Content = "🎲 Aleatorio",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = SolidColorBrush.Parse("#1e1e1f"),
                Foreground = Brushes.White,
                Padding = new Thickness(0, 7),
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.Bold,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            btnRnd.Click += (s, e) => GenerateNewPreset("Random");
            presetsPanel.Children.Add(btnRnd);
        }

        private void SetupSlidersPanel()
        {
            slidersPanel.Children.Clear();

            // Group: ENVOLVENTE
            AddGroupHeader("ENVOLVENTE DE VOLUMEN");
            AddParameterSlider("Tiempo de Ataque", "p_env_attack", () => synth.p_env_attack, v => synth.p_env_attack = v);
            AddParameterSlider("Tiempo de Sostenido", "p_env_sustain", () => synth.p_env_sustain, v => synth.p_env_sustain = v);
            AddParameterSlider("Ataque Fuerte (Punch)", "p_env_punch", () => synth.p_env_punch, v => synth.p_env_punch = v);
            AddParameterSlider("Tiempo de Decaimiento", "p_env_decay", () => synth.p_env_decay, v => synth.p_env_decay = v);

            // Group: FRECUENCIA (TONO)
            AddGroupHeader("FRECUENCIA Y TONO");
            AddParameterSlider("Frecuencia Inicial", "p_base_freq", () => synth.p_base_freq, v => synth.p_base_freq = v);
            AddParameterSlider("Límite de Frecuencia", "p_freq_limit", () => synth.p_freq_limit, v => synth.p_freq_limit = v);
            AddParameterSlider("Deslizamiento Frec.", "p_freq_ramp", () => synth.p_freq_ramp, v => synth.p_freq_ramp = v, -1.0, 1.0);
            AddParameterSlider("Aceleración Frec.", "p_freq_dramp", () => synth.p_freq_dramp, v => synth.p_freq_dramp = v, -1.0, 1.0);

            // Group: VIBRATO
            AddGroupHeader("VIBRATO DE TONO");
            AddParameterSlider("Profundidad de Vibrato", "p_vib_strength", () => synth.p_vib_strength, v => synth.p_vib_strength = v);
            AddParameterSlider("Velocidad de Vibrato", "p_vib_speed", () => synth.p_vib_speed, v => synth.p_vib_speed = v);

            // Group: ARPEGIO (SALTOS)
            AddGroupHeader("CAMBIO DE TONO (ARPEGIO)");
            AddParameterSlider("Velocidad de Arpegio", "p_arp_speed", () => synth.p_arp_speed, v => synth.p_arp_speed = v);
            AddParameterSlider("Cantidad de Arpegio", "p_arp_mod", () => synth.p_arp_mod, v => synth.p_arp_mod = v, -1.0, 1.0);

            // Group: ONDA CUADRADA
            AddGroupHeader("CICLO DE TRABAJO (ONDA CUADRADA)");
            AddParameterSlider("Ancho de Pulso (Duty)", "p_duty", () => synth.p_duty, v => synth.p_duty = v);
            AddParameterSlider("Barrido de Pulso", "p_duty_ramp", () => synth.p_duty_ramp, v => synth.p_duty_ramp = v, -1.0, 1.0);

            // Group: FILTROS
            AddGroupHeader("FILTROS DE AUDIO");
            AddParameterSlider("Corte Filtro Paso-Bajo", "p_lpf_freq", () => synth.p_lpf_freq, v => synth.p_lpf_freq = v);
            AddParameterSlider("Barrido Paso-Bajo", "p_lpf_ramp", () => synth.p_lpf_ramp, v => synth.p_lpf_ramp = v, -1.0, 1.0);
            AddParameterSlider("Resonancia Paso-Bajo", "p_lpf_resonance", () => synth.p_lpf_resonance, v => synth.p_lpf_resonance = v);
            AddParameterSlider("Corte Filtro Paso-Alto", "p_hpf_freq", () => synth.p_hpf_freq, v => synth.p_hpf_freq = v);
            AddParameterSlider("Barrido Paso-Alto", "p_hpf_ramp", () => synth.p_hpf_ramp, v => synth.p_hpf_ramp = v, -1.0, 1.0);

            // Group: PHASER
            AddGroupHeader("EFECTO PHASER / FLANGER");
            AddParameterSlider("Desfase de Phaser", "p_pha_offset", () => synth.p_pha_offset, v => synth.p_pha_offset = v, -1.0, 1.0);
            AddParameterSlider("Barrido de Phaser", "p_pha_ramp", () => synth.p_pha_ramp, v => synth.p_pha_ramp = v, -1.0, 1.0);

            // Group: REPETICIÓN
            AddGroupHeader("REPETICIÓN DE SONIDO");
            AddParameterSlider("Velocidad de Repetición", "p_repeat_speed", () => synth.p_repeat_speed, v => synth.p_repeat_speed = v);
        }

        private void AddGroupHeader(string title)
        {
            var header = new TextBlock
            {
                Text = title,
                Foreground = SolidColorBrush.Parse("#58a6ff"),
                FontSize = 9.5,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            };
            slidersPanel.Children.Add(header);
        }

        private void AddParameterSlider(string label, string paramName, Func<float> getter, Action<float> setter, double min = 0.0, double max = 1.0)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition(145, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(45, GridUnitType.Pixel));

            var txtLabel = new TextBlock
            {
                Text = label,
                Foreground = SolidColorBrush.Parse("#AAAAAA"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtLabel, 0);
            grid.Children.Add(txtLabel);

            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = getter(),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(slider, 1);
            grid.Children.Add(slider);

            var txtValue = new TextBlock
            {
                Text = slider.Value.ToString("F2"),
                Foreground = Brushes.White,
                FontSize = 10.5,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtValue, 2);
            grid.Children.Add(txtValue);

            slider.ValueChanged += (s, e) =>
            {
                if (isUpdatingSliders) return;
                setter((float)slider.Value);
                txtValue.Text = slider.Value.ToString("F2");
                OnParameterChanged();
            };

            parameterSliders[paramName] = slider;
            slidersPanel.Children.Add(grid);
        }

        private void SetWaveType(int index)
        {
            synth.wave_type = index;
            UpdateWaveButtonsSelection();
            OnParameterChanged();
        }

        private void UpdateWaveButtonsSelection()
        {
            for (int i = 0; i < 5; i++)
            {
                if (shapeButtons[i] == null) continue;
                if (i == synth.wave_type)
                {
                    shapeButtons[i].Background = SolidColorBrush.Parse("#58a6ff");
                    shapeButtons[i].Foreground = Brushes.Black;
                }
                else
                {
                    shapeButtons[i].Background = SolidColorBrush.Parse("#1e1e1f");
                    shapeButtons[i].Foreground = Brushes.White;
                }
            }
        }

        private void OnParameterChanged()
        {
            if (isUpdatingSliders) return;
            
            // Re-synthesize and refresh wave canvas
            List<float> samples = synth.SynthesizeSamples();
            DrawWaveform(samples);

            // Update active sound params
            if (activeSound != null)
            {
                activeSound.ParamsString = synth.GetSettingsString();
                Storage.Save(db);
            }

            // Play sound if playOnChange is true
            if (playOnChange)
            {
                byte[] wav = synth.GenerateWav();
                CrossPlatformAudioPlayer.PlayWav(wav);
            }

            txtSoundInfo.Text = string.Format("Muestras: {0} | Canales: Mono", samples.Count);
            if (txtActiveSoundDesc != null)
            {
                txtActiveSoundDesc.Text = "Tipo de Onda: " + GetWaveTypeName(synth.wave_type) + " | Parámetros: " + synth.GetSettingsString().Split(',').Length;
            }
        }

        private string GetWaveTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Cuadrada";
                case 1: return "Sierra";
                case 2: return "Senoidal";
                case 3: return "Ruido";
                case 4: return "Triangular";
                default: return "Desconocida";
            }
        }

        private void PlayActiveSound()
        {
            byte[] wav = synth.GenerateWav();
            CrossPlatformAudioPlayer.PlayWav(wav);
        }

        private void GenerateNewPreset(string type)
        {
            string baseName = "Sonido";
            switch (type)
            {
                case "Coin": synth.GenerateCoin(); baseName = "Moneda"; break;
                case "Laser": synth.GenerateLaser(); baseName = "Láser"; break;
                case "Explosion": synth.GenerateExplosion(); baseName = "Explosión"; break;
                case "Powerup": synth.GeneratePowerup(); baseName = "Powerup"; break;
                case "HitHurt": synth.GenerateHitHurt(); baseName = "Daño"; break;
                case "Jump": synth.GenerateJump(); baseName = "Salto"; break;
                case "Blip": synth.GenerateBlipSelect(); baseName = "Blip"; break;
                case "Random": synth.GenerateRandom(); baseName = "Aleatorio"; break;
            }

            // Add new sound to database
            string countSuffix = (db.Sounds.Count(s => s.Name.StartsWith(baseName)) + 1).ToString();
            var sound = new SavedSound
            {
                Name = baseName + " " + countSuffix,
                ParamsString = synth.GetSettingsString()
            };
            db.Sounds.Add(sound);
            Storage.Save(db);

            RefreshHistoryList();
            lstHistory.SelectedItem = sound;
        }

        private void RefreshHistoryList()
        {
            lstHistory.ItemsSource = null;
            lstHistory.ItemsSource = db.Sounds;
            // ListBox in Avalonia doesn't require DisplayMemberPath if we override the item display,
            // but we can bind or map string list items.
            // A simple map or custom datatemplate isn't strictly needed if we map ListBoxItem contents.
            // Let's populate the ListBoxItems manually to keep styling clean!
            var items = new List<ListBoxItem>();
            foreach (var sound in db.Sounds)
            {
                var lbi = new ListBoxItem
                {
                    Content = sound.Name,
                    Tag = sound
                };
                items.Add(lbi);
            }
            lstHistory.ItemsSource = items;
        }

        private void LstHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstHistory.SelectedItem is ListBoxItem lbi && lbi.Tag is SavedSound sound)
            {
                SelectSound(sound);
            }
        }

        private void SelectSound(SavedSound sound)
        {
            activeSound = sound;
            synth.SetSettingsString(sound.ParamsString);

            // Update UI sliders & buttons
            isUpdatingSliders = true;
            try
            {
                UpdateWaveButtonsSelection();
                
                foreach (var kp in parameterSliders)
                {
                    string name = kp.Key;
                    Slider slider = kp.Value;
                    float val = 0;
                    switch (name)
                    {
                        case "p_env_attack": val = synth.p_env_attack; break;
                        case "p_env_sustain": val = synth.p_env_sustain; break;
                        case "p_env_punch": val = synth.p_env_punch; break;
                        case "p_env_decay": val = synth.p_env_decay; break;
                        case "p_base_freq": val = synth.p_base_freq; break;
                        case "p_freq_limit": val = synth.p_freq_limit; break;
                        case "p_freq_ramp": val = synth.p_freq_ramp; break;
                        case "p_freq_dramp": val = synth.p_freq_dramp; break;
                        case "p_vib_strength": val = synth.p_vib_strength; break;
                        case "p_vib_speed": val = synth.p_vib_speed; break;
                        case "p_arp_speed": val = synth.p_arp_speed; break;
                        case "p_arp_mod": val = synth.p_arp_mod; break;
                        case "p_duty": val = synth.p_duty; break;
                        case "p_duty_ramp": val = synth.p_duty_ramp; break;
                        case "p_repeat_speed": val = synth.p_repeat_speed; break;
                        case "p_pha_offset": val = synth.p_pha_offset; break;
                        case "p_pha_ramp": val = synth.p_pha_ramp; break;
                        case "p_lpf_freq": val = synth.p_lpf_freq; break;
                        case "p_lpf_ramp": val = synth.p_lpf_ramp; break;
                        case "p_lpf_resonance": val = synth.p_lpf_resonance; break;
                        case "p_hpf_freq": val = synth.p_hpf_freq; break;
                        case "p_hpf_ramp": val = synth.p_hpf_ramp; break;
                    }
                    slider.Value = val;
                }

                if (volumeSlider != null) volumeSlider.Value = synth.sound_vol;
            }
            finally
            {
                isUpdatingSliders = false;
            }

            // Update Header Name
            if (txtActiveSoundName != null) txtActiveSoundName.Text = sound.Name;
            if (txtActiveSoundDesc != null) txtActiveSoundDesc.Text = "Tipo de Onda: " + GetWaveTypeName(synth.wave_type) + " | Parámetros: " + synth.GetSettingsString().Split(',').Length;

            // Synthesize and redraw
            List<float> samples = synth.SynthesizeSamples();
            DrawWaveform(samples);
            txtSoundInfo.Text = string.Format("Muestras: {0} | Canales: Mono", samples.Count);

            if (playOnChange)
            {
                byte[] wav = synth.GenerateWav();
                CrossPlatformAudioPlayer.PlayWav(wav);
            }
        }

        private void DrawWaveform(List<float> samples)
        {
            if (waveCanvas == null) return;
            waveCanvas.Children.Clear();
            if (samples == null || samples.Count == 0) return;

            double width = waveCanvas.Bounds.Width;
            double height = waveCanvas.Bounds.Height;
            if (width <= 0) width = 250;
            if (height <= 0) height = 100;

            // Background baseline grid line
            var baseline = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(0, height / 2.0),
                EndPoint = new Point(width, height / 2.0),
                Stroke = SolidColorBrush.Parse("#18ffffff"),
                StrokeThickness = 1
            };
            waveCanvas.Children.Add(baseline);

            var points = new List<Point>();
            int step = Math.Max(1, samples.Count / 200);
            for (int i = 0; i < samples.Count; i += step)
            {
                double x = (double)i / samples.Count * width;
                double y = (height / 2.0) - (samples[i] * (height / 2.0) * 0.9);
                points.Add(new Point(x, y));
            }

            var polyline = new Avalonia.Controls.Shapes.Polyline
            {
                Points = points,
                Stroke = SolidColorBrush.Parse("#58a6ff"),
                StrokeThickness = 1.5
            };

            waveCanvas.Children.Add(polyline);
        }

        // Action Handlers
        private void PlayActiveSound_Click(object sender, RoutedEventArgs e) => PlayActiveSound();
        private void BtnNewSound_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Random");
        
        private void SetWaveType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                SetWaveType(int.Parse(btn.Tag.ToString()));
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (sidebarBorder == null || btnExpand == null || btnCollapse == null || contentGrid == null) return;
            if (sidebarBorder.IsVisible)
            {
                sidebarBorder.IsVisible = false;
                btnExpand.IsVisible = true;
                btnCollapse.IsVisible = false;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(0);
            }
            else
            {
                sidebarBorder.IsVisible = true;
                btnExpand.IsVisible = false;
                btnCollapse.IsVisible = true;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(240);
            }
        }

        private async void Mutate_Click(object sender, RoutedEventArgs e)
        {
            synth.Mutate();
            string baseName = activeSound != null ? activeSound.Name : "Mutado";
            if (!baseName.Contains("Mutación")) baseName += " (Mutación)";
            
            var sound = new SavedSound
            {
                Name = baseName,
                ParamsString = synth.GetSettingsString()
            };
            db.Sounds.Add(sound);
            Storage.Save(db);

            RefreshHistoryList();
            
            // Select in list
            foreach (var item in lstHistory.Items)
            {
                if (item is ListBoxItem lbi && lbi.Tag is SavedSound s && s.Id == sound.Id)
                {
                    lstHistory.SelectedItem = lbi;
                    break;
                }
            }
        }

        private void Random_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Random");

        private void WaveCuadrada_Click(object sender, RoutedEventArgs e) => SetWaveType(0);
        private void WaveSierra_Click(object sender, RoutedEventArgs e) => SetWaveType(1);
        private void WaveSenoidal_Click(object sender, RoutedEventArgs e) => SetWaveType(2);
        private void WaveRuido_Click(object sender, RoutedEventArgs e) => SetWaveType(3);
        private void WaveTriangular_Click(object sender, RoutedEventArgs e) => SetWaveType(4);

        private void PresetCoin_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Coin");
        private void PresetLaser_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Laser");
        private void PresetExplosion_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Explosion");
        private void PresetPowerup_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Powerup");
        private void PresetHitHurt_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("HitHurt");
        private void PresetJump_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Jump");
        private void PresetBlip_Click(object sender, RoutedEventArgs e) => GenerateNewPreset("Blip");

        private void ResetParams_Click(object sender, RoutedEventArgs e)
        {
            synth.ResetParams();
            if (activeSound != null)
            {
                activeSound.ParamsString = synth.GetSettingsString();
                Storage.Save(db);
            }
            SelectSound(activeSound ?? db.Sounds.FirstOrDefault());
        }

        private async void RenameSelectedSound()
        {
            if (lstHistory.SelectedItem is ListBoxItem lbi && lbi.Tag is SavedSound sound)
            {
                var res = await ShowInputDialog("Renombrar Sonido", "Ingresa el nuevo nombre:", sound.Name);
                if (!string.IsNullOrEmpty(res))
                {
                    sound.Name = res.Trim();
                    Storage.Save(db);
                    RefreshHistoryList();
                    
                    // Re-select
                    foreach (var item in lstHistory.Items)
                    {
                        if (item is ListBoxItem li && li.Tag is SavedSound s && s.Id == sound.Id)
                        {
                            lstHistory.SelectedItem = li;
                            break;
                        }
                    }
                }
            }
        }

        private void DeleteSelectedSound()
        {
            if (lstHistory.SelectedItem is ListBoxItem lbi && lbi.Tag is SavedSound sound)
            {
                db.Sounds.Remove(sound);
                Storage.Save(db);
                RefreshHistoryList();

                if (db.Sounds.Count > 0)
                {
                    lstHistory.SelectedIndex = db.Sounds.Count - 1;
                }
                else
                {
                    GenerateNewPreset("Coin");
                }
            }
        }

        // File Operations via StorageProvider
        private async void ExportWav_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar archivo .WAV",
                DefaultExtension = "wav",
                SuggestedFileName = activeSound != null ? activeSound.Name + ".wav" : "sonido.wav",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Archivo de audio WAV") { Patterns = new[] { "*.wav" } }
                }
            });

            if (file != null)
            {
                try
                {
                    byte[] wav = synth.GenerateWav();
                    using (var stream = await file.OpenWriteAsync())
                    {
                        await stream.WriteAsync(wav, 0, wav.Length);
                    }
                    await ShowCustomMessageBox("Sonido exportado correctamente.", "Éxito");
                }
                catch (Exception ex)
                {
                    await ShowCustomMessageBox("Error al exportar: " + ex.Message, "Error");
                }
            }
        }

        private async void SaveParams_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar parámetros de EternSynth",
                DefaultExtension = "json",
                SuggestedFileName = activeSound != null ? activeSound.Name + ".json" : "parametros.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Parámetros JSON") { Patterns = new[] { "*.json", "*.txt" } }
                }
            });

            if (file != null)
            {
                try
                {
                    string content = synth.GetSettingsString();
                    using (var stream = await file.OpenWriteAsync())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(content);
                    }
                    await ShowCustomMessageBox("Parámetros guardados correctamente.", "Éxito");
                }
                catch (Exception ex)
                {
                    await ShowCustomMessageBox("Error al guardar: " + ex.Message, "Error");
                }
            }
        }

        private async void LoadParams_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Cargar parámetros de EternSynth",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Parámetros JSON") { Patterns = new[] { "*.json", "*.txt" } }
                }
            });

            if (files != null && files.Count > 0)
            {
                try
                {
                    string content;
                    using (var stream = await files[0].OpenReadAsync())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        content = await reader.ReadToEndAsync();
                    }

                    if (!string.IsNullOrEmpty(content))
                    {
                        var newSound = new SavedSound
                        {
                            Name = System.IO.Path.GetFileNameWithoutExtension(files[0].Name),
                            ParamsString = content.Trim()
                        };
                        db.Sounds.Add(newSound);
                        Storage.Save(db);

                        RefreshHistoryList();
                        
                        // Select in list
                        foreach (var item in lstHistory.Items)
                        {
                            if (item is ListBoxItem li && li.Tag is SavedSound s && s.Id == newSound.Id)
                            {
                                lstHistory.SelectedItem = li;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ShowCustomMessageBox("Error al cargar: " + ex.Message, "Error");
                }
            }
        }

        private async void CopyParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null && topLevel.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(synth.GetSettingsString());
                    await ShowCustomMessageBox("Parámetros copiados al portapapeles.", "Portapapeles");
                }
            }
            catch (Exception ex)
            {
                await ShowCustomMessageBox("Error al copiar: " + ex.Message, "Error");
            }
        }

        private async void PasteParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null && topLevel.Clipboard != null)
                {
                    string content = await topLevel.Clipboard.GetTextAsync();
                    if (string.IsNullOrEmpty(content) || !content.Contains(","))
                    {
                        await ShowCustomMessageBox("El portapapeles no contiene parámetros válidos.", "Error");
                        return;
                    }

                    var newSound = new SavedSound
                    {
                        Name = "Pegado " + (db.Sounds.Count + 1),
                        ParamsString = content.Trim()
                    };
                    db.Sounds.Add(newSound);
                    Storage.Save(db);

                    RefreshHistoryList();
                    
                    // Select in list
                    foreach (var item in lstHistory.Items)
                    {
                        if (item is ListBoxItem li && li.Tag is SavedSound s && s.Id == newSound.Id)
                        {
                            lstHistory.SelectedItem = li;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowCustomMessageBox("Error al pegar: " + ex.Message, "Error");
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            await ShowCustomMessageBox(
                "🚀 EternSynth v1.0 // Generador de Sonidos Retro\n\n" +
                "Inspirado en el clásico SFXR / BFXR.\n" +
                "Desarrollado para la síntesis nativa y procedimental de efectos de sonido de 8 bits en Windows, macOS y Linux.\n\n" +
                "© 2026 Etern Studio.",
                "Acerca de EternSynth"
            );
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // Custom Modal Helpers
        public async System.Threading.Tasks.Task ShowCustomMessageBox(string message, string title)
        {
            var win = new Window
            {
                Title = title,
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = SolidColorBrush.Parse("#1a1a1a"),
                CanResize = false,
                ShowInTaskbar = false
            };

            var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
            stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = Brushes.White });
            stack.Children.Add(new TextBlock { Text = message, FontSize = 11.5, Foreground = SolidColorBrush.Parse("#cccccc"), TextWrapping = TextWrapping.Wrap });
            
            var btnOk = new Button { Content = "Aceptar", HorizontalAlignment = HorizontalAlignment.Right, Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center, Background = SolidColorBrush.Parse("#007acc"), Foreground = Brushes.White };
            btnOk.Click += (s, e) => win.Close();
            stack.Children.Add(btnOk);

            win.Content = stack;
            await win.ShowDialog(this);
        }

        public async System.Threading.Tasks.Task<string> ShowInputDialog(string title, string message, string defaultValue = "")
        {
            var win = new Window
            {
                Title = title,
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = SolidColorBrush.Parse("#1a1a1a"),
                CanResize = false,
                ShowInTaskbar = false
            };

            var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
            stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = Brushes.White });
            stack.Children.Add(new TextBlock { Text = message, FontSize = 10, FontWeight = FontWeight.Bold, Foreground = SolidColorBrush.Parse("#888888") });
            
            var txtInput = new TextBox { Text = defaultValue, Width = 340, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(txtInput);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
            
            var btnCancel = new Button { Content = "Cancelar", Background = SolidColorBrush.Parse("#323233"), Foreground = Brushes.White, Width = 75, HorizontalContentAlignment = HorizontalAlignment.Center };
            btnCancel.Click += (s, e) => win.Close(null);
            buttons.Children.Add(btnCancel);

            var btnOk = new Button { Content = "Aceptar", Background = SolidColorBrush.Parse("#007acc"), Foreground = Brushes.White, Width = 75, HorizontalContentAlignment = HorizontalAlignment.Center };
            btnOk.Click += (s, e) => win.Close(txtInput.Text);
            buttons.Children.Add(btnOk);

            stack.Children.Add(buttons);
            win.Content = stack;

            return await win.ShowDialog<string>(this);
        }
    }

    // Cross-Platform Playback Wrapper
    // Uses shell commands on all platforms - no extra NuGet packages required.
    // Windows: PowerShell Media.SoundPlayer | macOS: afplay | Linux: aplay
    public static class CrossPlatformAudioPlayer
    {
        public static void PlayWav(byte[] wavBytes)
        {
            if (wavBytes == null) return;

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "eternsynth_temp.wav");
            try
            {
                File.WriteAllBytes(tempFile, wavBytes);

                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    // Windows: use PowerShell built-in Media.SoundPlayer (no extra assembly needed)
                    psi.FileName = "powershell";
                    psi.Arguments = $"-NoProfile -NonInteractive -Command \"(New-Object Media.SoundPlayer '{tempFile}').PlaySync()\"";
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                             System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    // macOS: afplay is built-in
                    psi.FileName = "afplay";
                    psi.Arguments = tempFile;
                }
                else
                {
                    // Linux: aplay (ALSA utils)
                    psi.FileName = "aplay";
                    psi.Arguments = tempFile;
                }

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Audio playback failed: " + ex.Message);
            }
        }
    }
}
