using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;
using Wpf.Ui.Appearance;

namespace BBoxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        // 添加 PropertyChanged 事件
        public event PropertyChangedEventHandler PropertyChanged;
        // 将私有字段 _rectangles 转换为可绑定的公共属性
        private ObservableCollection<Rectangle> _rectangles = new ObservableCollection<Rectangle>();
        public ObservableCollection<Rectangle> Rectangles
        {
            get => _rectangles;
            set
            {
                _rectangles = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rectangles)));
            }
        }

        // 添加用于通知属性更改的辅助方法
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ImageSource _image;
        private Line _cursorCross;
        private Rectangle _rectangleMask;
        private Point _startPoint;
        private bool _isDrawing;
        //private List<Rectangle> _rectangles = new List<Rectangle>();
        private double _scale = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            DataContext = this; // 设置数据上下文以支持数据绑定
            Rectangles = new ObservableCollection<Rectangle>();  // 初始化集合
            imageCanvas.MouseWheel += ImageCanvas_MouseWheel;
        }

        private void ImageCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_image != null)
            {
                double delta = e.Delta > 0 ? 0.1 : -0.1;
                _scale = Math.Max(0.1, Math.Min(10, _scale + delta));
                imageCanvas.RenderTransform = new ScaleTransform(_scale, _scale);
            }
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            LoadImage();
        }

        private void LoadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _image = new BitmapImage(new Uri(openFileDialog.FileName));
                imageCanvas.Children.Clear(); // 清除之前的内容
                Rectangles.Clear(); // 清除之前的矩形
                imageCanvas.Width = _image.Width;
                imageCanvas.Height = _image.Height;
                imageCanvas.Background = new ImageBrush(_image);
                // chooseImageButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_image != null)
            {
                Point position = e.GetPosition(imageCanvas);
                UpdateCursorCross(position);
                if (_isDrawing)
                {
                    UpdateRectangleMask(position);
                }
            }
        }

        private void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point position = e.GetPosition(imageCanvas);
            if (!_isDrawing)
            {
                _startPoint = position;
                _isDrawing = true;
                _rectangleMask = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
                };
                Canvas.SetLeft(_rectangleMask, _startPoint.X);
                Canvas.SetTop(_rectangleMask, _startPoint.Y);
                imageCanvas.Children.Add(_rectangleMask);
            }
            else
            {
                _isDrawing = false;
                if (_rectangleMask != null)
                {
                    Rectangles.Add(_rectangleMask);
                }
            }
        }

        private void UpdateCursorCross(Point position)
        {
            if (_cursorCross == null)
            {
                _cursorCross = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                };
                imageCanvas.Children.Add(_cursorCross);
            }

            _cursorCross.X1 = position.X - 10;
            _cursorCross.Y1 = position.Y;
            _cursorCross.X2 = position.X + 10;
            _cursorCross.Y2 = position.Y;

            Line verticalLine = new Line
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                X1 = position.X,
                Y1 = position.Y - 10,
                X2 = position.X,
                Y2 = position.Y + 10
            };
            imageCanvas.Children.Add(verticalLine);
            imageCanvas.Children.Remove(verticalLine);
        }

        private void UpdateRectangleMask(Point currentPosition)
        {
            double left = Math.Min(_startPoint.X, currentPosition.X);
            double top = Math.Min(_startPoint.Y, currentPosition.Y);
            double width = Math.Abs(currentPosition.X - _startPoint.X);
            double height = Math.Abs(currentPosition.Y - _startPoint.Y);

            Canvas.SetLeft(_rectangleMask, left);
            Canvas.SetTop(_rectangleMask, top);
            _rectangleMask.Width = width;
            _rectangleMask.Height = height;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Rectangles.Count == 0)
            {
                MessageBox.Show("No rectangles to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                Title = "Save Rectangles"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var rectangles = Rectangles.Select(rect => new
                    {
                        x = (int)Canvas.GetLeft(rect),
                        y = (int)Canvas.GetTop(rect),
                        w = (int)rect.Width,
                        h = (int)rect.Height
                    }).ToList();

                    string jsonString = JsonSerializer.Serialize(rectangles, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(saveFileDialog.FileName, jsonString);
                    MessageBox.Show("Rectangles saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}