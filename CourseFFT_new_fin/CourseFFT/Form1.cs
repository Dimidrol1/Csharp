using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;

using NAudio.Wave;
using NAudio.CoreAudioApi;


namespace CourseFFT
{
    public partial class Form1 : Form
    {

        // MICROPHONE ANALYSIS SETTINGS
        private int RATE; // sample rate of the sound card
        private int BUFFERSIZE; // must be a multiple of 2
        private string FileName_wav,FileName_mp3, ReadFileName;
        private int mp3_number;
        double[] fftReal_buff = new double[2048];
        int color_index = 0;
        // prepare class objects
        public BufferedWaveProvider bwp;


        public Form1()
        {

            InitializeComponent();
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            SetupGraphLabels();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog

            {
                InitialDirectory = System.IO.Path.Combine(Application.StartupPath, @"Examples"),
            Filter = "WAV files(*.wav) | *.wav|MP3 files(*.mp3) | *.mp3"
                

            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            FileName_wav = openFileDialog.FileName;
            if (System.IO.Path.GetExtension(FileName_wav) == ".mp3")
            {
                FileName_mp3 = @"Temp_mp3\" + "mp"+mp3_number+".wav";
                Mp3ToWav(FileName_wav, FileName_mp3);
                PlotLatestData(FileName_mp3);
                mp3_number++;
            }
            else PlotLatestData(FileName_wav);
        }

        public static void Mp3ToWav(string mp3File, string outputFile)
        {
            using (Mp3FileReader reader = new Mp3FileReader(mp3File))
            {
                using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(outputFile, pcmStream);
                }
            }
        }

        public void SetupGraphLabels()
        {
            ucPlot1.fig.labelTitle = "PCM";
            ucPlot1.fig.labelY = "Amplitude (PCM)";
            ucPlot1.fig.labelX = "Time (s)";
            ucPlot1.Redraw();

            ucPlot2.fig.labelTitle = "FFT";
            ucPlot2.fig.labelY = "Power (raw)";
            ucPlot2.fig.labelX = "Frequency (KHz)";
            ucPlot2.Redraw();

            ucPlot3.fig.labelTitle = "FFT comparison";
            ucPlot3.fig.labelY = "Power (raw)";
            ucPlot3.fig.labelX = "Frequency (KHz)";
            ucPlot3.Redraw();
        }

        public bool needsAutoScaling = true;
        public void PlotLatestData(string file)
        {
            toolStripStatusLabel1.Text = "In process";
            
            int it = 0;
            WaveFileReader wf = new WaveFileReader(file);
            int mod = (int)wf.Length;
            while (mod > 1)
            {
                mod = mod / 2;
                it++;
            }
            RATE = wf.WaveFormat.SampleRate;
            BUFFERSIZE = (int)Math.Pow(2, it + 1);
            // check the incoming audio
            var Channels = wf.WaveFormat.Channels;
            var Bits = wf.WaveFormat.BitsPerSample / 16;
            var Coeff = Channels * Bits;
            int frameSize = BUFFERSIZE;
            var audioBytes = new byte[frameSize];
            wf.Read(audioBytes, 0, frameSize);

            // return if there's nothing new to plot
            if (audioBytes.Length == 0)
                return;

            // incoming data is 16-bit (2 bytes per audio point)
            int BYTES_PER_POINT = 2;

            // create a (32-bit) int array ready to fill with the 16-bit data
            int graphPointCount = audioBytes.Length / BYTES_PER_POINT;

            // create double arrays to hold the data we will graph
            double[] pcm = new double[graphPointCount];
            double[] pcmBuff = new double[4096 * Coeff];
            double[] fftReal = new double[2048];
            // populate Xs and Ys with double data
            for (int i = 0; i < graphPointCount; i++)
            {
                // read the int16 from the two bytes
                Int16 val = BitConverter.ToInt16(audioBytes, i * 2);

                // store the value in Ys as a percent (+/- 100% = 200%)
                pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
            }
            int mss = 0;
            switch (it)
            {
                case 30:
                    mss = 262144 / Coeff;
                    break;
                case 29:
                    mss = 131072 / Coeff;
                    break;
                case 28:
                    mss = 65536 / Coeff;
                    break;
                case 27:
                    mss = 32768 / Coeff;
                    break;
                case 26:
                    mss = 16384 / Coeff;
                    break;
                case 25:
                    mss = 8192 / Coeff;
                    break;
                case 24:
                    mss = 4096 / Coeff;
                    break;
                case 23:
                    mss = 2048 / Coeff;
                    break;
                case 22:
                    mss = 1024 / Coeff;
                    break;
                case 21:
                    mss = 512 / Coeff;
                    break;
                case 20:
                    mss = 256 / Coeff;
                    break;
                case 19:
                    mss = 128 / Coeff;
                    break;
                case 18:
                    mss = 64 / Coeff;
                    break;
                case 17:
                    mss = 32 / Coeff;
                    break;
                case 16:
                    mss = 16 / Coeff;
                    break;
                case 15:
                    mss = 8 / Coeff;
                    break;
                case 14:
                    mss = 4 / Coeff;
                    break;
                case 13:
                    mss = 2 / Coeff;
                    break;
                case 12:
                    mss = 1 / Coeff;
                    break;
            }
            double[][] fftBuff = new double[mss][];
            // calculate the full FFT
            if (mss != 1 && mss != 0)
            {
                for (int i = 0; i < mss; i++)
                {
                    Array.Clear(pcmBuff, 0, pcmBuff.Length);
                    Array.Copy(pcm, i * 4096 * Coeff, pcmBuff, 0, pcmBuff.Length);
                    fftBuff[i] = new double[4096 * Coeff];
                    fftBuff[i] = FFT(pcmBuff);
                }
            }
            // just keep the real half (the other half imaginary)
            for (int i = 1; i < mss; i++)
            {
                for (int j = 0; j < 2048; j++)
                {
                    if (fftBuff[0][j] < fftBuff[i][j])
                    {
                        fftBuff[0][j] = fftBuff[i][j];
                    }
                }
            }

            Array.Copy(fftBuff[0], fftReal, fftReal.Length);
            // determine horizontal axis units for graphs
            double pcmPointSpacingMs = RATE*Coeff;
            if (RATE != 44100)
            {
                RATE = 44100;
            }
            double fftMaxFreq = RATE / 2;
            double fftPointSpacingHz = (2048/fftMaxFreq) * 1000;

            Array.Copy(fftReal, fftReal_buff, fftReal.Length);
            // plot the Xs and Ys for both graphs
            ucPlot1.Clear();
            ucPlot1.PlotSignal(pcm, pcmPointSpacingMs, Color.Black); 
            ucPlot2.Clear();
            ucPlot2.PlotSignal(fftReal, fftPointSpacingHz, Color.Red);

            // optionally adjust the scale to automatically fit the data
            needsAutoScaling = true;
            if (needsAutoScaling)
            {
                ucPlot1.AxisAuto();
                ucPlot2.AxisAuto();
                needsAutoScaling = false;
            }

            // this reduces flicker and helps keep the program responsive
            Application.DoEvents();

            toolStripStatusLabel1.Text = "Done";
           
        }

