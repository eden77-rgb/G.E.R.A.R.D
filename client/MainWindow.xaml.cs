using client.views;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace client
{
    public partial class MainWindow : Window
    {
        // --- Importation des fonctions natives de Windows (Win32) ---
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-hotkey
        private const int HOTKEY_ID = 9000;         // Un ID du raccourci
        private const uint K_ALT = 0x0001;          // ALT
        private const uint K_CTRL = 0x0002;         // CTRL
        private const uint K_A = 0x41;              // A
        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;

        private HomeView _homeView;
        private ChatView _chatView;

        public MainWindow()
        {
            InitializeComponent();

            _homeView = new HomeView();
            _chatView = new ChatView("custom");
            
            /*
            _chatView = new ChatView("custom"); // Instanciation par défaut
            */

            // Navigation depuis Home -> Chat
            _homeView.OnNavigateToChat += (actionName) => 
            {
                if (actionName.StartsWith("Custom: "))
                {
                    string promptText = actionName.Substring(8);

                    _chatView = new ChatView("custom");
                    _chatView.SetContext("Prompt personnalisé");

                    _chatView.OnNavigateHome += () => { MainContent.Content = _homeView; };
                    MainContent.Content = _chatView;

                    _chatView.AutoSend(promptText);
                }

                else 
                {
                    _chatView = new ChatView(actionName);
                    _chatView.SetContext(actionName);

                    _chatView.OnNavigateHome += () => { MainContent.Content = _homeView; };
                    MainContent.Content = _chatView;
                }
            };

            /*
            // Navigation depuis Chat -> Home
            _chatView.BackRequested += (s, e) => 
            {
                MainContent.Content = _homeView;
            };
            */

            MainContent.Content = _homeView;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;

            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, K_CTRL | K_ALT, K_A);   // CTRL + ALT + A
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                OpenClosedApp();
                handled = true; // event gérer
            }
            return IntPtr.Zero;
        }

        private void OpenClosedApp()
        {
            if (this.WindowState == WindowState.Minimized || this.Visibility != Visibility.Visible)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate(); 
            }
            else
            {
                this.WindowState = WindowState.Minimized;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        { // free
            _hwndSource?.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        // --- Ajout pour la nouvelle UI (Barre de titre WindowChrome) ---
        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void Maximize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
        private void Close_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }
    }
}