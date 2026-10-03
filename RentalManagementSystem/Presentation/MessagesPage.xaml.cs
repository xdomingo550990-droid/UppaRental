using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentalManagementSystem.Presentation
{
    public class TenantConversation
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
    }

    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSentByLandlord { get; set; }
    }

    public partial class MessagesPage : UserControl
    {
        public ObservableCollection<TenantConversation> Conversations { get; set; }
        private TenantConversation? _selectedConversation;

        public MessagesPage()
        {
            InitializeComponent();

            Conversations = new ObservableCollection<TenantConversation>();
            LoadDummyData();

            LstTenants.ItemsSource = Conversations;

            // Select first tenant by default
            if (Conversations.Count > 0)
            {
                LstTenants.SelectedIndex = 0;
            }
        }

        private void LoadDummyData()
        {
            Conversations.Add(new TenantConversation
            {
                TenantId = 1,
                TenantName = "Juan Dela Cruz",
                RoomNumber = "Room 101",
                Messages = new ObservableCollection<ChatMessage>
                {
                    new ChatMessage { Content = "Hello landlord! Just sent the payment for this month.", Timestamp = DateTime.Now.AddHours(-3), IsSentByLandlord = false },
                    new ChatMessage { Content = "Thank you Juan! I received it. I'll issue the receipt shortly.", Timestamp = DateTime.Now.AddHours(-1), IsSentByLandlord = true }
                }
            });

            Conversations.Add(new TenantConversation
            {
                TenantId = 2,
                TenantName = "Maria Santos",
                RoomNumber = "Room 204",
                Messages = new ObservableCollection<ChatMessage>
                {
                    new ChatMessage { Content = "Good afternoon, the faucet in the sink is leaking.", Timestamp = DateTime.Now.AddDays(-1), IsSentByLandlord = false },
                    new ChatMessage { Content = "Got it. I will send a maintenance guy tomorrow morning.", Timestamp = DateTime.Now.AddHours(-5), IsSentByLandlord = true }
                }
            });
        }

        private void LstTenants_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstTenants.SelectedItem is TenantConversation selected)
            {
                _selectedConversation = selected;
                TxtHeaderTenant.Text = selected.TenantName;
                TxtHeaderRoom.Text = selected.RoomNumber;

                MessagesItemsControl.ItemsSource = selected.Messages;
                ScrollToBottom();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendMessage()
        {
            if (_selectedConversation == null) return;

            string messageText = TxtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(messageText)) return;

            _selectedConversation.Messages.Add(new ChatMessage
            {
                Content = messageText,
                Timestamp = DateTime.Now,
                IsSentByLandlord = true
            });

            TxtInput.Clear();
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.UpdateLayout();
            ChatScrollViewer.ScrollToBottom();
        }
    }
}