using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;

namespace Face_Recognition_project
{
    public partial class FaceRecognition : Form
    {

        //Deklaracje

        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> NamePersons = new List<string>();
        int face_num = 0;
        int ContTrain, NumLabels, t;
        string name, names = null;

        private void load_database()
        {
            try
            {
                //załadowanie bazy twarzy i nazw
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels + 1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd przy ładowaniu bazy osób", "Baza", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public FaceRecognition()
        {
            InitializeComponent();
            //załadowanie haarcascade
            face = new HaarCascade("haarcascade_frontalface_alt.xml");
            eye = new HaarCascade("haarcascade_eye.xml");
            load_database();
        }

        // Kamera
        private void button1_Click(object sender, EventArgs e)
        {
            //Inicjalizacja obsługi kamery
            grabber = new Capture();
            grabber.QueryFrame();
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;

        }
        void FrameGrabber(object sender, EventArgs e)
        {

            label3.Text = "0";
            NamePersons.Add("");

            currentFrame = grabber.QueryFrame().Resize(800, 500, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            gray = currentFrame.Convert<Gray, Byte>();
            grabber.FlipHorizontal = true;
            //Funkcja wykrywająca twarz
            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face, 
                1.2,
                3, 
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, 
                new Size(100, 100));

            foreach (MCvAvgComp f in facesDetected[0])
            {
                t = t + 1;
                result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                //rysuje kwadrat na wykrytej twarzy
                currentFrame.Draw(f.rect, new Bgr(Color.Green), 3);


                if (trainingImages.ToArray().Length != 0)
                {

                    double threshold = trackBar1.Value;
                    MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), threshold, ref termCrit);
                    name = recognizer.Recognize(result);
                    //Podpis
                    if (name == "")
                    {
                        currentFrame.Draw("Unknown", ref font, new Point(f.rect.X, f.rect.Y - 10), new Bgr(Color.Red));
                    }
                    else {
                        currentFrame.Draw(name, ref font, new Point(f.rect.X, f.rect.Y - 10), new Bgr(Color.Yellow));
                    }


                }

                NamePersons[t - 1] = name;
                NamePersons.Add("");

                //Liczba wykrytych twarzy
                label3.Text = facesDetected[0].Length.ToString();

            }
            if (wykryjOczyToolStripMenuItem.Checked)
            {
                MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                   eye,
                   1.2,
                   5,
                   Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                   new Size(50, 50)
                   );

                gray.ROI = Rectangle.Empty;

                foreach (MCvAvgComp ey in eyesDetected[0])
                {
                    Rectangle eyeRect = ey.rect;
                    currentFrame.Draw(eyeRect, new Bgr(Color.Red), 2);
                }
            }
            t = 0;

            //Names concatenation of persons recognized
            for (int n = 0; n < facesDetected[0].Length; n++)
            {
                names = names + NamePersons[n] + ", ";
            }
            //label z imionami
            imageBoxFrameGrabber.Image = currentFrame;
            label4.Text = names;
            names = "";
            //Odświerzanie
            NamePersons.Clear();

        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {

                ContTrain = ContTrain + 1;

                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //skalowanie
                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox1.Text);

                //wyświetla dodaną twarz
                imageBox1.Image = TrainedFace;

                //zapisuje do pliku liczbe zapisanych twarzy
                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //zapisuje do pliku imiona do odpowiednich twarzy
                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show("Cześć " + textBox1.Text , "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Uruchom kamere", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        // Przeglądarka
        private void button3_Click(object sender, EventArgs e)
        {
            
            if (face_num < NumLabels)
            {
                pictureBox1.ImageLocation = Application.StartupPath + "/TrainedFaces/face" + (face_num + 1) + ".bmp";
                label6.Text = labels[face_num];
                label8.Text = (face_num +1) + "/" + NumLabels;
                face_num++;
            }
            else if (face_num == NumLabels)
            {
                face_num = 0;
                pictureBox1.ImageLocation = Application.StartupPath + "/TrainedFaces/face" + (face_num + 1) + ".bmp";
                label6.Text = labels[face_num];
                label8.Text = (face_num + 1) + "/" + NumLabels;
                face_num++;
            }
        }
        // Odświerzanie
        private void button4_Click(object sender, EventArgs e)
        {
            load_database();
            pictureBox1.ImageLocation = null;
            face_num = 0;
            label8.Text = face_num + "/" + NumLabels;
            label6.Text = null;
        }
        // Wyjśćie
        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void wykryjZObrazuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            image_detection settingsForm = new image_detection();
            settingsForm.Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            trackBar1.Value = 2000;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

    }
}
