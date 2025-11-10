using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Windows.Media.Media3D;

namespace App
{
    using static System.Net.WebRequestMethods;
    using Imaging = System.Windows.Media.Imaging;

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //Структура для цвета пикселя
        public struct PixelColor
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the interop host control.
            System.Windows.Forms.Integration.WindowsFormsHost host =
                new System.Windows.Forms.Integration.WindowsFormsHost();

            // Create the MaskedTextBox control.
            MaskedTextBox mtbDate = new MaskedTextBox("00/00/0000");

            // Assign the MaskedTextBox control as the host control's child.
            host.Child = mtbDate;

            // Add the interop host control to the Grid
            // control's collection of child controls.
            this.App.Children.Add(host);
        }

        //Путь до выбранной картинки
        Uri uri;

        //Маски для фильтра собеля
        int[,] GX = {{ -1, -2, -1 },
                         { 0, 0, 0 },
                         { 1, 2, 1 }};
        int[,] GY = {{ -1, 0, 1 },
                         { -2, 0, 2 },
                         { -1, 0, 1}};

        //Метод для копирования данных о цвете пикселя
        public void copyPixels(BitmapSource source, PixelColor[,] pixels, int stride, int offset)
        {
            var height = source.PixelHeight;
            var width = source.PixelWidth;

            var pixelBytes = new byte[height * stride];
            source.CopyPixels(pixelBytes, stride, offset);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    pixels[x, y] = new PixelColor
                    {
                        Blue = pixelBytes[index],
                        Green = pixelBytes[index + 1],
                        Red = pixelBytes[index + 2],
                        Alpha = pixelBytes[index + 3],
                    };
                }
            }
        }

        //Метод Собеля
        private WriteableBitmap Sobel(PixelColor[,] pixels, int width, int height)
        {
            WriteableBitmap w = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int sumX = 0, sumY = 0;

                    // Применяем маску GX
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int brightness = (pixels[x + j, y + i].Red + pixels[x + j, y + i].Green + pixels[x + j, y + i].Blue) / 3;
                            sumX += brightness * GX[i + 1, j + 1];
                            sumY += brightness * GY[i + 1, j + 1];
                        }
                    }

                    int gradient = (int)Math.Sqrt(sumX * sumX + sumY * sumY);
                    gradient = Math.Min(255, Math.Max(0, gradient));

                    byte colorValue = (byte)gradient;

                    Int32Rect rect = new Int32Rect(x, y, 1, 1);
                    byte[] Pixel = { colorValue, colorValue, colorValue, 255 };
                    w.WritePixels(rect, Pixel, 4, 0);
                }
            }

            // Обрабатываем границы изображения
            for (int x = 0; x < width; x++)
            {
                byte[] borderPixel = { 0, 0, 0, 255 };
                Int32Rect topRect = new Int32Rect(x, 0, 1, 1);
                Int32Rect bottomRect = new Int32Rect(x, height - 1, 1, 1);
                w.WritePixels(topRect, borderPixel, 4, 0);
                w.WritePixels(bottomRect, borderPixel, 4, 0);
            }

            for (int y = 0; y < height; y++)
            {
                byte[] borderPixel = { 0, 0, 0, 255 };
                Int32Rect leftRect = new Int32Rect(0, y, 1, 1);
                Int32Rect rightRect = new Int32Rect(width - 1, y, 1, 1);
                w.WritePixels(leftRect, borderPixel, 4, 0);
                w.WritePixels(rightRect, borderPixel, 4, 0);
            }

            return w;
        }

        //Загрузка картинки
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdPicture = new OpenFileDialog();
            ofdPicture.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG;*.TIF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIF|All files (*.*)|*.*";
            ofdPicture.FilterIndex = 1;

            if (ofdPicture.ShowDialog() == true)
            {
                try
                {
                    PictureBox_Load.Source = new BitmapImage(new Uri(ofdPicture.FileName));
                    PictureBox_Safe.Source = new BitmapImage(new Uri(ofdPicture.FileName));
                    contrast.IsEnabled = true;
                    gradient.IsEnabled = true;
                    uri = new Uri(ofdPicture.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        //Сохранение картинки
        private void Button_Safe_Click(object sender, RoutedEventArgs e)
        {
            if (PictureBox_Safe.Source == null)
            {
                errorSafe errorSafe = new errorSafe();
                errorSafe.Show();
                return;
            }

            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Image Files(*.BMP)|*.BMP|Image Files(*.JPG)|*.JPG|Image Files(*.GIF)|*.GIF|Image Files(*.PNG)|*.PNG|All files (*.*)|*.*"
            };

            if (save.ShowDialog() == true)
            {
                try
                {
                    BitmapSource bitmapSource = (BitmapSource)PictureBox_Safe.Source;

                    // Выбираем энкодер в зависимости от расширения файла
                    BitmapEncoder encoder;
                    string extension = System.IO.Path.GetExtension(save.FileName).ToLower();

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        case ".gif":
                            encoder = new GifBitmapEncoder();
                            break;
                        default:
                            encoder = new PngBitmapEncoder();
                            break;
                    }

                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    using (FileStream fileStream = new FileStream(save.FileName, FileMode.Create))
                        encoder.Save(fileStream);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                }
            }
        }

        private void gradient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage(uri);
                int width = bitmap.PixelWidth;
                int height = bitmap.PixelHeight;

                // Создаем массив правильного размера [width, height]
                PixelColor[,] pixels = new PixelColor[width, height];
                copyPixels(bitmap, pixels, bitmap.PixelWidth * 4, 0);

                WriteableBitmap sobel = Sobel(pixels, width, height);
                PictureBox_Safe.Source = sobel;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка применения фильтра Собеля: {ex.Message}");
            }
        }

        private void contrast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int correction = (int)Kontrast.Value;

                BitmapImage bitmap = new BitmapImage(uri);
                WriteableBitmap w = new WriteableBitmap(bitmap);

                int width = bitmap.PixelWidth;
                int height = bitmap.PixelHeight;

                PixelColor[,] pixels = new PixelColor[width, height];
                copyPixels(bitmap, pixels, bitmap.PixelWidth * 4, 0);

                double c = (100.0 + correction) / 100.0;
                c *= c;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        double newValueR = pixels[x, y].Red / 255.0 - 0.5;
                        double newValueG = pixels[x, y].Green / 255.0 - 0.5;
                        double newValueB = pixels[x, y].Blue / 255.0 - 0.5;

                        newValueR = (newValueR * c + 0.5) * 255;
                        newValueG = (newValueG * c + 0.5) * 255;
                        newValueB = (newValueB * c + 0.5) * 255;

                        newValueR = Math.Max(0, Math.Min(255, newValueR));
                        newValueG = Math.Max(0, Math.Min(255, newValueG));
                        newValueB = Math.Max(0, Math.Min(255, newValueB));

                        Int32Rect rect = new Int32Rect(x, y, 1, 1);
                        byte[] Pixel = { (byte)newValueB, (byte)newValueG, (byte)newValueR, pixels[x, y].Alpha };
                        w.WritePixels(rect, Pixel, 4, 0);
                    }
                }
                PictureBox_Safe.Source = w;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка изменения контраста: {ex.Message}");
            }
        }
    }
}