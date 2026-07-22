using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Media;

namespace EternSynth
{
    public class WpfMainWindow : Window
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new Application();
                app.Run(new WpfMainWindow());
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash_log.txt", ex.ToString());
            }
        }

        // Color definitions for Dark Premium theme
        private static readonly Brush BgMain = new SolidColorBrush(Color.FromRgb(18, 18, 18));
        private static readonly Brush BgSidebar = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        private static readonly Brush BgCard = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        private static readonly Brush BorderColor = new SolidColorBrush(Color.FromRgb(45, 45, 48));
        
        private static readonly Brush TextActive = Brushes.White;
        private static readonly Brush TextMuted = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        private static readonly Brush AccentBlue = new SolidColorBrush(Color.FromRgb(88, 166, 255));
        private static readonly Brush AccentGreen = new SolidColorBrush(Color.FromRgb(57, 211, 83));
        private static readonly Brush AccentOrange = new SolidColorBrush(Color.FromRgb(240, 136, 62));
        private static readonly Brush AccentRed = new SolidColorBrush(Color.FromRgb(248, 81, 73));
        private static readonly Brush AccentPurple = new SolidColorBrush(Color.FromRgb(188, 140, 255));

        // Model & Database State
        private SfxrSynth synth = new SfxrSynth();
        private SynthDatabase db;
        private SavedSound activeSound;
        private bool playOnChange = true;
        private bool isUpdatingSliders = false;

        // UI Playback & Layout references
        private SoundPlayer activePlayer = null;
        private MemoryStream activeStream = null;
        private Grid mainGrid;
        private UIElement menuBarControl;
        private bool isMenuContextOpen = false;
        private Border btnCollapseSidebar;
        private Border btnExpandSidebar;
        private Grid contentGrid;
        private Grid workspaceGrid;
        private double sidebarPreviousWidth = 240;
        private bool isSidebarCollapsed = false;
        private ScaleTransform layoutScale;
        private TextBlock txtActiveSoundName;
        private TextBlock txtActiveSoundDesc;

        // UI Controls
        private Canvas waveCanvas;
        private ListBox lstHistory;
        private StackPanel slidersPanel;
        private CheckBox chkPlayOnChange;
        private Slider volumeSlider;
        private TextBlock txtSoundInfo;
        
        // Shape Selector Buttons
        private Button[] shapeButtons = new Button[5];

        // Sliders Reference Dictionary
        private Dictionary<string, Slider> parameterSliders = new Dictionary<string, Slider>();

        public WpfMainWindow()
        {
            // Set Window styling
            Title = "EternSynth // Generador de Sonidos Retro";
            Width = 1120;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = BgMain;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
            ResizeMode = ResizeMode.CanResize;

            // Load saved sounds database
            db = Storage.Load();

            // Set up WPF styling overrides
            SetupGlobalStyles();

            // Root layout Grid
            mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // Custom title bar (Row 0)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });  // Auto-hiding menu bar (Row 1)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Workspace (Row 2)
            this.Content = mainGrid;

            // Title Bar
            var titleBar = CreateTitleBar();
            mainGrid.Children.Add(titleBar);
            Grid.SetRow(titleBar, 0);

            // Menu Bar (Initially hidden)
            menuBarControl = CreateMenuBarControl();
            menuBarControl.Visibility = Visibility.Collapsed;
            mainGrid.Children.Add(menuBarControl);
            Grid.SetRow(menuBarControl, 1);

            // Workspace Layout Grid (Row 2)
            contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240), MinWidth = 180, MaxWidth = 400 }); // Col 0: Sidebar
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) }); // Col 1: Splitter
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 2: Workspace
            
            mainGrid.Children.Add(contentGrid);
            Grid.SetRow(contentGrid, 2);

            // Hover triggers to show/hide menu bar on title bar / workspace interaction
            titleBar.MouseEnter += (s, e) => ShowTopMenuBar();
            menuBarControl.MouseEnter += (s, e) => ShowTopMenuBar();
            contentGrid.MouseEnter += (s, e) => { if (!isMenuContextOpen) HideTopMenuBar(); };

            // Sidebar container
            var sidebarBorder = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            contentGrid.Children.Add(sidebarBorder);
            Grid.SetColumn(sidebarBorder, 0);

            var sidebarGrid = new Grid();
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) }); // Section Title
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Saved Sounds List
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(90) }); // Footer Actions & Zoom Scale
            sidebarBorder.Child = sidebarGrid;

            // Sidebar Header
            var sidebarHeader = new Grid { Margin = new Thickness(15, 10, 15, 0) };
            sidebarHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sidebarHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sidebarGrid.Children.Add(sidebarHeader);
            Grid.SetRow(sidebarHeader, 0);

            var txtSecTitle = new TextBlock
            {
                Text = "SONIDOS GUARDADOS",
                FontFamily = new FontFamily("Outfit"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 110)),
                VerticalAlignment = VerticalAlignment.Center
            };
            sidebarHeader.Children.Add(txtSecTitle);
            Grid.SetColumn(txtSecTitle, 0);

            // Collapse Sidebar Button
            btnCollapseSidebar = new Border
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            var arrowCollapse = new TextBlock { Text = "◀", Foreground = TextMuted, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            btnCollapseSidebar.Child = arrowCollapse;
            btnCollapseSidebar.MouseEnter += (s, e) => { btnCollapseSidebar.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)); arrowCollapse.Foreground = TextActive; };
            btnCollapseSidebar.MouseLeave += (s, e) => { btnCollapseSidebar.Background = Brushes.Transparent; arrowCollapse.Foreground = TextMuted; };
            btnCollapseSidebar.MouseDown += (s, e) => { ToggleSidebar(); };
            sidebarHeader.Children.Add(btnCollapseSidebar);
            Grid.SetColumn(btnCollapseSidebar, 1);

            // History ListBox (Saved Sounds List)
            lstHistory = new ListBox
            {
                Margin = new Thickness(10, 5, 10, 5),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11.5
            };
            lstHistory.SelectionChanged += LstHistory_SelectionChanged;

            // Setup Context Menu for history items (Rename / Delete)
            var ctxMnu = new ContextMenu();
            var mnuRename = new MenuItem { Header = "Renombrar" };
            mnuRename.Click += (s, e) => RenameSelectedSound();
            ctxMnu.Items.Add(mnuRename);

            var mnuDelete = new MenuItem { Header = "Eliminar" };
            mnuDelete.Click += (s, e) => DeleteSelectedSound();
            ctxMnu.Items.Add(mnuDelete);

            lstHistory.ContextMenu = ctxMnu;
            sidebarGrid.Children.Add(lstHistory);
            Grid.SetRow(lstHistory, 1);

            // Sidebar Footer Grid
            var sidebarFooter = new Grid { Margin = new Thickness(10, 0, 10, 10) };
            sidebarFooter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) }); // "+ Nuevo Sonido"
            sidebarFooter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) }); // Scale/Escala slider
            sidebarGrid.Children.Add(sidebarFooter);
            Grid.SetRow(sidebarFooter, 2);

            // "+ Nuevo Sonido" button
            var btnNewSound = CreateFlatButton("+ Nuevo Sonido", new SolidColorBrush(Color.FromRgb(45, 45, 48)), new SolidColorBrush(Color.FromRgb(55, 55, 60)), TextActive);
            btnNewSound.FontSize = 11;
            btnNewSound.FontWeight = FontWeights.Bold;
            btnNewSound.Click += (s, e) => GenerateNewPreset("Random");
            sidebarFooter.Children.Add(btnNewSound);
            Grid.SetRow(btnNewSound, 0);

            // Zoom Controller Stack
            var zoomStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var lblZoom = new TextBlock { Text = "Escala", Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Width = 40, VerticalAlignment = VerticalAlignment.Center };
            zoomStack.Children.Add(lblZoom);

            var zoomSlider = new Slider { Minimum = 0.6, Maximum = 1.3, Value = 1.0, Width = 110, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
            zoomStack.Children.Add(zoomSlider);

            var zoomValText = new TextBlock { Text = "100%", Foreground = TextMuted, FontSize = 10, Width = 35, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            zoomStack.Children.Add(zoomValText);
            sidebarFooter.Children.Add(zoomStack);
            Grid.SetRow(zoomStack, 1);

            // Scale Transform bound to Zoom Slider
            layoutScale = new ScaleTransform(1, 1);
            zoomSlider.ValueChanged += (s, e) =>
            {
                layoutScale.ScaleX = zoomSlider.Value;
                layoutScale.ScaleY = zoomSlider.Value;
                zoomValText.Text = Math.Round(zoomSlider.Value * 100) + "%";
            };

            // Grid Splitter Column 1
            var splitter = new GridSplitter
            {
                Width = 4,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            splitter.MouseEnter += (s, e) => splitter.Background = AccentBlue;
            splitter.MouseLeave += (s, e) => splitter.Background = Brushes.Transparent;
            contentGrid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);

            // Workspace Grid Column 2
            workspaceGrid = new Grid();
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) }); // Active Sound Header
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Editor Columns panel
            contentGrid.Children.Add(workspaceGrid);
            Grid.SetColumn(workspaceGrid, 2);

            // Active Sound Header (Row 0)
            var workspaceHeader = new Grid { Background = BgMain };
            workspaceHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Toggle Button
            workspaceHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Sound Info (Name, preset type)
            workspaceHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Quick actions (Mutar, Copiar)
            
            var bottomBorder = new Border { BorderBrush = BorderColor, BorderThickness = new Thickness(0, 0, 0, 1) };
            Grid.SetColumnSpan(bottomBorder, 3);
            workspaceHeader.Children.Add(bottomBorder);
            workspaceGrid.Children.Add(workspaceHeader);
            Grid.SetRow(workspaceHeader, 0);

            // Toggle Expand Sidebar Button
            btnExpandSidebar = new Border
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Margin = new Thickness(15, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            var arrowExpand = new TextBlock { Text = "▶", Foreground = TextMuted, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            btnExpandSidebar.Child = arrowExpand;
            btnExpandSidebar.MouseEnter += (s, e) => { btnExpandSidebar.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)); arrowExpand.Foreground = TextActive; };
            btnExpandSidebar.MouseLeave += (s, e) => { btnExpandSidebar.Background = Brushes.Transparent; arrowExpand.Foreground = TextMuted; };
            btnExpandSidebar.MouseDown += (s, e) => { ToggleSidebar(); };
            workspaceHeader.Children.Add(btnExpandSidebar);
            Grid.SetColumn(btnExpandSidebar, 0);

            // Sound Info Block
            var soundInfoStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(25, 0, 0, 0) };
            var waveIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Volume, AccentBlue, 24);
            waveIcon.VerticalAlignment = VerticalAlignment.Center;
            soundInfoStack.Children.Add(waveIcon);

            var titlesStack = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            txtActiveSoundName = new TextBlock { Text = "Sin Sonido", Foreground = TextActive, FontSize = 16, FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
            txtActiveSoundDesc = new TextBlock { Text = "Modo: Chiptune 8-bit", Foreground = TextMuted, FontSize = 10.5, Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            titlesStack.Children.Add(txtActiveSoundName);
            titlesStack.Children.Add(txtActiveSoundDesc);
            soundInfoStack.Children.Add(titlesStack);
            workspaceHeader.Children.Add(soundInfoStack);
            Grid.SetColumn(soundInfoStack, 1);

            // Quick Actions Block (Mutar, Copiar)
            var quickActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 25, 0) };
            workspaceHeader.Children.Add(quickActions);
            Grid.SetColumn(quickActions, 2);

            var btnMutarQuick = CreateFlatButton("🧪 Mutar Copia", new SolidColorBrush(Color.FromRgb(40, 40, 40)), new SolidColorBrush(Color.FromRgb(55, 55, 60)), TextActive);
            btnMutarQuick.Padding = new Thickness(12, 5, 12, 5);
            btnMutarQuick.FontSize = 11.5;
            btnMutarQuick.FontWeight = FontWeights.Bold;
            btnMutarQuick.Margin = new Thickness(0, 0, 10, 0);
            btnMutarQuick.Click += (s, e) => MutateCurrentSound();
            quickActions.Children.Add(btnMutarQuick);

            var btnCopiarQuick = CreateFlatButton("📋 Copiar", new SolidColorBrush(Color.FromRgb(35, 134, 54)), new SolidColorBrush(Color.FromRgb(46, 160, 67)), TextActive);
            btnCopiarQuick.Padding = new Thickness(12, 5, 12, 5);
            btnCopiarQuick.FontSize = 11.5;
            btnCopiarQuick.FontWeight = FontWeights.Bold;
            btnCopiarQuick.Click += (s, e) => CopyParamsToClipboard();
            quickActions.Children.Add(btnCopiarQuick);

            // Editor columns grid (bound to layoutScale transformation!)
            var workspaceContentGrid = new Grid { LayoutTransform = layoutScale, Margin = new Thickness(25, 15, 25, 15) };
            workspaceContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Col 0: Presets Panel
            workspaceContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });  // Spacing
            workspaceContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 1: Synth Parameters (Sliders)
            workspaceContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });  // Spacing
            workspaceContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) }); // Col 2: Playback & Actions

            workspaceGrid.Children.Add(workspaceContentGrid);
            Grid.SetRow(workspaceContentGrid, 1);

            // Column 0: Presets Panel
            var colPresetsBorder = new Border { Background = BgSidebar, BorderBrush = BorderColor, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12) };
            workspaceContentGrid.Children.Add(colPresetsBorder);
            Grid.SetColumn(colPresetsBorder, 0);

            var presetsStack = new StackPanel();
            colPresetsBorder.Child = presetsStack;
            SetupPresetsPanel(presetsStack);

            // Column 1: Synth Parameters (Sliders)
            var colParamsBorder = new Border { Background = BgSidebar, BorderBrush = BorderColor, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12) };
            workspaceContentGrid.Children.Add(colParamsBorder);
            Grid.SetColumn(colParamsBorder, 2);

            var paramsGrid = new Grid();
            paramsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            paramsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Waveform selectors
            paramsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Sliders list
            colParamsBorder.Child = paramsGrid;

            SetupParametersPanel(paramsGrid);

            // Column 2: Playback & Actions
            var colActionsBorder = new Border { Background = BgSidebar, BorderBrush = BorderColor, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12) };
            workspaceContentGrid.Children.Add(colActionsBorder);
            Grid.SetColumn(colActionsBorder, 4);

            var actionsStack = new StackPanel();
            colActionsBorder.Child = actionsStack;
            SetupVisualizerAndActions(actionsStack);

            // Initial Database Load
            RefreshHistoryList();

            // Select or generate first sound
            if (db.Sounds.Count > 0)
            {
                SelectSound(db.Sounds[0]);
            }
            else
            {
                GenerateNewPreset("Coin");
            }
        }

        private void SetupGlobalStyles()
        {
            // ListBox custom dark styling
            try
            {
                string listStyle = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListBox'>
                    <Setter Property='Background' Value='#1a1a1a' />
                    <Setter Property='BorderThickness' Value='0' />
                    <Setter Property='Foreground' Value='White' />
                    <Setter Property='ItemContainerStyle'>
                        <Setter.Value>
                            <Style TargetType='ListBoxItem'>
                                <Setter Property='Padding' Value='8,6,8,6' />
                                <Setter Property='Margin' Value='0,2,0,2' />
                                <Setter Property='Background' Value='Transparent' />
                                <Setter Property='BorderThickness' Value='0' />
                                <Setter Property='Template'>
                                    <Setter.Value>
                                        <ControlTemplate TargetType='ListBoxItem'>
                                            <Border Name='Border' CornerRadius='4' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}'>
                                                <ContentPresenter />
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property='IsMouseOver' Value='true'>
                                                    <Setter TargetName='Border' Property='Background' Value='#2d2d2d' />
                                                </Trigger>
                                                <Trigger Property='IsSelected' Value='true'>
                                                    <Setter TargetName='Border' Property='Background' Value='#007acc' />
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(listStyle);
                Application.Current.Resources[typeof(ListBox)] = style;
            }
            catch { }

            // ScrollBar custom dark template
            try
            {
                string scrollBarXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ScrollBar'>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ScrollBar'>
                                <Grid Background='#121212' SnapsToDevicePixels='true'>
                                    <Track Name='PART_Track' IsDirectionReversed='true'>
                                        <Track.Thumb>
                                            <Thumb>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='Thumb'>
                                                        <Border CornerRadius='3.5' Background='#3e3e3e' />
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                    <Style.Triggers>
                        <Trigger Property='Orientation' Value='Vertical'>
                            <Setter Property='Width' Value='7'/>
                            <Setter Property='Height' Value='Auto'/>
                        </Trigger>
                        <Trigger Property='Orientation' Value='Horizontal'>
                            <Setter Property='Width' Value='Auto'/>
                            <Setter Property='Height' Value='7'/>
                        </Trigger>
                    </Style.Triggers>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(scrollBarXaml);
                Application.Current.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = style;
            }
            catch { }

            // Slider custom dark template
            try
            {
                string sliderXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>
                    <Setter Property='Background' Value='Transparent' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Slider'>
                                <Grid SnapsToDevicePixels='true'>
                                    <Border Name='TrackBackground' Background='#2d2d2d' BorderBrush='#444444' BorderThickness='1' CornerRadius='3' Height='6' VerticalAlignment='Center' Margin='5,0,5,0' />
                                    <Track Name='PART_Track'>
                                        <Track.DecreaseRepeatButton>
                                            <RepeatButton Command='Slider.DecreaseLarge'>
                                                <RepeatButton.Template>
                                                    <ControlTemplate TargetType='RepeatButton'>
                                                        <Border Background='Transparent' />
                                                    </ControlTemplate>
                                                </RepeatButton.Template>
                                            </RepeatButton>
                                        </Track.DecreaseRepeatButton>
                                        <Track.IncreaseRepeatButton>
                                            <RepeatButton Command='Slider.IncreaseLarge'>
                                                <RepeatButton.Template>
                                                    <ControlTemplate TargetType='RepeatButton'>
                                                        <Border Background='Transparent' />
                                                    </ControlTemplate>
                                                </RepeatButton.Template>
                                            </RepeatButton>
                                        </Track.IncreaseRepeatButton>
                                        <Track.Thumb>
                                            <Thumb Width='14' Height='14' Focusable='false'>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='Thumb'>
                                                        <Border Name='ThumbBorder' CornerRadius='7' Background='#888888' BorderBrush='#555555' BorderThickness='1' Cursor='Hand' />
                                                        <ControlTemplate.Triggers>
                                                            <Trigger Property='IsMouseOver' Value='true'>
                                                                 <Setter TargetName='ThumbBorder' Property='Background' Value='#aaaaaa' />
                                                                 <Setter TargetName='ThumbBorder' Property='BorderBrush' Value='#777777' />
                                                            </Trigger>
                                                        </ControlTemplate.Triggers>
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";
                var style = (Style)System.Windows.Markup.XamlReader.Parse(sliderXaml);
                Application.Current.Resources[typeof(Slider)] = style;
            }
            catch { }
        }

        private Grid CreateTitleBar()
        {
            var titleBarGrid = new Grid { Background = BgSidebar };
            titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title/Logo
            titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Window buttons

            titleBarGrid.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            };

            // Logo & Title
            var logoStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var synthIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Volume, AccentBlue, 14);
            synthIcon.VerticalAlignment = VerticalAlignment.Center;
            logoStack.Children.Add(synthIcon);

            var titleText = new TextBlock
            {
                Text = "EternSynth // Generador de Sonidos Retro",
                Foreground = TextActive,
                FontFamily = new FontFamily("Outfit"),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            logoStack.Children.Add(titleText);
            titleBarGrid.Children.Add(logoStack);
            Grid.SetColumn(logoStack, 0);

            // Window Buttons Stack
            var btnStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            var btnMin = CreateTitleButton(WpfVectorIcons.Minimize, "Minimizar", (s, e) => this.WindowState = WindowState.Minimized);
            var btnMax = CreateTitleButton(WpfVectorIcons.Maximize, "Maximizar", (s, e) => {
                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Maximized;
            });
            var btnClose = CreateTitleButton(WpfVectorIcons.Close, "Cerrar", (s, e) => this.Close(), isCloseButton: true);

            btnStack.Children.Add(btnMin);
            btnStack.Children.Add(btnMax);
            btnStack.Children.Add(btnClose);
            titleBarGrid.Children.Add(btnStack);
            Grid.SetColumn(btnStack, 1);

            return titleBarGrid;
        }

        private Button CreateTitleButton(string geometry, string tooltip, RoutedEventHandler clickHandler, bool isCloseButton = false)
        {
            var btn = new Button
            {
                Width = 46,
                Height = 36,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = WpfVectorIcons.GetIcon(geometry, new SolidColorBrush(Color.FromRgb(150, 150, 150)), 12),
                ToolTip = tooltip,
                Cursor = Cursors.Hand
            };

            var hoverBg = isCloseButton ? new SolidColorBrush(Color.FromRgb(232, 17, 35)) : new SolidColorBrush(Color.FromRgb(60, 60, 60));
            var hoverFg = Brushes.White;

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = hoverBg;
                ((System.Windows.Shapes.Path)btn.Content).Fill = hoverFg;
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                ((System.Windows.Shapes.Path)btn.Content).Fill = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            };

            btn.Click += clickHandler;
            return btn;
        }

        private void ShowTopMenuBar()
        {
            if (menuBarControl != null)
            {
                menuBarControl.Visibility = Visibility.Visible;
                if (mainGrid != null && mainGrid.RowDefinitions.Count > 1)
                {
                    mainGrid.RowDefinitions[1].Height = new GridLength(34);
                }
            }
        }

        private void HideTopMenuBar()
        {
            if (menuBarControl != null && !isMenuContextOpen)
            {
                menuBarControl.Visibility = Visibility.Collapsed;
                if (mainGrid != null && mainGrid.RowDefinitions.Count > 1)
                {
                    mainGrid.RowDefinitions[1].Height = new GridLength(0);
                }
            }
        }

        private void ToggleSidebar()
        {
            if (isSidebarCollapsed)
            {
                // Expand
                contentGrid.ColumnDefinitions[0].MinWidth = 180;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(sidebarPreviousWidth);
                contentGrid.ColumnDefinitions[1].Width = new GridLength(4);
                btnExpandSidebar.Visibility = Visibility.Collapsed;
                btnCollapseSidebar.Visibility = Visibility.Visible;
                isSidebarCollapsed = false;
            }
            else
            {
                // Collapse
                sidebarPreviousWidth = contentGrid.ColumnDefinitions[0].ActualWidth;
                if (sidebarPreviousWidth < 50) sidebarPreviousWidth = 240;
                contentGrid.ColumnDefinitions[0].MinWidth = 0;
                contentGrid.ColumnDefinitions[0].Width = new GridLength(0);
                contentGrid.ColumnDefinitions[1].Width = new GridLength(0);
                btnExpandSidebar.Visibility = Visibility.Visible;
                btnCollapseSidebar.Visibility = Visibility.Collapsed;
                isSidebarCollapsed = true;
            }
        }

        private UIElement CreateMenuBarControl()
        {
            var menuPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                Height = 34,
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnFile = CreateMenuButton("Archivo", (s, e) => ShowFileContextMenu(s as FrameworkElement));
            var btnEdit = CreateMenuButton("Editar", (s, e) => ShowEditContextMenu(s as FrameworkElement));
            var btnView = CreateMenuButton("Ver", (s, e) => ShowViewContextMenu(s as FrameworkElement));
            var btnTools = CreateMenuButton("Herramientas", (s, e) => ShowToolsContextMenu(s as FrameworkElement));
            var btnHelp = CreateMenuButton("Ayuda", (s, e) => ShowHelpContextMenu(s as FrameworkElement));

            menuPanel.Children.Add(btnFile);
            menuPanel.Children.Add(btnEdit);
            menuPanel.Children.Add(btnView);
            menuPanel.Children.Add(btnTools);
            menuPanel.Children.Add(btnHelp);

            return menuPanel;
        }

        private Button CreateMenuButton(string text, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(4, 2, 0, 2),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 48)), "border"));
            triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            template.Triggers.Add(triggerHover);

            btn.Template = template;
            btn.Click += onClick;
            return btn;
        }

        private void ShowFileContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemLoad = new MenuItem { Header = "📁 Cargar parámetros desde disco...", Foreground = Brushes.White, FontSize = 12 };
            itemLoad.Click += (s, e) => LoadParamsFromFile();
            cm.Items.Add(itemLoad);

            var itemSave = new MenuItem { Header = "💿 Guardar parámetros en disco...", Foreground = Brushes.White, FontSize = 12 };
            itemSave.Click += (s, e) => SaveParamsToFile();
            cm.Items.Add(itemSave);

            var itemExport = new MenuItem { Header = "💾 Exportar archivo .WAV...", Foreground = Brushes.White, FontSize = 12 };
            itemExport.Click += (s, e) => ExportWavFile();
            cm.Items.Add(itemExport);

            cm.Items.Add(new Separator());

            var itemExit = new MenuItem { Header = "❌ Salir", Foreground = Brushes.White, FontSize = 12 };
            itemExit.Click += (s, e) => Application.Current.Shutdown();
            cm.Items.Add(itemExit);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowEditContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemCopy = new MenuItem { Header = "📋 Copiar parámetros al portapapeles", Foreground = Brushes.White, FontSize = 12 };
            itemCopy.Click += (s, e) => CopyParamsToClipboard();
            cm.Items.Add(itemCopy);

            var itemPaste = new MenuItem { Header = "📥 Pegar parámetros del portapapeles", Foreground = Brushes.White, FontSize = 12 };
            itemPaste.Click += (s, e) => PasteParamsFromClipboard();
            cm.Items.Add(itemPaste);

            cm.Items.Add(new Separator());

            var itemMut = new MenuItem { Header = "🧪 Mutar copia del sonido activo", Foreground = Brushes.White, FontSize = 12 };
            itemMut.Click += (s, e) => MutateCurrentSound();
            cm.Items.Add(itemMut);

            var itemRnd = new MenuItem { Header = "🎲 Generar sonido aleatorio", Foreground = Brushes.White, FontSize = 12 };
            itemRnd.Click += (s, e) => GenerateNewPreset("Random");
            cm.Items.Add(itemRnd);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowViewContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemToggle = new MenuItem { Header = "👁️ Alternar Barra Lateral de Sonidos", Foreground = Brushes.White, FontSize = 12 };
            itemToggle.Click += (s, e) => ToggleSidebar();
            cm.Items.Add(itemToggle);

            cm.Items.Add(new Separator());

            Action<string, int> addWaveViewBtn = (name, index) =>
            {
                var item = new MenuItem { Header = name, Foreground = Brushes.White, FontSize = 12, IsChecked = synth.wave_type == index };
                item.Click += (s, e) => SetWaveType(index);
                cm.Items.Add(item);
            };

            addWaveViewBtn("🔳 Forma de Onda: Cuadrada", 0);
            addWaveViewBtn("📈 Forma de Onda: Sierra", 1);
            addWaveViewBtn("〰️ Forma de Onda: Senoidal", 2);
            addWaveViewBtn("💨 Forma de Onda: Ruido", 3);
            addWaveViewBtn("🔺 Forma de Onda: Triangular", 4);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowToolsContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            Action<string, string> addPresetMenu = (name, type) =>
            {
                var item = new MenuItem { Header = name, Foreground = Brushes.White, FontSize = 12 };
                item.Click += (s, e) => GenerateNewPreset(type);
                cm.Items.Add(item);
            };

            addPresetMenu("🪙 Preajuste: Moneda", "Coin");
            addPresetMenu("🔫 Preajuste: Disparo / Láser", "Laser");
            addPresetMenu("💥 Preajuste: Explosión", "Explosion");
            addPresetMenu("⚡ Preajuste: Powerup", "Powerup");
            addPresetMenu("🤕 Preajuste: Daño / Golpe", "HitHurt");
            addPresetMenu("🦘 Preajuste: Salto", "Jump");
            addPresetMenu("👾 Preajuste: Blip", "Blip");

            cm.Items.Add(new Separator());

            var itemReset = new MenuItem { Header = "🔄 Restablecer parámetros del sintetizador", Foreground = Brushes.White, FontSize = 12 };
            itemReset.Click += (s, e) => ResetParamsUI();
            cm.Items.Add(itemReset);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ShowHelpContextMenu(FrameworkElement target)
        {
            if (target == null) return;

            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                HasDropShadow = true
            };
            cm.Opened += (s, e) => { isMenuContextOpen = true; ShowTopMenuBar(); };
            cm.Closed += (s, e) => { isMenuContextOpen = false; HideTopMenuBar(); };

            var itemAbout = new MenuItem { Header = "ℹ️ Acerca de EternSynth", Foreground = Brushes.White, FontSize = 12 };
            itemAbout.Click += (s, e) => ShowAboutDialog();
            cm.Items.Add(itemAbout);

            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }

        private void ResetParamsUI()
        {
            synth.ResetParams();
            if (activeSound != null)
            {
                activeSound.ParamsString = synth.GetSettingsString();
                Storage.Save(db);
            }
            SelectSound(activeSound ?? db.Sounds.FirstOrDefault());
        }

        private void ShowAboutDialog()
        {
            ShowCustomMessageBox(
                "🚀 EternSynth v1.0 // Generador de Sonidos Retro\n\n" +
                "Inspirado en el clásico SFXR / BFXR.\n" +
                "Desarrollado para la síntesis nativa y procedimental de efectos de sonido de 8 bits en Windows.\n\n" +
                "© 2026 Etern Studio.",
                "Acerca de EternSynth"
            );
        }

        private void SetupPresetsPanel(StackPanel container)
        {
            var txtPresetsTitle = new TextBlock { Text = "PREAJUSTES RÁPIDOS", Foreground = AccentBlue, FontFamily = new FontFamily("Outfit"), FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(txtPresetsTitle);

            // Helper to add preset button
            Action<string, Brush, string> addPresetBtn = (name, accent, presetType) =>
            {
                var btn = new Button
                {
                    Content = name,
                    Background = BgCard,
                    Foreground = TextActive,
                    BorderThickness = new Thickness(1),
                    BorderBrush = BorderColor,
                    Padding = new Thickness(0, 7, 0, 7),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                
                btn.MouseEnter += (s, e) => { btn.Background = accent; btn.BorderBrush = accent; btn.Foreground = Brushes.Black; };
                btn.MouseLeave += (s, e) => { btn.Background = BgCard; btn.BorderBrush = BorderColor; btn.Foreground = TextActive; };
                btn.Click += (s, e) => GenerateNewPreset(presetType);
                
                container.Children.Add(btn);
            };

            addPresetBtn("🪙 Moneda", AccentOrange, "Coin");
            addPresetBtn("🔫 Disparo / Láser", AccentBlue, "Laser");
            addPresetBtn("💥 Explosión", AccentRed, "Explosion");
            addPresetBtn("⚡ Powerup", AccentPurple, "Powerup");
            addPresetBtn("🤕 Daño / Golpe", AccentOrange, "HitHurt");
            addPresetBtn("🦘 Salto", AccentBlue, "Jump");
            addPresetBtn("👾 Blip / Selecc.", AccentGreen, "Blip");
            
            // Separator
            container.Children.Add(new Border { BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = BorderColor, Margin = new Thickness(0, 5, 0, 10) });

            var btnRnd = new Button { Content = "🎲 Aleatorio", Background = BgCard, Foreground = TextActive, BorderThickness = new Thickness(1), BorderBrush = BorderColor, Padding = new Thickness(0, 7, 0, 7), Cursor = Cursors.Hand, FontFamily = new FontFamily("Segoe UI"), FontSize = 11, FontWeight = FontWeights.Bold };
            btnRnd.MouseEnter += (s, e) => { btnRnd.Background = AccentGreen; btnRnd.BorderBrush = AccentGreen; btnRnd.Foreground = Brushes.Black; };
            btnRnd.MouseLeave += (s, e) => { btnRnd.Background = BgCard; btnRnd.BorderBrush = BorderColor; btnRnd.Foreground = TextActive; };
            btnRnd.Click += (s, e) => GenerateNewPreset("Random");
            container.Children.Add(btnRnd);
        }

        private void SetupParametersPanel(Grid parentGrid)
        {
            // 1. Header Title
            var txtTitle = new TextBlock
            {
                Text = "PARÁMETROS DEL SINTETIZADOR",
                Foreground = AccentBlue,
                FontFamily = new FontFamily("Outfit"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            parentGrid.Children.Add(txtTitle);
            Grid.SetRow(txtTitle, 0);

            // 2. Wave shape buttons
            var shapeGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            shapeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            parentGrid.Children.Add(shapeGrid);
            Grid.SetRow(shapeGrid, 1);

            Action<string, int, int> addShapeBtn = (name, index, col) =>
            {
                var btn = new Button
                {
                    Content = name,
                    Background = BgCard,
                    Foreground = TextActive,
                    BorderThickness = new Thickness(1),
                    BorderBrush = BorderColor,
                    Padding = new Thickness(0, 6, 0, 6),
                    Cursor = Cursors.Hand,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold
                };
                btn.Click += (s, e) => SetWaveType(index);
                shapeButtons[index] = btn;
                Grid.SetColumn(btn, col);
                shapeGrid.Children.Add(btn);
            };

            addShapeBtn("🔳 Cuadrada", 0, 0);
            addShapeBtn("📈 Sierra", 1, 2);
            addShapeBtn("〰️ Senoidal", 2, 4);
            addShapeBtn("💨 Ruido", 3, 6);
            addShapeBtn("🔺 Triang.", 4, 8);

            // 3. Scroll List containing Sliders
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 5, 0)
            };
            parentGrid.Children.Add(scroll);
            Grid.SetRow(scroll, 2);

            slidersPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            scroll.Content = slidersPanel;

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
            AddParameterSlider("Deslizamiento Frec.", "p_freq_ramp", () => synth.p_freq_ramp, v => synth.p_freq_ramp = v);
            AddParameterSlider("Aceleración Frec.", "p_freq_dramp", () => synth.p_freq_dramp, v => synth.p_freq_dramp = v);

            // Group: VIBRATO
            AddGroupHeader("VIBRATO DE TONO");
            AddParameterSlider("Profundidad de Vibrato", "p_vib_strength", () => synth.p_vib_strength, v => synth.p_vib_strength = v);
            AddParameterSlider("Velocidad de Vibrato", "p_vib_speed", () => synth.p_vib_speed, v => synth.p_vib_speed = v);

            // Group: ARPEGIO (SALTOS)
            AddGroupHeader("CAMBIO DE TONO (ARPEGIO)");
            AddParameterSlider("Velocidad de Arpegio", "p_arp_speed", () => synth.p_arp_speed, v => synth.p_arp_speed = v);
            AddParameterSlider("Cantidad de Arpegio", "p_arp_mod", () => synth.p_arp_mod, v => synth.p_arp_mod = v);

            // Group: ONDA CUADRADA
            AddGroupHeader("CICLO DE TRABAJO (ONDA CUADRADA)");
            AddParameterSlider("Ancho de Pulso (Duty)", "p_duty", () => synth.p_duty, v => synth.p_duty = v);
            AddParameterSlider("Barrido de Pulso", "p_duty_ramp", () => synth.p_duty_ramp, v => synth.p_duty_ramp = v);

            // Group: FILTROS
            AddGroupHeader("FILTROS DE AUDIO");
            AddParameterSlider("Corte Filtro Paso-Bajo", "p_lpf_freq", () => synth.p_lpf_freq, v => synth.p_lpf_freq = v);
            AddParameterSlider("Barrido Paso-Bajo", "p_lpf_ramp", () => synth.p_lpf_ramp, v => synth.p_lpf_ramp = v);
            AddParameterSlider("Resonancia Paso-Bajo", "p_lpf_resonance", () => synth.p_lpf_resonance, v => synth.p_lpf_resonance = v);
            AddParameterSlider("Corte Filtro Paso-Alto", "p_hpf_freq", () => synth.p_hpf_freq, v => synth.p_hpf_freq = v);
            AddParameterSlider("Barrido Paso-Alto", "p_hpf_ramp", () => synth.p_hpf_ramp, v => synth.p_hpf_ramp = v);

            // Group: PHASER
            AddGroupHeader("EFECTO PHASER / FLANGER");
            AddParameterSlider("Desfase de Phaser", "p_pha_offset", () => synth.p_pha_offset, v => synth.p_pha_offset = v);
            AddParameterSlider("Barrido de Phaser", "p_pha_ramp", () => synth.p_pha_ramp, v => synth.p_pha_ramp = v);

            // Group: REPETICIÓN
            AddGroupHeader("REPETICIÓN DE SONIDO");
            AddParameterSlider("Velocidad de Repetición", "p_repeat_speed", () => synth.p_repeat_speed, v => synth.p_repeat_speed = v);
        }

        private void AddGroupHeader(string title)
        {
            var header = new TextBlock
            {
                Text = title,
                Foreground = AccentBlue,
                FontFamily = new FontFamily("Outfit"),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            };
            slidersPanel.Children.Add(header);
        }

        private void AddParameterSlider(string label, string paramName, Func<float> getter, Action<float> setter)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            var txtLabel = new TextBlock
            {
                Text = label,
                Foreground = TextMuted,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtLabel, 0);
            grid.Children.Add(txtLabel);

            var slider = new Slider
            {
                Minimum = 0.0,
                Maximum = 1.0,
                Value = getter(),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            if (paramName == "p_freq_ramp" || paramName == "p_freq_dramp" || paramName == "p_arp_mod" || 
                paramName == "p_duty_ramp" || paramName == "p_pha_offset" || paramName == "p_pha_ramp" ||
                paramName == "p_lpf_ramp" || paramName == "p_hpf_ramp")
            {
                slider.Minimum = -1.0;
                slider.Maximum = 1.0;
            }

            Grid.SetColumn(slider, 1);
            grid.Children.Add(slider);

            var txtValue = new TextBlock
            {
                Text = slider.Value.ToString("F2"),
                Foreground = TextActive,
                FontFamily = new FontFamily("Consolas"),
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

        private void SetupVisualizerAndActions(StackPanel container)
        {
            // Section: VISUALIZADOR
            var txtVisTitle = new TextBlock { Text = "VISUALIZACIÓN DE ONDA", Foreground = AccentBlue, FontFamily = new FontFamily("Outfit"), FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(txtVisTitle);

            // Visualizer Canvas
            var canvasBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(15, 15, 15)), BorderBrush = BorderColor, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), ClipToBounds = true, Height = 100, Margin = new Thickness(0, 0, 0, 15) };
            waveCanvas = new Canvas { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            canvasBorder.Child = waveCanvas;
            container.Children.Add(canvasBorder);

            // Play On Change CheckBox
            chkPlayOnChange = new CheckBox
            {
                Content = "Reproducir al cambiar parámetros",
                IsChecked = playOnChange,
                Foreground = TextActive,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 10)
            };
            chkPlayOnChange.Checked += (s, e) => playOnChange = true;
            chkPlayOnChange.Unchecked += (s, e) => playOnChange = false;
            container.Children.Add(chkPlayOnChange);

            // PLAY Button
            var btnPlay = new Button
            {
                Content = "▶️ REPRODUCIR SONIDO",
                Background = AccentBlue,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 12, 0, 12),
                Margin = new Thickness(0, 0, 0, 15),
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            btnPlay.MouseEnter += (s, e) => btnPlay.Background = new SolidColorBrush(Color.FromRgb(120, 190, 255));
            btnPlay.MouseLeave += (s, e) => btnPlay.Background = AccentBlue;
            btnPlay.Click += (s, e) => PlayActiveSound();
            container.Children.Add(btnPlay);

            // Volume Slider
            var volGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            volGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            volGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var volIcon = WpfVectorIcons.GetIcon(WpfVectorIcons.Volume, TextMuted, 12);
            volIcon.VerticalAlignment = VerticalAlignment.Center;
            volIcon.Margin = new Thickness(0, 0, 8, 0);
            volGrid.Children.Add(volIcon);
            Grid.SetColumn(volIcon, 0);

            volumeSlider = new Slider { Minimum = 0.0, Maximum = 1.0, Value = synth.sound_vol, VerticalAlignment = VerticalAlignment.Center };
            volumeSlider.ValueChanged += (s, e) => { synth.sound_vol = (float)volumeSlider.Value; OnParameterChanged(); };
            volGrid.Children.Add(volumeSlider);
            Grid.SetColumn(volumeSlider, 1);

            container.Children.Add(volGrid);

            // Separator
            var sep = new Border { BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = BorderColor, Margin = new Thickness(0, 5, 0, 15) };
            container.Children.Add(sep);

            // Export Actions Section
            var txtActTitle = new TextBlock { Text = "ACCIONES RÁPIDAS", Foreground = AccentBlue, FontFamily = new FontFamily("Outfit"), FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(txtActTitle);

            Action<string, RoutedEventHandler> addActionBtn = (text, clickHandler) =>
            {
                var btn = new Button
                {
                    Content = text,
                    Background = BgCard,
                    Foreground = TextActive,
                    BorderThickness = new Thickness(1),
                    BorderBrush = BorderColor,
                    Padding = new Thickness(0, 8, 0, 8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = Cursors.Hand,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                btn.MouseLeave += (s, e) => btn.Background = BgCard;
                btn.Click += clickHandler;
                container.Children.Add(btn);
            };

            addActionBtn("💾 Exportar archivo .WAV", (s, e) => ExportWavFile());
            addActionBtn("📁 Cargar parámetros", (s, e) => LoadParamsFromFile());
            addActionBtn("💿 Guardar parámetros", (s, e) => SaveParamsToFile());

            txtSoundInfo = new TextBlock
            {
                Text = "",
                Foreground = TextMuted,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            container.Children.Add(txtSoundInfo);
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
                    shapeButtons[i].Background = AccentBlue;
                    shapeButtons[i].BorderBrush = AccentBlue;
                    shapeButtons[i].Foreground = Brushes.Black;
                }
                else
                {
                    shapeButtons[i].Background = BgCard;
                    shapeButtons[i].BorderBrush = BorderColor;
                    shapeButtons[i].Foreground = TextActive;
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
                PlaySound(wav);
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
            PlaySound(wav);
        }

        private void PlaySound(byte[] wavBytes)
        {
            if (wavBytes == null) return;
            try
            {
                // Stop and dispose the previous player and stream to release resources
                if (activePlayer != null)
                {
                    activePlayer.Stop();
                    activePlayer.Dispose();
                    activePlayer = null;
                }
                if (activeStream != null)
                {
                    activeStream.Dispose();
                    activeStream = null;
                }

                // Keep stream and player active as fields so they don't get disposed prematurely
                activeStream = new MemoryStream(wavBytes);
                activePlayer = new SoundPlayer(activeStream);
                activePlayer.Play();
            }
            catch (Exception ex)
            {
                // Avoid displaying alert message boxes on slider updates to prevent visual stuttering
                System.Diagnostics.Debug.WriteLine("Error al reproducir audio: " + ex.Message);
            }
        }

        private void GenerateNewPreset(string type)
        {
            string baseName = "Sonido";
            switch (type)
            {
                case "Coin":
                    synth.GenerateCoin();
                    baseName = "Moneda";
                    break;
                case "Laser":
                    synth.GenerateLaser();
                    baseName = "Láser";
                    break;
                case "Explosion":
                    synth.GenerateExplosion();
                    baseName = "Explosión";
                    break;
                case "Powerup":
                    synth.GeneratePowerup();
                    baseName = "Powerup";
                    break;
                case "HitHurt":
                    synth.GenerateHitHurt();
                    baseName = "Daño";
                    break;
                case "Jump":
                    synth.GenerateJump();
                    baseName = "Salto";
                    break;
                case "Blip":
                    synth.GenerateBlipSelect();
                    baseName = "Blip";
                    break;
                case "Random":
                    synth.GenerateRandom();
                    baseName = "Aleatorio";
                    break;
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
            
            // Select the newly generated sound
            lstHistory.SelectedItem = sound;
        }

        private void MutateCurrentSound()
        {
            synth.Mutate();
            
            // Add new sound to database representing mutated copy
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
            lstHistory.SelectedItem = sound;
        }

        private void RefreshHistoryList()
        {
            lstHistory.ItemsSource = null;
            lstHistory.ItemsSource = db.Sounds;
            lstHistory.DisplayMemberPath = "Name";
        }

        private void LstHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SavedSound sound = lstHistory.SelectedItem as SavedSound;
            if (sound != null)
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

                volumeSlider.Value = synth.sound_vol;
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
                PlaySound(wav);
            }
        }

        private void RenameSelectedSound()
        {
            SavedSound sound = lstHistory.SelectedItem as SavedSound;
            if (sound != null)
            {
                var res = ShowInputDialog("Renombrar Sonido", "Ingresa el nuevo nombre:", sound.Name);
                if (res != null && res.Confirmed && !string.IsNullOrEmpty(res.Value))
                {
                    sound.Name = res.Value.Trim();
                    Storage.Save(db);
                    RefreshHistoryList();
                    lstHistory.SelectedItem = sound;
                }
            }
        }

        private void DeleteSelectedSound()
        {
            SavedSound sound = lstHistory.SelectedItem as SavedSound;
            if (sound != null)
            {
                db.Sounds.Remove(sound);
                Storage.Save(db);
                RefreshHistoryList();

                if (db.Sounds.Count > 0)
                {
                    lstHistory.SelectedItem = db.Sounds[db.Sounds.Count - 1];
                }
                else
                {
                    GenerateNewPreset("Coin");
                }
            }
        }

        private void DrawWaveform(List<float> samples)
        {
            waveCanvas.Children.Clear();
            if (samples == null || samples.Count == 0) return;

            double width = waveCanvas.ActualWidth;
            double height = waveCanvas.ActualHeight;
            if (width <= 0) width = 250;
            if (height <= 0) height = 100;

            // Background baseline grid line
            var baseline = new Line
            {
                X1 = 0, Y1 = height / 2.0,
                X2 = width, Y2 = height / 2.0,
                Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 1
            };
            waveCanvas.Children.Add(baseline);

            Polyline polyline = new Polyline
            {
                Stroke = AccentBlue,
                StrokeThickness = 1.5
            };

            int step = Math.Max(1, samples.Count / 200);
            for (int i = 0; i < samples.Count; i += step)
            {
                double x = (double)i / samples.Count * width;
                double y = (height / 2.0) - (samples[i] * (height / 2.0) * 0.9);
                polyline.Points.Add(new Point(x, y));
            }

            waveCanvas.Children.Add(polyline);
        }

        private void ExportWavFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo de audio WAV (*.wav)|*.wav",
                FileName = activeSound != null ? activeSound.Name + ".wav" : "sonido.wav"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] wav = synth.GenerateWav();
                    File.WriteAllBytes(dlg.FileName, wav);
                    ShowCustomMessageBox("Sonido exportado correctamente en:\n" + dlg.FileName, "Éxito");
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error al guardar archivo WAV: " + ex.Message, "Error");
                }
            }
        }

        private void SaveParamsToFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Parámetros de EternSynth (*.json)|*.json|Archivo de texto (*.txt)|*.txt",
                FileName = activeSound != null ? activeSound.Name + ".json" : "parametros.json"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, synth.GetSettingsString(), Encoding.UTF8);
                    ShowCustomMessageBox("Parámetros guardados correctamente.", "Éxito");
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error al guardar parámetros: " + ex.Message, "Error");
                }
            }
        }

        private void LoadParamsFromFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Parámetros de EternSynth (*.json;*.txt)|*.json;*.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string content = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var newSound = new SavedSound
                        {
                            Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName),
                            ParamsString = content.Trim()
                        };
                        db.Sounds.Add(newSound);
                        Storage.Save(db);

                        RefreshHistoryList();
                        lstHistory.SelectedItem = newSound;
                    }
                }
                catch (Exception ex)
                {
                    ShowCustomMessageBox("Error al cargar parámetros: " + ex.Message, "Error");
                }
            }
        }

        private void CopyParamsToClipboard()
        {
            try
            {
                Clipboard.SetText(synth.GetSettingsString());
                ShowCustomMessageBox("Parámetros copiados al portapapeles.", "Portapapeles");
            }
            catch (Exception ex)
            {
                ShowCustomMessageBox("Error al copiar: " + ex.Message, "Error");
            }
        }

        private void PasteParamsFromClipboard()
        {
            try
            {
                string content = Clipboard.GetText();
                if (string.IsNullOrEmpty(content) || !content.Contains(","))
                {
                    ShowCustomMessageBox("El portapapeles no contiene parámetros válidos.", "Error");
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
                lstHistory.SelectedItem = newSound;
            }
            catch (Exception ex)
            {
                ShowCustomMessageBox("Error al pegar: " + ex.Message, "Error");
            }
        }

        // Custom styled modern dark message box dialog
        public void ShowCustomMessageBox(string message, string title)
        {
            this.Dispatcher.Invoke(() =>
            {
                var win = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 380,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ShowInTaskbar = false
                };

                var border = new Border
                {
                    Background = BgSidebar,
                    BorderBrush = BorderColor,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20),
                    Margin = new Thickness(10),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Direction = 270,
                        ShadowDepth = 5,
                        Opacity = 0.5,
                        BlurRadius = 15
                    }
                };
                win.Content = border;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Message
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons
                border.Child = grid;

                var txtTitle = new TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextActive,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                grid.Children.Add(txtTitle);
                Grid.SetRow(txtTitle, 0);

                var txtMsg = new TextBlock
                {
                    Text = message,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 15)
                };
                grid.Children.Add(txtMsg);
                Grid.SetRow(txtMsg, 1);

                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                grid.Children.Add(btnPanel);
                Grid.SetRow(btnPanel, 2);

                var btnOk = CreateFlatButton("Aceptar", new SolidColorBrush(Color.FromRgb(0, 122, 204)), new SolidColorBrush(Color.FromRgb(30, 150, 240)), TextActive);
                btnOk.Width = 80;
                btnOk.Click += (s, e) => { win.DialogResult = true; };
                btnPanel.Children.Add(btnOk);

                win.ShowDialog();
            });
        }

        // Custom styled lightweight modal input dialog
        public class InputDialogResult
        {
            public bool Confirmed { get; set; }
            public string Value { get; set; }
        }

        public InputDialogResult ShowInputDialog(string title, string message, string defaultValue = "")
        {
            var result = new InputDialogResult { Confirmed = false, Value = defaultValue };

            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ShowInTaskbar = false
            };

            var border = new Border
            {
                Background = BgSidebar,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 5,
                    Opacity = 0.5,
                    BlurRadius = 15
                }
            };
            win.Content = border;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            border.Child = grid;

            var titleGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(titleGrid);
            Grid.SetRow(titleGrid, 0);

            var txtTitle = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = TextActive
            };
            titleGrid.Children.Add(txtTitle);
            Grid.SetColumn(txtTitle, 0);

            var formStack = new StackPanel();
            grid.Children.Add(formStack);
            Grid.SetRow(formStack, 1);

            formStack.Children.Add(new TextBlock { Text = message, Foreground = TextMuted, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtInput = new TextBox { Text = defaultValue, Background = BgMain, Foreground = TextActive, BorderBrush = BorderColor, BorderThickness = new Thickness(1), Padding = new Thickness(5), FontSize = 12, CaretBrush = Brushes.White, SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)) };
            formStack.Children.Add(txtInput);
            
            win.Loaded += (s, e) =>
            {
                txtInput.Focus();
                txtInput.SelectAll();
            };

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            grid.Children.Add(btnStack);
            Grid.SetRow(btnStack, 2);

            var btnCancel = CreateFlatButton("Cancelar", new SolidColorBrush(Color.FromRgb(50, 50, 50)), new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextActive);
            btnCancel.Width = 75;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => win.DialogResult = false;
            btnStack.Children.Add(btnCancel);

            var btnSave = CreateFlatButton("Aceptar", new SolidColorBrush(Color.FromRgb(0, 122, 204)), new SolidColorBrush(Color.FromRgb(20, 142, 224)), TextActive);
            btnSave.Width = 75;
            btnSave.FontWeight = FontWeights.Bold;
            btnSave.Click += (s, e) =>
            {
                result.Value = txtInput.Text.Trim();
                result.Confirmed = true;
                win.DialogResult = true;
            };
            btnStack.Children.Add(btnSave);

            if (win.ShowDialog() == true)
            {
                return result;
            }
            return null;
        }

        public static Button CreateFlatButton(string text, Brush normalBg, Brush hoverBg, Brush fg)
        {
            var btn = new Button
            {
                Content = text,
                Background = normalBg,
                Foreground = fg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Style = null
            };

            btn.MouseEnter += (s, e) => btn.Background = hoverBg;
            btn.MouseLeave += (s, e) => btn.Background = normalBg;

            return btn;
        }
    }
}
