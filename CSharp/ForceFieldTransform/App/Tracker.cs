using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Blob;
using VideoInputSharp;
using System.Speech.Synthesis;
using System.Runtime.InteropServices;

namespace ForceFieldTransform
{
    /// <summary>
    /// Responsible of Image Processing.
    /// </summary>
    class Tracker
    {
        #region Public Variables

        /// <summary>
        /// The offset of the window used to calculate the Force Field Transform for one pixel.
        /// The window dimension = 2*offset + 1;
        /// </summary>
        public int WindowOffset = 1;
        /// <summary>
        /// Horizontally invert the captured frame from the camera.
        /// </summary>
        public bool InvertHorizontal = false;
        /// <summary>
        /// Vertically invert the captured frame from the camera.
        /// </summary>
        public bool InvertVertical = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Set interval time of the main timer.
        /// </summary>
        public int TimerIntervalTime
        {
            set
            {
                timerIntervalTime = value;
                mainTimer.Interval = timerIntervalTime;
            }

            get
            {
                return timerIntervalTime;
            }
        }

        #endregion

        #region Private Variables

        private readonly int deviceID = 0;
        private readonly int screenHeight = SystemInformation.VirtualScreen.Height;
        private readonly int screenWidth = SystemInformation.VirtualScreen.Width;

        private int timerIntervalTime = 30;
        private int counter = 0;
        private Label labelCounter;

        private bool timerInProgress = false;
        private System.Windows.Forms.Timer fpsTimer;
        private System.Windows.Forms.Timer mainTimer;

        private IplImage frame;
        private IplImage grayFrame;
        private IplImage transformedFrame;
        private VideoInput videoInput;
        private CvWindow window;
        private CvSize size;

        #endregion

        #region Constructor

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="lblCounter"></param>
        public Tracker(ref Label lblCounter)
        {
            labelCounter = lblCounter;
            InitializeComponents();
        }

        #endregion

        #region Public Methods


