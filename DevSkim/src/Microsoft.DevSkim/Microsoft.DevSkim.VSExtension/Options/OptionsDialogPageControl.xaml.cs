using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Interaction logic for OptionsDialogPageControl.xaml
    /// </summary>
    public partial class OptionsDialogPageControl : System.Windows.Controls.UserControl
    {
        public OptionsDialogPageControl()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (Directory.Exists(CustomRulesPath.Text))
            {
                fbd.SelectedPath = CustomRulesPath.Text;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                CustomRulesPath.Text = fbd.SelectedPath;
            }
        }
    }
}
