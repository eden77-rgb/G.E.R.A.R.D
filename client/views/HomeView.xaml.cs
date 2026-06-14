using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace client.views
{
    public partial class HomeView : UserControl
    {
        public event Action<string>? OnNavigateToChat;

        public HomeView()
        {
            InitializeComponent();
            LoadActions();
            SetupPlaceholder();
        }

        private void LoadActions()
        {
            MainActionsControl.ItemsSource = new List<ActionItem>
            {
                new ActionItem { Title = "Traduire", Icon = "\uE8C1", Color = "#3B82F6" },
                new ActionItem { Title = "Résumer", Icon = "\uE8A5", Color = "#10B981" },
                new ActionItem { Title = "Réécrire", Icon = "\uE895", Color = "#F59E0B" },
                new ActionItem { Title = "Répondre", Icon = "\uE8F2", Color = "#8B5CF6" }
            };

            OtherActionsControl.ItemsSource = new List<ActionItem>
            {
                new ActionItem { Title = "Corriger", Icon = "\uE73E", Color = "#E11D48" },
                new ActionItem { Title = "Expliquer", Icon = "\uEA80", Color = "#D97706" },
                new ActionItem { Title = "Simplifier", Icon = "\uE82D", Color = "#0284C7" },
                new ActionItem { Title = "Générer Code", Icon = "\uE943", Color = "#475569" },
                new ActionItem { Title = "Améliorer", Icon = "\uE712", Color = "#C026D3" },
                new ActionItem { Title = "Analyser", Icon = "\uE9D9", Color = "#16A34A" },
                new ActionItem { Title = "Comparer", Icon = "\uE81E", Color = "#2563EB" },
                new ActionItem { Title = "Rédiger Email", Icon = "\uE715", Color = "#7C3AED" },
                new ActionItem { Title = "Calculer", Icon = "\uE94C", Color = "#4B5563" },
                new ActionItem { Title = "Traduire Tech", Icon = "\uE12B", Color = "#0D9488" },
                new ActionItem { Title = "Brainstorm", Icon = "\uE90F", Color = "#9333EA" },
                new ActionItem { Title = "Discussion", Icon = "\uE8F2", Color = "#DC2626" }
            };
        }

        private void SetupPlaceholder()
        {
            CustomPromptInput.Text = "Décrivez ce que vous voulez faire...";
            CustomPromptInput.Foreground = (System.Windows.Media.Brush)App.Current.FindResource("TextSecondaryBrush");

            CustomPromptInput.GotFocus += (s, e) => {
                if (CustomPromptInput.Text == "Décrivez ce que vous voulez faire...") {
                    CustomPromptInput.Text = "";
                    CustomPromptInput.Foreground = (System.Windows.Media.Brush)App.Current.FindResource("TextPrimaryBrush");
                }
            };

            CustomPromptInput.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(CustomPromptInput.Text)) {
                    CustomPromptInput.Text = "Décrivez ce que vous voulez faire...";
                    CustomPromptInput.Foreground = (System.Windows.Media.Brush)App.Current.FindResource("TextSecondaryBrush");
                }
            };
        }

        private void Action_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel sp && sp.DataContext is ActionItem item)
            {
                OnNavigateToChat?.Invoke(item.Title);
            }
        }

        private void Start_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string text = CustomPromptInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text != "Décrivez ce que vous voulez faire...")
            {
                OnNavigateToChat?.Invoke("Custom: " + text);
                CustomPromptInput.Text = "Décrivez ce que vous voulez faire..."; 
            }
        }

        private void CustomPrompt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Start_Click(sender, e);
        }
    }

    public class ActionItem
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
    }
}