using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentalManagementSystem.Presentation
{
    public class LandlordConversation
    {
        public int LandlordId { get; set; }
        public string LandlordName { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public ObservableCollection<TenantChatMessage> Messages { get; set; } = new ObservableCollection<TenantChatMessage>();
    }

    public class TenantChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        // True when the logged-in tenant sent it, false when the landlord did.
        public bool IsSentByMe { get; set; }
    }

    public partial class TenantMessagesPage : UserControl
    {
        public ObservableCollection<LandlordConversation> Conversations { get; set; }
        private LandlordConversation? _selectedConversation;

        public TenantMessagesPage()
        {
            InitializeComponent();

            Conversations = new ObservableCollection<LandlordConversation>();
            LoadDummyData();

            LstLandlords.ItemsSource = Conversations;

            // Select the first conversation by default
            if (Conversations.Count > 0)
            {
                LstLandlords.SelectedIndex = 0;
            }
        }

        private void LoadDummyData()
        {
            Conversations.Add(new LandlordConversation
            {
                LandlordId = 1,
                LandlordName = "Kim Vincent Domingo",
                Subtitle = "Landlord • Unit 101",
                Messages = new ObservableCollection<TenantChatMessage>
                {
                    new TenantChatMessage { Content = "Hello landlord! Just sent the payment for this month.", Timestamp = DateTime.Now.AddHours(-3), IsSentByMe = true },
                    new TenantChatMessage { Content = "Thank you Juan! I received it. I'll issue the receipt shortly.", Timestamp = DateTime.Now.AddHours(-1), IsSentByMe = false }
                }
            });
        }

        private void LstLandlords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstLandlords.SelectedItem is LandlordConversation selected)
            {
                _selectedConversation = selected;
                TxtHeaderLandlord.Text = selected.LandlordName;
                TxtHeaderSubtitle.Text = selected.Subtitle;

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

            _selectedConversation.Messages.Add(new TenantChatMessage
            {
                Content = messageText,
                Timestamp = DateTime.Now,
                IsSentByMe = true
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
