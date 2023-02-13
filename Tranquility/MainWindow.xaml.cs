using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinRT;
using Cupola;
using System.IO;
using System;
using ComputeSharp;

namespace Tranquility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[]? files;
        ReadWriteTexture2D<Bgra32, float4>? finalImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFilesClick(object sender, RoutedEventArgs e)
        {
            string file = OpenFolder();

            files = Directory.GetFiles(file);

            string iFilesT = "";

            foreach (string iFile in files)
            {
                iFilesT += iFile + Environment.NewLine;
            }

            Input_Folder.Text = file;
            Input_Files.Text = iFilesT;
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

        private void RunOp(object sender, RoutedEventArgs e)
        {
            if (files == null)
                throw new Exception("Please select files before running");

            Progress.Value = 10;

            ReadWriteTexture2D<Bgra32, float4>[] images = Cupola.Cupola.Load(files);

            Progress.Value = 30;

            finalImage = Cupola.Cupola.RunSingle(images);

            Progress.Value = 90;

            CommonSaveFileDialog saveFileDialog = new CommonSaveFileDialog();
            saveFileDialog.DefaultExtension = ".jpg";
            saveFileDialog.EnsurePathExists = true;
            CommonFileDialogResult result = saveFileDialog.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
                throw new Exception("please set location valid OKAY");

            finalImage.Save(saveFileDialog.FileName);

            Progress.Value = 100;
        }
    }
}
