using System.Windows;

namespace krusach
{
    public partial class PrintWindow : Window
    {
        public PrintWindow(string content)
        {
            InitializeComponent();
            TxtContent.Text = content;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
