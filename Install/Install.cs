﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace OnlineSetup
{
    public partial class Install : Form
    {
        public Install()
        {
            InitializeComponent();
        }

        private void Install_Load(object sender, EventArgs e)
        {
            if (pictureBox1.Size != new Size(76, 76))
            {
                pictureBox1.Image = ResizeImage(Properties.Resources.Logo, pictureBox1.Size.Width, pictureBox1.Size.Height);
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void Install_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.antiExit)
            {
                e.Cancel = true;
                MessageBox.Show("Please wait until the installation is finished", "CSClock Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Install_Shown(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Program.Install();
        }
    }
}