using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using client.models;
using client.services;

namespace client.views
{
    public partial class ChatView : UserControl
    {
        public event Action? OnNavigateHome;
        private Conversation _currentConversation;
        
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        private const string ApiBaseUrl = "http://localhost:8000/api";
        
        private readonly string _promptType;
        private readonly string _customInstruction;
        private List<Conversation> _history;

        public ObservableCollection<DisplayMessage> UIMessages { get; set; } = new ObservableCollection<DisplayMessage>();

        public ChatView(string actionName = "custom")
        {
            InitializeComponent();
            
            _promptType = MapPromptType(actionName);
            _customInstruction = actionName.StartsWith("Custom: ") ? actionName.Replace("Custom: ", "") : "";
            _history = HistoryService.Load();

            _currentConversation = _history.LastOrDefault(c => c.PromptType == _promptType)
                ?? new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    PromptType = _promptType,
                    CustomInstruction = _customInstruction,
                    CreatedAt = DateTime.Now,
                    Messages = new List<ChatMessage>()
                };

            UIMessages.Add(new DisplayMessage 
            { 
                Role = "system", 
                Content = $"— {_currentConversation.CreatedAt:dd/MM/yyyy HH:mm} —" 
            });


            foreach (var msg in _currentConversation.Messages)
            {
                UIMessages.Add(new DisplayMessage
                {
                    Role = msg.Role,
                    Content = msg.Content
                });
            }
            
            MessagesItemsControl.ItemsSource = UIMessages;
        }

        public void SetContext(string title)
        {
            ChatTitle.Text = title;
        }

        // --- MAPPER UI -> BACKEND ---
        private string MapPromptType(string actionName)
        {
            if (actionName.StartsWith("Custom: ")) return "custom";
            return actionName switch
            {
                "Traduire" => "translate",
                "Résumer" => "summary",
                "Réécrire" => "rewrite",
                "Répondre" => "response",
                "Corriger" => "correct",
                _ => "custom"
            };
        }

        private void Back_Click(object sender, RoutedEventArgs e) => OnNavigateHome?.Invoke();

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Send_Click(sender, e);
        }

        public void AutoSend(string prompt) 
        {
            MessageInput.Text = prompt;
            Send_Click(this, new RoutedEventArgs());
        }

        private void ChatPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChatScroll.ScrollToEnd();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            MessageInput.Clear();
            MessageInput.IsEnabled = false;

            LoadingIndicator.Visibility = Visibility.Visible;

            UIMessages.Add(new DisplayMessage { Role = "user", Content = text });
            _currentConversation.Messages.Add(new ChatMessage { Role = "user", Content = text, Timestamp = DateTime.Now });

            string content = _promptType == "custom" ? _customInstruction + "\n\n---\n\n" + text : text;

            string json = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "prompt_type", _promptType },
                { "content", content }
            });

            try
            {
                bool useStream = StreamToggle.IsChecked == true; 
                string? reply = useStream ? await SendStreamRequest(json) : await SendGenerateRequest(json);

                if (reply != null)
                {
                    _currentConversation.Messages.Add(new ChatMessage { Role = "assistant", Content = reply, Timestamp = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                UIMessages.Add(new DisplayMessage { Role = "system", Content = "Erreur de connexion : " + ex.Message });
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;

                MessageInput.IsEnabled = true;
                MessageInput.Focus();
                
                if (_promptType != "custom")
                {
                    var history = HistoryService.Load();
                    history.Add(_currentConversation);
                    HistoryService.Save(history);
                }

                ChatScroll.ScrollToEnd();
            }
        }

        private async Task<string?> SendStreamRequest(string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl + "/stream")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var err = new DisplayMessage { Role = "assistant", Content = "Erreur serveur : " + (int)response.StatusCode };
                    UIMessages.Add(err);
                    return err.Content;
                }

                var streamingMessage = new DisplayMessage { Role = "assistant", Content = "" };
                UIMessages.Add(streamingMessage);

                var sb = new StringBuilder();
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var buffer = new char[64];
                    int read;
                    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Append(buffer, 0, read);
                        streamingMessage.Content = StripQuotes(sb.ToString());
                        ChatScroll.ScrollToEnd();
                    }
                }
                return StripQuotes(sb.ToString());
            }
        }

        private async Task<string?> SendGenerateRequest(string json)
        {
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = await _httpClient.PostAsync(ApiBaseUrl + "/generate", httpContent))
            {
                if (!response.IsSuccessStatusCode)
                    return "Erreur serveur : " + (int)response.StatusCode;

                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                string reply = StripQuotes(result != null && result.ContainsKey("data") ? result["data"] : "Erreur.");

                UIMessages.Add(new DisplayMessage { Role = "assistant", Content = reply });
                return reply;
            }
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string textToCopy)
            {
                Clipboard.SetText(textToCopy);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _currentConversation.Messages.Clear();
            UIMessages.Clear();
        }

        private static string StripQuotes(string text) => text.TrimStart('"').TrimEnd('"').Trim();
    }

    public class DisplayMessage : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        public string Role { get; set; } = string.Empty; 
        
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}