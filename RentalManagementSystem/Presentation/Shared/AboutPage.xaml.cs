using System.Windows;

namespace RentalManagementSystem.Presentation
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Window
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        // Back to the landing page
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            LandingPage landing = new LandingPage();
            landing.Show();
            this.Close();
        }
    }
}
