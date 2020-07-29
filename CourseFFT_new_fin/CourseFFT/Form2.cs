using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using NAudio;
using System.Threading;
using System.Media;

namespace CourseFFT
{
    public partial class Form2 : Form
    {
        // WaveIn - поток для записи
        WaveIn waveIn;
        //Класс для записи в файл
        WaveFileWriter writer;
        //Имя файла для записи
        string outputFilename = "Rec.wav";
        Boolean the_world = true;
        
        public Form2()
        {
            InitializeComponent();
        }

        [Obsolete]
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(waveIn_DataAvailable), sender, e);
            }
            else
            {
                //Записываем данные из буфера в файл
                writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            }
        }
        private void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(waveIn_RecordingStopped), sender, e);
            }
            else
            {
                waveIn.Dispose();
                waveIn = null;
                writer.Close();
                writer = null;
            }
        }

        [Obsolete]
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog savefile = new SaveFileDialog();
                savefile.FileName = outputFilename;
                savefile.InitialDirectory = System.IO.Path.Combine(Application.StartupPath, @"Record");
                savefile.Filter = "WAV Files (*.wav)|*.wav";
                if (savefile.ShowDialog() == DialogResult.OK) outputFilename = savefile.FileName;
                else return;

                waveIn = new WaveIn();
                //Дефолтное устройство для записи (если оно имеется)
                //встроенный микрофон ноутбука имеет номер 0
                waveIn.DeviceNumber = 0;
                //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
                waveIn.DataAvailable += waveIn_DataAvailable;
                //Прикрепляем обработчик завершения записи
                waveIn.RecordingStopped += waveIn_RecordingStopped;
                //Формат wav-файла - принимает параметры - частоту дискретизации и количество каналов(здесь mono)
                waveIn.WaveFormat = new WaveFormat(44100, 1);
                //Инициализируем объект WaveFileWriter
                writer = new WaveFileWriter(outputFilename, waveIn.WaveFormat);
                //Начало записи
                waveIn.StartRecording();
                toolStripStatusLabel1.Text = "In process";
                the_world = true;
                StartTimer();

            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }
        }
   
        void StartTimer()
        {
            TimeSpan ts = new TimeSpan(0, 0, 0);
            Task.Factory.StartNew(() => {
                while (the_world != false)
                {
                    label1.Invoke((Action)(() => { label1.Text = ts.ToString(); }));
                    Thread.Sleep(1000);
                    ts = ts.Add(new TimeSpan(0, 0, 1));
                }
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (waveIn != null)
            {

                the_world = false;
                waveIn.StopRecording();
                toolStripStatusLabel1.Text = "Done";
              
                MessageBox.Show("Stop recording");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
           button2_Click(null,null);
        }
    }
}