        public double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
                fft[i] = fftComplex[i].Magnitude;
            return fft;
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            this.listBox1.DrawItem += new DrawItemEventHandler(listBox1_DrawItem);
        }

     

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ucPlot2.SaveDialog(System.IO.Path.GetFileNameWithoutExtension(FileName_wav) + ".png");
        }

        private void ucPlot1_Load(object sender, EventArgs e)
        {

        }

        private void recToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f2=new Form2();
            f2.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void plotToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void savebinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string outFileName = System.IO.Path.GetFileNameWithoutExtension(FileName_wav)+".bin";
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = outFileName;
            savefile.InitialDirectory = System.IO.Path.Combine(Application.StartupPath, @"Bin");
            savefile.Filter = "BIN Files (*.bin)|*.bin";
            if (savefile.ShowDialog() == DialogResult.OK) outFileName = savefile.FileName;
            else return;
            using (var writer = new BinaryWriter(File.OpenWrite(outFileName)))
                foreach (double number in fftReal_buff)
                    writer.Write(number);
        }

        private void readbinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (color_index == 6)
            {
                MessageBox.Show("Max=6, please click Clear");
                return;
            }

            
            
            
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = System.IO.Path.Combine(Application.StartupPath, @"Bin"),
                Multiselect=true,
                Filter = "BIN Files (*.bin)|*.bin"
                
        };
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;
            string [] ReadFileName = openFileDialog.FileNames;
            int count_file = ReadFileName.Length;

            if (count_file > 6 || count_file+color_index>6)
            {
                MessageBox.Show("Максимально кол-во графиков, которые можно построить 6!");
                return;
            }
             
            for (int k = 0; k < count_file; k++)
            {
                double[] fftReal_read_bin = new double[2048];
                using (var reader = new BinaryReader(File.OpenRead(ReadFileName[k])))
                    for (int i = 0; i < fftReal_read_bin.Length; i++)
                        fftReal_read_bin[i] = reader.ReadDouble();

                if (RATE != 44100)
                {
                    RATE = 44100;
                }
                double fftMaxFreq = RATE / 2;
                double fftPointSpacingHz = (2048 / fftMaxFreq) * 1000;

                // plot the Xs and Ys for both graphs
                Color[] myColor = new Color[] { Color.Blue, Color.Green, Color.Orange, Color.Red, Color.Brown, Color.Gray };
                ucPlot3.PlotSignal(fftReal_read_bin, fftPointSpacingHz, myColor[color_index]);

                needsAutoScaling = true;
                if (needsAutoScaling)
                {
                    ucPlot3.AxisAuto();
                    needsAutoScaling = false;
                }
                color_index++;
                listBox1.Items.Add(System.IO.Path.GetFileNameWithoutExtension(ReadFileName[k]));
            }
            
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            
                e.DrawBackground();
                Brush myBrush;

                switch (e.Index + 1)
                {
                    case 1:
                        myBrush = Brushes.Blue;
                        break;
                    case 2:
                        myBrush = Brushes.Green;
                        break;
                    case 3:
                        myBrush = Brushes.Orange;
                        break;
                    case 4:
                        myBrush = Brushes.Red;
                        break;
                    case 5:
                        myBrush = Brushes.Brown;
                        break;
                    case 6:
                        myBrush = Brushes.Gray;
                        break;
                    default:
                        myBrush = Brushes.Black;
                        break;

                }

                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
                e.DrawFocusRectangle();
            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            ucPlot3.Clear();
            ucPlot3.Redraw();
            color_index = 0;

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void ucPlot3_Load(object sender, EventArgs e)
        {

        }
    }
}
