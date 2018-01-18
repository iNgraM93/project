using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;

namespace Face_Recognition_project
{
    public partial class image_detection : Form
    {
        // deklaracje
        string fileName;
        public image_detection()
        {
            InitializeComponent();
        }
        // głowny program
        private void Run()
        {
            //Odczyt na 8-bit
            Image<Bgr, Byte> image = new Image<Bgr, byte>(fileName);   
            // szare
            Image<Gray, Byte> gray = image.Convert<Gray, Byte>();

            //pomiar czasu
            Stopwatch watch = Stopwatch.StartNew();
            //jasność , kontrast poprawa
            gray._EqualizeHist();

            //HaarCascae 
            HaarCascade face = new HaarCascade("haarcascade_frontalface_default.xml");
            HaarCascade eye = new HaarCascade("haarcascade_eye.xml");

            //Wykrycie i zapisanie do kwadratu
            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
            face, 
            1.2, 
            10,
            Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, 
            new Size(20, 20));

            foreach (MCvAvgComp f in facesDetected[0])
            {
                //rysowanie kwadratu
                image.Draw(f.rect, new Bgr(Color.Blue), 2);
                gray.ROI = f.rect;
                
                if (checkBox1.Checked)
                {
                    MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                        eye,
                        1.1,
                        10,
                        Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                        new Size(20, 20));
                    gray.ROI = Rectangle.Empty;

                    foreach (MCvAvgComp e in eyesDetected[0])
                    {
                        Rectangle eyeRect = e.rect;
                        eyeRect.Offset(f.rect.X, f.rect.Y);
                        image.Draw(eyeRect, new Bgr(Color.Red), 2);
                    }
                }

            }


            imageBox1.Image = image;
            watch.Stop();

            label3.Text = String.Format("Rozpoznanie twarzy/oczu trwało {0} ms", watch.ElapsedMilliseconds);
            label5.Text = facesDetected[0].Length.ToString();
        }
        // przeglądarka
        private void button1_Click(object sender, EventArgs e)
        {
            // OpenFileDialog dla przegladania plików
            OpenFileDialog opf = new OpenFileDialog();
            //filtr jpg bmp
            opf.Filter = "Choose Image(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*png";
            if (opf.ShowDialog() == DialogResult.OK)
            {
                fileName = opf.FileName;
                label1.Text = fileName;
                Run();
            }
        }
        //WYjście
        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