        /// <summary>
        /// Initialize camera input, frame window and other image objects required.
        /// This is done after getting the settings of the tracker object of this class.
        /// </summary>
        public void InitilizeCamera()
        {
            // Intialize camera
            try
            {
                //capture_ = new Capture(1);
                videoInput = new VideoInput();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Failed to initialize the camera, the program will be closed." +
                    "\n\nThis is the internal error:\n" + exception.Message, "Notify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }

            // small frame to decrease computational complexity
            size = new CvSize(320, 240);

            videoInput.SetupDevice(deviceID, size.Width, size.Height);
            videoInput.SetIdealFramerate(deviceID, 30);

            frame = new IplImage(size, BitDepth.U8, 3);
            grayFrame = new IplImage(size, BitDepth.U8, 1);
            transformedFrame = new IplImage(size, BitDepth.U8, 1);

            // window to view what's going on
            window = new CvWindow("Force Field Transform", WindowMode.KeepRatio);
            window.Resize(320, 240);
            window.Move(screenWidth - 614, 55);
        }

        /// <summary>
        /// used to dispose any object created from this class
        /// </summary>
        public void Dispose()
        {
            if (timerInProgress)
                mainTimer.Stop();

            if (mainTimer != null)
                mainTimer.Dispose();

            if (window != null)
            {
                window.Close();
                window.Dispose();
            }

            //if (capture_ != null)
            //    capture_.Dispose();

            if (videoInput != null)
            {
                videoInput.StopDevice(deviceID);
                videoInput.Dispose();
            }
        }

        /// <summary>
        /// Start mainThread, that starts tracking
        /// </summary>
        public void StartProcessing()
        {
            mainTimer.Start();
            fpsTimer.Start();
            window.Resize(size.Width, size.Height);
            timerInProgress = true;
        }

        /// <summary>
        /// Stop mainThread, that stops tracking
        /// </summary>
        public void StopProcessing()
        {
            mainTimer.Stop();
            fpsTimer.Stop();
            timerInProgress = false;
        }

        /// <summary>
        /// Apply the field force transform on the given image and show it on the window.
        /// </summary>
        /// <param name="image"></param>
        public void TransformImage(IplImage image)
        {
            BitDepth depth = BitDepth.U8;
            int channels = 1;

            IplImage grayImage = new IplImage(image.Size, depth, channels);
            IplImage transformedImage = new IplImage(image.Size, depth, channels);

            Cv.CvtColor(image, grayImage, ColorConversion.BgrToGray);
            transformedImage = ForceFieldTransform(grayImage);

            // window to view what's going on
            window.Resize(transformedImage.Width, transformedImage.Height);
            window.Image = transformedImage;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize Camera, timer and some objects
        /// </summary>
        private void InitializeComponents()
        {
            // initialize mainTimer
            mainTimer = new System.Windows.Forms.Timer();
            mainTimer.Interval = timerIntervalTime;
            mainTimer.Tick += ProcessFrame;

            // initialize timer used to count frames per seconds of the camera
            fpsTimer = new System.Windows.Forms.Timer();
            fpsTimer.Interval = 1000;
            fpsTimer.Tick += new EventHandler((object obj, EventArgs eventArgs) =>
            {
                labelCounter.Text = counter.ToString();
                counter = 0;
            });

            //int min, max = 0, SteppingDelta, currentValue, flags, defaultValue;
            //vi.GetVideoSettingCamera(deviceID, vi.PropZoom, out min, ref max, out SteppingDelta, out currentValue, out flags, out defaultValue);
            //MessageBox.Show("min" + min.ToString() + " max" + max.ToString() + " steppingDelta" + SteppingDelta + " currentValue" + currentValue + " flags" + flags + " defaultValue" + defaultValue);
        }

        /// <summary>
        /// Image Processing. It is done using OpenCVSharp Library.
        /// </summary>
        private void ProcessFrame(object sender, EventArgs e)
        {
            // increment counter
            counter++;
            videoInput.GetPixels(deviceID, frame.ImageData, false, !InvertVertical);
            if (InvertHorizontal)
            {
                frame.Flip(frame, FlipMode.Y);
            }

            // convert to grayscale image
            Cv.CvtColor(frame, grayFrame, ColorConversion.BgrToGray);

            // apply force field transform
            transformedFrame = ForceFieldTransform(grayFrame);

            //Cv.Threshold(grayFrame, grayFrame, grayLowValue, 255, ThresholdType.Binary);
            //Cv.CvtColor(grayFrame, frame, ColorConversion.GrayToBgr);
            //Cv.Smooth(grayFrame, grayFrame, SmoothType.Gaussian, smoothGaussianValue);            

            // show image on the separate window
            window.Image = transformedFrame;
        }

        /// <summary>
        /// Apply Force field transform on the given image.
        /// </summary>
        private IplImage ForceFieldTransform(IplImage image)
        {
            IplImage result = new IplImage(image.Size, image.Depth, image.NChannels);

            CvScalar color;
            byte pixel;
            int imageStepWidth = image.WidthStep;
            int resultStepWidth = result.WidthStep;
            double scalar;
            double[] force;
            int[] r = new int[2];

            // loop on the rows of the image
            for (int y = WindowOffset; y < image.Height - WindowOffset; y++)
            {
                // loop on the columns of the image
                for (int x = WindowOffset; x < image.Width - WindowOffset; x++)
                {
                    force = new double[2];

                    for (int yy = y - WindowOffset; yy <= y + WindowOffset; yy++)
                    {
                        for (int xx = x - WindowOffset; xx <= x + WindowOffset; xx++)
                        {
                            if (xx != x && yy != y)
                            {
                                r[0] = xx - x;
                                r[1] = yy - y;

                                color = image[yy, xx];
                                pixel = (byte)color.Val0;

                                scalar = pixel / Math.Pow(Math.Sqrt(Math.Pow(r[0], 2) + Math.Pow(r[1], 2)), 3);

                                force[0] += r[0] * scalar;
                                force[1] += r[1] * scalar;
                            }
                        }
                    }

                    pixel = (byte)Math.Sqrt(Math.Pow(force[0], 2) + Math.Pow(force[1], 2));
                    result[y, x] = new CvScalar(pixel);
                }
            }

            return result;
        }

        #endregion

        #region Obsolete Methods

        [Obsolete("Please used ForceFieldTransform() instead.", true)]
        private IplImage ForceFieldTransform_(IplImage image)
        {
            IplImage result = new IplImage(image.Size, image.Depth, image.NChannels);

            unsafe
            {
                int index;
                byte pixel;
                int imageStepWidth = image.WidthStep;
                int resultStepWidth = result.WidthStep;
                int offset = 3;
                byte* ptr_img = (byte*)image.ImageData;
                byte* ptr_result = (byte*)result.ImageData;
                double scalar;
                double[] force;
                int[] r = new int[2];

                // loop on the rows of the image
                for (int y = offset; y < image.Height - offset; y++)
                {
                    // loop on the columns of the image
                    for (int x = offset; x < image.Width - offset; x++)
                    {
                        force = new double[2];

                        for (int yy = y - offset; yy <= y + offset; yy++)
                        {
                            for (int xx = x - offset; xx <= x + offset; xx++)
                            {
                                if (xx != x && yy != y)
                                {
                                    r[0] = xx - x;
                                    r[1] = yy - y;

                                    index = (imageStepWidth * yy) + xx;
                                    pixel = ptr_img[index];

                                    scalar = pixel / Math.Pow(Math.Sqrt(Math.Pow(r[0], 2) + Math.Pow(r[1], 2)), 3);

                                    force[0] += r[0] * scalar;
                                    force[1] += r[1] * scalar;
                                }
                            }
                        }

                        index = (resultStepWidth * y) + x;
                        ptr_result[index] = (byte)Math.Sqrt(Math.Pow(force[0], 2) + Math.Pow(force[1], 2));
                    }
                }
            }

            return result;
        }

        [Obsolete("Please used ForceFieldTransform() instead.", true)]
        private IplImage ForceFieldTransform__(IplImage image)
        {
            IplImage result = new IplImage(image.Size, image.Depth, image.NChannels);

            int index;
            byte pixel;
            int imageStepWidth = image.WidthStep;
            int resultStepWidth = result.WidthStep;
            int offset = 3;
            IntPtr ptr_img = image.ImageData;
            IntPtr ptr_result = result.ImageData;
            double scalar;
            double[] force;
            int[] r = new int[2];

            // loop on the rows of the image
            for (int y = offset; y < image.Height - offset; y++)
            {
                // loop on the columns of the image
                for (int x = offset; x < image.Width - offset; x++)
                {
                    force = new double[2];

                    for (int yy = y - offset; yy <= y + offset; yy++)
                    {
                        for (int xx = x - offset; xx <= x + offset; xx++)
                        {
                            if (xx != x && yy != y)
                            {
                                r[0] = xx - x;
                                r[1] = yy - y;

                                index = (imageStepWidth * yy) + xx;
                                pixel = Marshal.ReadByte(ptr_img, index);

                                scalar = pixel / Math.Pow(Math.Sqrt(Math.Pow(r[0], 2) + Math.Pow(r[1], 2)), 3);

                                force[0] += r[0] * scalar;
                                force[1] += r[1] * scalar;
                            }
                        }
                    }

                    index = (resultStepWidth * y) + x;
                    Marshal.WriteByte(ptr_result, index, (byte)Math.Sqrt(Math.Pow(force[0], 2) + Math.Pow(force[1], 2)));
                }
            }

            return result;
        }

        #endregion
    }
}