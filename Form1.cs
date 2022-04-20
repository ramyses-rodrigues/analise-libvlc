using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace analise_libvlc
{
    public partial class Form1 : Form
    {
        public LibVLC _libVLC;
        public MediaPlayer _mp;
        private Timer aTimer;
        
        public Form1()
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            _libVLC = new LibVLC();
            _mp = new MediaPlayer(_libVLC);

            // Create a timer with a two second interval.
            aTimer = new Timer();
            // Hook up the Elapsed event for the timer. 
            aTimer.Tick += t_timer_Tick;
            aTimer.Interval = 500;
            aTimer.Enabled = true;

            videoView1.MediaPlayer = _mp;
            Load += Form1_Load;
            FormClosed += Form1_FormClosed;
        }

        private void t_timer_Tick(object sender, EventArgs e)
        {
            if (_mp == null) return;

            var pos = _mp.Position; // obtém a posição da stream em percentual
            var length = _mp.Length; // tamanho total da string em milisegundos 
            var ctime = _mp.Time; // posição da stream em milisegundos
            var _state = _mp.State;
            var _rate = _mp.Rate;

            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < length ? ctime : 0);
            TimeSpan tsTotal = TimeSpan.FromMilliseconds(length > 0 ? length : 0);

            // atualiza caption
            this.Text = _state.ToString() + " @rate " + _rate.ToString() +
                        " - " + ts.ToString(@"hh\:mm\:ss") +
                        " / " + tsTotal.ToString(@"hh\:mm\:ss");
        }

        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 250,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 100, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        private void openMediaFile()
        {
            //var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    var media = new Media(_libVLC, new Uri(filePath));
                    _mp.Play(media);
                    media.Dispose();
                }
            }
        }

        private void openMediaURL()
        {
            var filePath = ShowDialog("Entre com a URL", "URL: ");
            if (filePath != string.Empty)
            {
                var media = new Media(_libVLC, new Uri(filePath));
                _mp.Play(media);
                media.Dispose();
            }

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            aTimer.Stop();
            _mp.Stop();
            _mp.Dispose();
            _libVLC.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //var media = new Media(_libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            //_mp.Play(media);
            //media.Dispose();

            //openMediaFile();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    {
                        _mp.SetPause(_mp.IsPlaying);
                        break;
                    }
                case Keys.Subtract:
                    {
                        if (ModifierKeys == Keys.Control)
                        {
                            _mp.SetRate(_mp.Rate - 0.1f);
                        }

                        break;
                    }
                case Keys.Add:
                    {
                        if (ModifierKeys == Keys.Control)
                        {
                            _mp.SetRate(_mp.Rate + 0.1f);
                        }

                        break;
                    }
            }
        }

        private void abrirToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openMediaFile();
        }

        private void abrirURLToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openMediaURL();
        }

        private void novaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form2 form2 = new Form2())
            {
                form2.ShowDialog();
            }
        }

    }
}
