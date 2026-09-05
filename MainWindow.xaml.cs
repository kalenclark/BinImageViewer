using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BinImageViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum EColorFormat
        {
            RGBA,
            BGRA,
            RGB,
            BGR,
        }

        private EColorFormat _ColorFormat = EColorFormat.RGBA;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UIOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UIWidthInput.Text) || string.IsNullOrEmpty(UIHeightInput.Text)) {
                UIStatusText.Text = "Width/Height must be greater than zero!";
                return;
            }

            var width = int.Parse(UIWidthInput.Text);
            var height = int.Parse(UIHeightInput.Text);
            if (width <= 0 || height <= 0) {
                UIStatusText.Text = "Width/Height must be greater than zero!";
                return;
            }

            var dialog = new OpenFileDialog()
            {
                Filter = "*.bin",
            };
            var res = dialog.ShowDialog(Application.Current.MainWindow);
            if (res is null || !(bool)res) {
                return;
            }

            var format = PixelFormats.Bgra32;
            var bytesPerPixel = 4;
            if (_ColorFormat == EColorFormat.RGB || _ColorFormat == EColorFormat.BGR) {
                bytesPerPixel = 3;
                format = PixelFormats.Bgr24;
            }
            var expectedBytes = width * height * bytesPerPixel;
            using var s = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, expectedBytes);
            if (s.Length < expectedBytes) {
                return;
            }

            var image = new WriteableBitmap(width, height, 96, 96, format, null);
            image.Lock();
            try {
                unsafe {
                    using var bstream = new UnmanagedMemoryStream((byte *)image.BackBuffer.ToPointer(), expectedBytes, expectedBytes, FileAccess.Write);
                    s.CopyTo(bstream);

                    bstream.Position = 0;
                    byte *pBase = bstream.PositionPointer;
                    var length = bstream.Length;

                    if (_ColorFormat == EColorFormat.RGBA || _ColorFormat == EColorFormat.RGB) {
                        for (var i = 0; i < length; i += bytesPerPixel) {
                            var tmp = pBase[i];
                            pBase[i] = pBase[i + 2];
                            pBase[i + 2] = tmp;
                        }
                    }
                }

                image.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally {
                image.Unlock();
            }

            UIImage.Source = image;
        }

        private void OnColorFormatChanged(object sender, RoutedEventArgs e)
        {
            if (UIColorFormatOpt1.IsChecked == true) {
                _ColorFormat = EColorFormat.RGBA;
            }
            else if (UIColorFormatOpt2.IsChecked == true) {
                _ColorFormat = EColorFormat.BGRA;
            }
            else if (UIColorFormatOpt3.IsChecked == true) {
                _ColorFormat = EColorFormat.RGB;
            }
            else if (UIColorFormatOpt4.IsChecked == true) {
                _ColorFormat = EColorFormat.BGR;
            }
        }
    }
}