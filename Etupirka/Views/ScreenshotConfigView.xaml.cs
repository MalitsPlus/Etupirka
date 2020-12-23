using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MSAPI = Microsoft.WindowsAPICodePack;

namespace Etupirka.Views
{
    /// <summary>
    /// Interaction logic for ScreenshotConfigView.xaml
    /// </summary>
    public partial class ScreenshotConfigView : UserControl
    {
        public bool EnableScreenShot { get; set; }
        public string ScreenShotSavePath { get; set; }
        public string FileName { get; set; }

        public ScreenshotConfigView() {
            InitializeComponent();
            this.DataContext = this;
            EnableScreenShot = Properties.Settings.Default.enableScreenShot;
            ScreenShotSavePath = Properties.Settings.Default.screenShotSavePath;
            FileName = Properties.Settings.Default.fileName;
        }

        private void GetScreenShotPath_Click(object sender, RoutedEventArgs e) {
            var dialog = new MSAPI.Dialogs.CommonOpenFileDialog() {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "フォルダーを選択してください",
                InitialDirectory = ScreenShotSavePath
            };
            if (dialog.ShowDialog() == MSAPI.Dialogs.CommonFileDialogResult.Ok) {
                ScreenShotSavePath = dialog.FileName;
                TextSavePath.Text = ScreenShotSavePath;
            }
        }
    }
}
