using RentalManagementSystem.Model;
using RentalManagementSystem.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RentalManagementSystem.Presentation
{
    public partial class AuthCard : UserControl
    {
        private const double RightX = 400;   // overlay on the right -> Register form visible
        private const double LeftX = -120;   // overlay on the left  -> Login form visible
        private static readonly Duration SlideTime = new Duration(TimeSpan.FromMilliseconds(600));

        /// <summary>Raised when the user logs in successfully.</summary>
        public event EventHandler LoginSucceeded;

        /// <summary>Raised when the user clicks "Register" on the login side (so the page can ask tenant/landlord first).</summary>
        public event EventHandler RegisterRequested;

        /// <summary>Raised when the user clicks the close (X) button.</summary>
        public event EventHandler CloseRequested;

        public AuthCard()
        {
            InitializeComponent();
            this.DataContext = new UserViewModel();
        }

        /// <summary>Shows the card instantly in Login or Register mode (no animation).</summary>
        public void ShowMode(bool login)
        {
            OverlayTransform.BeginAnimation(TranslateTransform.XProperty, null);
            OverlayTransform.X = login ? LeftX : RightX;
            SetVisible(LoginPanel, login);
            SetVisible(HelloPanel, login);
            SetVisible(RegisterPanel, !login);
            SetVisible(WelcomeBackPanel, !login);
        }

        private static void SetVisible(UIElement element, bool show)
        {
            element.BeginAnimation(OpacityProperty, null);
            element.Opacity = show ? 1 : 0;
            element.IsHitTestVisible = show;
        }

        // ---------- Slide between Register and Login ----------
        private void ShowLogin_Click(object sender, RoutedEventArgs e) => Slide(toLogin: true);
        private void ShowRegister_Click(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke(this, EventArgs.Empty);

        private void Slide(bool toLogin)
        {
            var move = new DoubleAnimation(toLogin ? LeftX : RightX, SlideTime)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            OverlayTransform.BeginAnimation(TranslateTransform.XProperty, move);

            Fade(LoginPanel, toLogin);
            Fade(HelloPanel, toLogin);
            Fade(RegisterPanel, !toLogin);
            Fade(WelcomeBackPanel, !toLogin);
        }

        private static void Fade(UIElement element, bool show)
        {
            element.IsHitTestVisible = show;
            var fade = new DoubleAnimation(show ? 1 : 0, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                BeginTime = show ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero
            };
            element.BeginAnimation(OpacityProperty, fade);
        }

        // ---------- Placeholder (hint) text ----------
        private void Field_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is Control c && c.Tag is TextBlock hint)
            {
                string value = sender is PasswordBox pb ? pb.Password : ((TextBox)sender).Text;
                hint.Visibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
            }

            // Live "passwords do not match" warning while typing on the register form
            if (sender == RegPassword || sender == RegPasswordPlain ||
                sender == RegConfirmPassword || sender == RegConfirmPasswordPlain)
            {
                CheckPasswordMatch();
            }
        }

        // ---------- Show / hide password ----------
        // Each password field is a Grid holding a PasswordBox, a plain TextBox (hidden) and the eye ToggleButton.
        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            var grid = (Grid)toggle.Parent;
            var pb = grid.Children.OfType<PasswordBox>().First();
            var tb = grid.Children.OfType<TextBox>().First();

            if (toggle.IsChecked == true)       // show the password
            {
                tb.Text = pb.Password;
                pb.Visibility = Visibility.Collapsed;
                tb.Visibility = Visibility.Visible;
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }
            else                                // hide the password
            {
                pb.Password = tb.Text;
                tb.Visibility = Visibility.Collapsed;
                pb.Visibility = Visibility.Visible;
                pb.Focus();
            }
        }

        /// <summary>Reads the password from whichever control (masked or plain) is currently showing.</summary>
        private static string GetPassword(PasswordBox pb, TextBox tb) =>
            tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;

        private bool CheckPasswordMatch()
        {
            if (RegMessage == null) return true;

            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            bool mismatch = confirm.Length > 0 && password != confirm;
            RegMessage.Text = "Passwords do not match.";
            RegMessage.Visibility = mismatch ? Visibility.Visible : Visibility.Collapsed;
            return !mismatch;
        }

        // ---------- Actions (hook these up to your DAO / Service layer) ----------
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            // 1. Validate empty inputs
            if (string.IsNullOrWhiteSpace(RegUsername.Text) ||
                string.IsNullOrWhiteSpace(RegEmail.Text) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirm))
            {
                MessageBox.Show("Please fill in all fields.", "Registration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validate password match
            if (password != confirm)
            {
                RegMessage.Text = "Passwords do not match.";
                RegMessage.Visibility = Visibility.Visible;
                return;
            }

            RegMessage.Visibility = Visibility.Collapsed;

            // 3. Sync to ViewModel and save
            if (DataContext is UserViewModel vm)
            {
                vm.Username = RegUsername.Text.Trim();
                vm.EmailAddress = RegEmail.Text.Trim();
                vm.Password = password;

                vm.SaveUserToDatabase();

                // 4. Navigate to Login after successful registration
                Slide(toLogin: true);
            }
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserViewModel vm && sender is PasswordBox pbox)
            {
                vm.Password = pbox.Password;
            }

            Field_Changed(sender, e);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string password = GetPassword(LoginPassword, LoginPasswordPlain);
            string username = LoginUsername.Text.Trim();

            if (DataContext is UserViewModel vm)
            {
                vm.Username = username;
                vm.Password = password;

                // Perform login check against database
                if (vm.AuthenticateUser(username, password))
                {
                    LoginSucceeded?.Invoke(this, EventArgs.Empty);

                    // Fetch authenticated user model
                    Model.User loggedInUser = vm.CurrentUser;

                    if (loggedInUser != null)
                    {
                        if (loggedInUser.getRole() == Role.Tenant)
                        {
                            // Pass authenticated user into TenantDashboardPage
                            TenantDashboardPage dashboard = new TenantDashboardPage(loggedInUser);
                            dashboard.Show();

                            // Close login window
                            Window.GetWindow(this)?.Close();
                        }
                        else if (loggedInUser.getRole() == Role.Landlord)
                        {
                            // Pass authenticated user into Landlord DashboardPage Window
                            DashboardPage adminDashboard = new DashboardPage(loggedInUser);
                            adminDashboard.Show();

                            // Close login window
                            Window.GetWindow(this)?.Close();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}