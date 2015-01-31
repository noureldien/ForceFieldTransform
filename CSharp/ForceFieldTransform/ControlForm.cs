using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace ForceFieldTransform
{
    partial class ControlForm : Form
    {
        #region Private Constants

        private readonly int screenHeight = SystemInformation.VirtualScreen.Height;
        private readonly int screenWidth = SystemInformation.VirtualScreen.Width;
        
        #endregion

        #region Private Variables
        
        /// <summary>
        /// object from the class that do all the image processing and traking
        /// </summary>
        private Tracker tracking;
        /// <summary>
        /// indicates if traking is running or it is stopped
        /// </summary>
        private bool isTracking;

        #endregion

        #region Constructor
        
        /// <summary>
        /// constructor
        /// </summary>
        public ControlForm()
        {
            InitializeComponent();
        }

        #endregion
        
        #region Event Handlers

        /// <summary>
        /// event handler for form onLoad
        /// </summary>
        private void ControlForm_Load(object sender, EventArgs e)
        {
            // adjust starting position
            //this.StartPosition = FormStartPosition.Manual;
            //int bottom = Screen.PrimaryScreen.WorkingArea.Bottom;
            //SystemInformation.VirtualScreen.Bottom;           
            this.Location = new Point(screenWidth - this.Width - 4, 60);
        }

        /// <summary>
        /// Event handler for the application window first show.
        /// </summary>       
        private void ControlForm_Shown(object sender, EventArgs e)
        {
            // initialize the tracker            
            tracking = new Tracker(ref label1);
            // At the first time, all required objects for the camera capturing are defined.
            tracking.InitilizeCamera();
            // return focus back to the windows form as it was unfocused due
            // to initializing camera window
            this.Focus();
        }

        /// <summary>
        /// safely dispose some objects to free resources
        /// </summary>        
        private void ControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isTracking)
            {
                tracking.StopProcessing();
            }

            tracking.Dispose();
            this.Dispose(true);
        }

        /// <summary>
        /// capture video stream
        /// </summary>        
        private void buttonCapture_Click(object sender, EventArgs e)
        {
            if (isTracking)
            {
                tracking.StopProcessing();
                buttonCapture.Text = "Capture";
            }
            else
            {
                tracking.StartProcessing();
                buttonCapture.Text = "Stop";
            }
            isTracking = !isTracking;
        }

        /// <summary>
        /// change some parameters of blob detection and tracking
        /// </summary>        
        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            switch (((NumericUpDown)sender).Name)
            {
                case "numericUpDown1":
                    tracking.TimerIntervalTime = (int)numericUpDown1.Value;
                    break;
                case "numericUpDown2":
                    tracking.WindowOffset = (int)numericUpDown2.Value;
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// checkboxes in the settings tab (H. and V. flip, play sound)
        /// </summary>        
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            tracking.InvertHorizontal = checkBox2.Checked;
            tracking.InvertVertical = checkBox3.Checked;
        }

        /// <summary>
        /// Take a picture and apply Force Field Transform on it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPicture_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            DialogResult result = ofd.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            OpenCvSharp.IplImage image = OpenCvSharp.IplImage.FromFile(ofd.FileName);
            tracking.TransformImage(image);
        }

        #endregion        
    }
}