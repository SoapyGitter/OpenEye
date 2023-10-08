using Emgu.CV;
using Emgu.CV.Structure;
using FastYolo;
using FastYolo.Model;

namespace a
{

    public partial class Form1 : Form
    {
        YoloWrapper yoloWrapper;
        VideoCapture cap;
        Mat frame = new();

        readonly float MetricsSize = 1.7f;

        readonly Dictionary<string, float> keyLabelPairs = new()
        {
            { "chair", 1.7f },
            { "puff", 1f }
        };

        readonly Dictionary<float, float> DistancePercentageOnAvg1MSize = new Dictionary<float, float>()
        {
            { .95f , 0.5f },
            { .70f , 1f },
            { .60f , 2f },
            { .50f , 3f },
            { .01f , 200f },
        };

        public Form1()
        {
            InitializeComponent();
            Application.Idle += new EventHandler(Init_Click);
        }

        private void Init_Click(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            try
            {
                yoloWrapper = new YoloWrapper(@"C:\\Users\\SoaPisGirseb\\Desktop\\yolo\\fastyolov3.cfg", @"C:\\Users\\SoaPisGirseb\\Desktop\\yolo\\yolov3.weights", @"C:\\Users\\SoaPisGirseb\\Desktop\\yolo\\obj.names");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void UploadImage_Click(object sender, EventArgs e)
        {
            UpdateFrameAsync();
        }

        private float GetNearbyPercentage(float percent)
        {
            var lastDiff = 0f;
            var lastVal = 0f;
            var toReturn = 0f;
            foreach (var item in DistancePercentageOnAvg1MSize)
            {
                if(item.Key > percent)
                {
                    lastDiff = item.Key - percent;
                    lastVal = item.Key;
                }
                else if(lastDiff > percent - item.Key)
                {
                    toReturn = lastVal;
                    break;
                }
                else
                {
                    toReturn = item.Key;
                    break;
                }
            }

            return toReturn;
        }

        private float GetBoxPercentageInFrame(Rectangle rectangle)
        {
            var xDiff = pictureBox1.Width - rectangle.Width - rectangle.X;
            var yDiff = pictureBox1.Height - rectangle.Height - rectangle.Y;
            var xInboundWidth = 0;
            var yInboundWidth = 0;
            if (xDiff < 0)
            {
                xInboundWidth = pictureBox1.Width - rectangle.X;
            }
            if(yDiff < 0)
            {
                yInboundWidth = pictureBox1.Height - rectangle.Y;
            }

            return ((float)xInboundWidth + (float)yInboundWidth) / ((float)pictureBox1.Width + (float)pictureBox1.Height);

        }
        private async Task UpdateFrameAsync()
        {
            string url = "http://192.168.1.148:8080/video";
            cap = new VideoCapture(url);

            while (true)
            {
                frame = cap.QuerySmallFrame();
                if (frame is null) continue;
                var frameImage = frame.ToImage<Bgr, byte>();
                IEnumerable<YoloItem> yoloItems = yoloWrapper.Detect(frameImage.ToJpegData());
                IEnumerable<CustomRectangle> rectangles = yoloItems.Where(x => x.Confidence > 0.8).Select(x =>
                {
                    return new CustomRectangle
                    {
                        Rectangle = new Rectangle
                        {
                            X = x.X,
                            Y = x.Y,
                            Height = x.Height,
                            Width = x.Width
                        },
                        Name = x.Type,
                        Confidence = x.Confidence
                    };
                });

                foreach (var rectangle in rectangles)
                {
                    float diffPercentage = (((float)rectangle.Rectangle.Width + (float)rectangle.Rectangle.Height)/ ((float)pictureBox1.Width + (float)pictureBox1.Height));
                    var distance = GetNearbyPercentage(diffPercentage);
                    var percentageBoxInFrame = GetBoxPercentageInFrame(rectangle.Rectangle);
                    keyLabelPairs.TryGetValue(rectangle.Name, out var label);
                    if (label > MetricsSize)
                    {
                        distance = distance / label;
                    }else if(label < MetricsSize)
                    {
                        distance = distance * label;    
                    }


                    CvInvoke.Rectangle(frameImage, rectangle.Rectangle, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.PutText(frameImage, rectangle.Name, new Point(rectangle.Rectangle.X, rectangle.Rectangle.Y - 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1.0, new MCvScalar(0, 0, 255), 2);
                    CvInvoke.PutText(frameImage, rectangle.Confidence.ToString().Substring(0, 4), new Point(rectangle.Rectangle.X + 50, rectangle.Rectangle.Y - 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1.0, new MCvScalar(0, 0, 233), 2);
                    CvInvoke.PutText(frameImage, "Distance: " + distance.ToString(), new Point(rectangle.Rectangle.X + 100, rectangle.Rectangle.Y - 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1.0, new MCvScalar(0, 0, 233), 2);
                }
                if (pictureBox1.InvokeRequired)
                {
                    pictureBox1.Invoke((Action)(() =>
                    {
                        pictureBox1.Image?.Dispose();
                        pictureBox1.Image = new Bitmap(frameImage.ToBitmap(), pictureBox1.Width, pictureBox1.Height);
                    }));
                }
                else
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = new Bitmap(frameImage.ToBitmap(), pictureBox1.Width, pictureBox1.Height);
                }

                await Task.Delay(200);
            }

        }

        private void GrabbedImage(object? sender, EventArgs e)
        {
            try
            {
                cap.Retrieve(frame);
                if (frame is null) return;
                IEnumerable<YoloItem> yoloItems = yoloWrapper.Detect(frame.ToImage<Bgr, byte>().ToJpegData());
                IEnumerable<CustomRectangle> rectangles = yoloItems.Where(x => x.Confidence > 0.9).Select(x =>
                {
                    return new CustomRectangle
                    {
                        Rectangle = new Rectangle
                        {
                            X = x.X,
                            Y = x.Y,
                            Height = x.Height,
                            Width = x.Width
                        },
                        Name = x.Type,
                        Confidence = x.Confidence
                    };
                });
                var frameImage = frame.ToImage<Bgr, byte>();

                foreach (var rectangle in rectangles)
                {
                    CvInvoke.Rectangle(frameImage, rectangle.Rectangle, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.PutText(frameImage, rectangle.Name, new Point(rectangle.Rectangle.X, rectangle.Rectangle.Y - 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1.0, new MCvScalar(0, 0, 255), 2);
                    CvInvoke.PutText(frameImage, rectangle.Confidence.ToString().Substring(0, 4), new Point(rectangle.Rectangle.X + 50, rectangle.Rectangle.Y - 20), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1.0, new MCvScalar(0, 0, 233), 2);
                    //Console.WriteLine($"Object Found: {item.Type}, Confidence: {item.Confidence} with Shape: {item.Shape}, X: {item.X}, Y: {item.Y}, Width: {item.Width}, Height: {item.Height}");
                }
                if (pictureBox1.InvokeRequired)
                {
                    pictureBox1.Invoke((Action)(() =>
                    {
                        pictureBox1.Image?.Dispose(); // Dispose the previous image if it exists
                        pictureBox1.Image = new Bitmap(frameImage.ToBitmap(), pictureBox1.Width, pictureBox1.Height);
                    }));
                }
                else
                {
                    pictureBox1.Image?.Dispose(); // Dispose the previous image if it exists
                    pictureBox1.Image = new Bitmap(frameImage.ToBitmap(), pictureBox1.Width, pictureBox1.Height);
                }
                Thread.Sleep(200); // Add a delay between frame processing

            }

            catch (Exception ex)
            {

            }
        }


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}