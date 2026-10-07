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
        private const double RightX = 400;   // Overlay on right -> Register form visible
        private const double LeftX = -120;   // Overlay on left  -> Login form visible
        private static readonly Duration SlideTime = new Duration(TimeSpan.FromMilliseconds(600));

        // Red highlight brush for validation failure
        private static readonly Brush ErrorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C84B41"));
        private static readonly Brush DefaultBrush = Brushes.Transparent;

        /// <summary>Raised when the user logs in successfully.</summary>
        public event EventHandler LoginSucceeded;

        /// <summary>Raised when the user clicks "Register" on the login side.</summary>
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
            ResetFieldBorders();
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

        // ---------- Field validation & Placeholder text ----------
        private void Field_Changed(object sender, RoutedEventArgs e)
        {
            // 1. Reset error border back to transparent when typing/editing
            if (sender is Control c)
            {
                c.BorderBrush = DefaultBrush;

                // 2. Manage hint text visibility
                if (c.Tag is TextBlock hint)
                {
                    string value = sender is PasswordBox pb ? pb.Password : ((TextBox)sender).Text;
                    hint.Visibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // 3. Live "passwords do not match" check while typing
            if (sender == RegPassword || sender == RegPasswordPlain ||
                sender == RegConfirmPassword || sender == RegConfirmPasswordPlain)
            {
                CheckPasswordMatch();
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

        // ---------- Show / Hide Password Toggle ----------
        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            var grid = (Grid)toggle.Parent;
            var pb = grid.Children.OfType<PasswordBox>().First();
            var tb = grid.Children.OfType<TextBox>().First();

            if (toggle.IsChecked == true) // Show password
            {
                tb.Text = pb.Password;
                pb.Visibility = Visibility.Collapsed;
                tb.Visibility = Visibility.Visible;
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }
            else // Hide password
            {
                pb.Password = tb.Text;
                tb.Visibility = Visibility.Collapsed;
                pb.Visibility = Visibility.Visible;
                pb.Focus();
            }
        }

        /// <summary>Reads the password from whichever control (masked or plain) is currently visible.</summary>
        private static string GetPassword(PasswordBox pb, TextBox tb) =>
            tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;

        private bool CheckPasswordMatch()
        {
            if (RegMessage == null) return true;

            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            bool mismatch = confirm.Length > 0 && password != confirm;
            if (mismatch)
            {
                RegConfirmPassword.BorderBrush = ErrorBrush;
                RegConfirmPasswordPlain.BorderBrush = ErrorBrush;
                RegMessage.Text = "Passwords do not match.";
                RegMessage.Visibility = Visibility.Visible;
            }
            else if (confirm.Length > 0)
            {
                RegConfirmPassword.BorderBrush = DefaultBrush;
                RegConfirmPasswordPlain.BorderBrush = DefaultBrush;
                RegMessage.Visibility = Visibility.Collapsed;
            }

            return !mismatch;
        }

        // ---------- Actions ----------
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ResetFieldBorders();
            bool isValid = true;

            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            // Validate empty inputs and highlight empty fields in red
            if (string.IsNullOrWhiteSpace(RegFirstName.Text))
            {
                RegFirstName.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(RegLastName.Text))
            {
                RegLastName.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(RegUsername.Text))
            {
                RegUsername.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(RegEmail.Text) || !RegEmail.Text.Contains("@"))
            {
                RegEmail.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                RegPassword.BorderBrush = ErrorBrush;
                RegPasswordPlain.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(confirm))
            {
                RegConfirmPassword.BorderBrush = ErrorBrush;
                RegConfirmPasswordPlain.BorderBrush = ErrorBrush;
                isValid = false;
            }

            if (!isValid)
            {
                RegMessage.Text = "Please fill in all required fields.";
                RegMessage.Visibility = Visibility.Visible;
                return;
            }

            // Validate password match
            if (password != confirm)
            {
                RegConfirmPassword.BorderBrush = ErrorBrush;
                RegConfirmPasswordPlain.BorderBrush = ErrorBrush;
                RegMessage.Text = "Passwords do not match.";
                RegMessage.Visibility = Visibility.Visible;
                return;
            }

            RegMessage.Visibility = Visibility.Collapsed;

            if (DataContext is UserViewModel vm)
            {
                vm.FirstName = RegFirstName.Text.Trim();
                vm.LastName = RegLastName.Text.Trim();
                vm.Username = RegUsername.Text.Trim();
                vm.EmailAddress = RegEmail.Text.Trim();
                vm.Password = password;
                // Ensure vm.Role was assigned via SetUserRole() prior to saving!

                vm.SaveUserToDatabase();
                Slide(toLogin: true);
            }
        }
        /// <summary>Passes the role picked in UserTypeCard into the AuthCard's ViewModel.</summary>
        public void SetUserRole(string roleString)
        {
            if (DataContext is UserViewModel vm && !string.IsNullOrEmpty(roleString))
            {
                // Convert string "Tenant" / "Landlord" into your Role Enum
                if (Enum.TryParse(roleString, out Role parsedRole))
                {
                    vm.Role = parsedRole; // Assumes vm.Role or vm.CurrentUser.Role exists
                }
            }
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            ResetFieldBorders();
            bool isValid = true;

            string username = LoginUsername.Text.Trim();
            string password = GetPassword(LoginPassword, LoginPasswordPlain);

            if (string.IsNullOrWhiteSpace(username))
            {
                LoginUsername.BorderBrush = ErrorBrush;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                LoginPassword.BorderBrush = ErrorBrush;
                LoginPasswordPlain.BorderBrush = ErrorBrush;
                isValid = false;
            }

            if (!isValid) return;

            if (DataContext is UserViewModel vm)
            {
                vm.Username = username;
                vm.Password = password;

                // Perform authentication
                if (vm.AuthenticateUser(username, password))
                {
                    Model.User loggedInUser = vm.CurrentUser;

                    if (loggedInUser != null)
                    {
                        // 1. Get reference to parent window BEFORE opening new windows
                        Window parentWindow = Window.GetWindow(this);

                        // 2. Open ONLY the single dashboard matching the user's role
                        Role userRole = loggedInUser.getRole();

                        if (userRole == Role.Tenant)
                        {
                            TenantDashboardPage tenantDashboard = new TenantDashboardPage(loggedInUser);
                            tenantDashboard.Show();
                        }
                        else if (userRole == Role.Landlord)
                        {
                            DashboardPage adminDashboard = new DashboardPage(loggedInUser);
                            adminDashboard.Show();
                        }

                        // 3. Notify parent/subscribers ONLY IF needed for state logging, 
                        //    but DO NOT open windows in Auth_LoginSucceeded on LandingPage.xaml.cs!
                        // 4. Close the landing/login window
                        parentWindow?.Close();
                    }
                }
                else
                {
                    LoginUsername.BorderBrush = ErrorBrush;
                    LoginPassword.BorderBrush = ErrorBrush;
                    LoginPasswordPlain.BorderBrush = ErrorBrush;
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetFieldBorders()
        {
            RegFirstName.BorderBrush = DefaultBrush;
            RegLastName.BorderBrush = DefaultBrush;
            RegUsername.BorderBrush = DefaultBrush;
            RegEmail.BorderBrush = DefaultBrush;
            RegPassword.BorderBrush = DefaultBrush;
            RegPasswordPlain.BorderBrush = DefaultBrush;
            RegConfirmPassword.BorderBrush = DefaultBrush;
            RegConfirmPasswordPlain.BorderBrush = DefaultBrush;

            LoginUsername.BorderBrush = DefaultBrush;
            LoginPassword.BorderBrush = DefaultBrush;
            LoginPasswordPlain.BorderBrush = DefaultBrush;
        }

        private void Close_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}