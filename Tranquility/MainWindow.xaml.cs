using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinRT;

namespace Tranquility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFilesClick(object sender, RoutedEventArgs e)
        {
            string file = OpenFolder();   
        }

        private static string? OpenFolder()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                return dialog.FileName;

            return null;
        }
    }
}
