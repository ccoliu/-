using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor = null;
        private FrameDescription frameDesc = null;
        private WriteableBitmap bitmap = null;

        private ColorFrameReader colorFrameReader = null;

        private BodyFrameReader bodyFrameReader = null;
        public int boneThickness = 6;
        public int jointThickness = 15;
        private const float InferredZPositionClamp = 0.1f;
        public double Xratio = 1280.0 / 512;
        public double Yratio = 720.0 / 424;

        public int time = 5;
        public bool start = false;
        public int score = 0;

        private CoordinateMapper coodinateMapper = null;
        private Body[] bodies = null;
        private List<Tuple<JointType, JointType>> bones;
        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.frameDesc = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
            this.coodinateMapper = this.kinectSensor.CoordinateMapper;

            this.bones = new List<Tuple<JointType, JointType>>();

            this.bitmap = new WriteableBitmap(this.frameDesc.Width, this.frameDesc.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.kinectSensor.Open();
            InitializeComponent();
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Summary()
        {
            Replay.Visibility = Visibility.Visible;
            Exit.Visibility = Visibility.Visible;
            Summary_word.Visibility = Visibility.Visible;
            Score_str.Text = score.ToString();
            Score_str.Visibility = Visibility.Visible;
        }
        private async void StartGame()
        {
            Time.Visibility = Visibility.Visible;

            ImageSource.Source = new BitmapImage(new Uri("GameStart.jpg", UriKind.RelativeOrAbsolute));

            for (int i = time; i >= 0; i--)
            {
                Time.Text = i.ToString();
                await Task.Delay(1000);
            }

            Summary();
        }
        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            TitleButton.Visibility = Visibility.Hidden;
            Title.Visibility = Visibility.Hidden;

            StartGame();
        }
        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            Replay.Visibility = Visibility.Hidden;
            Summary_word.Visibility = Visibility.Hidden;
            Score_str.Visibility = Visibility.Hidden;
            score = 0;

            StartGame();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
