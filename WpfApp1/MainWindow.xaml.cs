

namespace WpfApp1
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Threading.Tasks;
    using Microsoft.Kinect;
    using System.Windows.Resources;
    using System.Windows.Controls;
    using System.Runtime.CompilerServices;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        /////////////////////////////////////////

        //private KinectSensor kinectSensor = null;
        private FrameDescription frameDesc = null;
        private WriteableBitmap bitmap = null;

        private ColorFrameReader colorFrameReader = null;

        
        //private BodyFrameReader bodyFrameReader = null;
        public int boneThickness = 6;
        public int jointThickness = 15;
        //private const float InferredZPositionClamp = 0.1f;
        public double Xratio = 1280.0 / 512;
        public double Yratio = 720.0 / 424;

        public static int time = 60;
        public bool start = false;
        public int score = 0;
        public int button_num = 5;
        public int available_button = 0;
        public int current_button_num = 0;

       // private string statusText = null;
        public event PropertyChangedEventHandler PropertyChanged;

        private CoordinateMapper coodinateMapper = null;
        //private Body[] bodies = null;
        //private List<Tuple<JointType, JointType>> bones;

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public ImageSource trackImage
        {
            get
            {
                return this.imageSource;
            }
        }
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            //this.mainwindow.Title = this.kinectSensor.IsAvailable ? "OK" : "NO";

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
            ///////////////////////////////////////////////////
            
            this.kinectSensor = KinectSensor.GetDefault();
            this.frameDesc = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            //this.colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            //this.bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
            this.coodinateMapper = this.kinectSensor.CoordinateMapper;

            this.bones = new List<Tuple<JointType, JointType>>();

            this.bitmap = new WriteableBitmap(this.frameDesc.Width, this.frameDesc.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            //this.kinectSensor.Open();
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {

                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            //this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            if (Math.Abs(jointPoints[JointType.HandLeft].Y - jointPoints[JointType.HandRight].Y) < 10 && Math.Abs(jointPoints[JointType.HandLeft].X - jointPoints[JointType.HandRight].X) < 20)
                            {
                                DoMouseClick((uint)(jointPoints[JointType.HandLeft].X + jointPoints[JointType.HandRight].X)/2, (uint)(jointPoints[JointType.HandLeft].Y + jointPoints[JointType.HandRight].Y)/2);
                            }
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
                default:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        public void DoMouseClick(uint X, uint Y)
        {
            SetCursorPos((int)X + 80, (int)Y + 120);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.mainwindow.Title = this.kinectSensor.IsAvailable ? "OK" : "NO";
        }

        //private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Summary()
        {
            SquirrelButton1.Visibility = Visibility.Hidden;
            SquirrelButton2.Visibility = Visibility.Hidden;
            SquirrelButton3.Visibility = Visibility.Hidden;
            SquirrelButton4.Visibility = Visibility.Hidden;
            SquirrelButton5.Visibility = Visibility.Hidden;
            SquirrelButton6.Visibility = Visibility.Hidden;
            SquirrelButton7.Visibility = Visibility.Hidden;
            SquirrelButton8.Visibility = Visibility.Hidden;

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
                available_button = 1 + (60 - i) / 10;
                while (current_button_num != available_button)
                {
                    var rand = new Random();
                    int randNum = rand.Next(8);
                    if (randNum == 0)
                    {
                        if (SquirrelButton1.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton1.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 1)
                    {
                        if (SquirrelButton2.Visibility == Visibility.Hidden) 
                        {
                            SquirrelButton2.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 2)
                    {
                        if (SquirrelButton3.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton3.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 3)
                    {
                        if (SquirrelButton4.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton4.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 4)
                    {
                        if (SquirrelButton5.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton5.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 5)
                    {
                        if (SquirrelButton6.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton6.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 6)
                    {
                        if (SquirrelButton7.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton7.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                    if (randNum == 7)
                    {
                        if (SquirrelButton8.Visibility == Visibility.Hidden)
                        {
                            SquirrelButton8.Visibility = Visibility.Visible;
                            current_button_num++;
                        }
                    }
                }
            }

            Summary();
        }
        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            TitleButton.Visibility = Visibility.Hidden;
            GameTitle.Visibility = Visibility.Hidden;

            StartGame();
        }

        private void SquirrelButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Visibility == Visibility.Visible)
            {
                score += 50;
                button.Visibility = Visibility.Hidden;
                current_button_num--;
            }
        }

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            Replay.Visibility = Visibility.Hidden;
            Summary_word.Visibility = Visibility.Hidden;
            Score_str.Visibility = Visibility.Hidden;
            Exit.Visibility = Visibility.Hidden;
            score = 0;
            current_button_num = 0;
            StartGame();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
