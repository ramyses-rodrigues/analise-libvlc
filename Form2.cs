using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace analise_libvlc
{
    public partial class Form2 : Form
    {

        private LibVLC _libVLC;
        private MediaPlayer _mp;
        private Timer _aTimer;
        private List<String> _playlist = new List<string>(1);
        private ContextMenuStrip _playlistContextMenuStrip;

        public Form2()
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            _libVLC = new LibVLC();
            _mp = new MediaPlayer(_libVLC);

            // Create a timer with a two second interval.
            _aTimer = new Timer();
            // Hook up the Elapsed event for the timer. 
            _aTimer.Tick += new EventHandler(On_timer_Tick);
            _aTimer.Interval = 300;
            _aTimer.Enabled = true;

            //videoView1.MediaPlayer = _mp;
            KeyPreview = true;
            Load += On_FormLoad;
            FormClosed += new FormClosedEventHandler(On_FormClosed);
            KeyDown += new KeyEventHandler(On_FormKeyDown);

            // handler para lista de opções de velocidade
            String[] rateList = { "0,5", "1,0", "1,5", "2,0" };
            void ToolStripMenuItemClick(object sender, EventArgs e)
            {
                _mp.SetRate(float.Parse((sender as ToolStripMenuItem).Text));
            }

            // atualiza itens na tsRate
            tsRate.DropDownItems.Clear();
            for (int i = 0; i < rateList.Length; i++)
            {
                tsRate.DropDownItems.Add(rateList[i]); //
                tsRate.DropDownItems[i].Click += new EventHandler(ToolStripMenuItemClick); // associa handler para click
                //playlist.DropDownItems[i].
            }

            // menus de contexto
            _playlistContextMenuStrip = new ContextMenuStrip();
            _playlistContextMenuStrip.Items.Add(new ToolStripMenuItem("Apagar item"));
            _playlistContextMenuStrip.Items.Add(new ToolStripMenuItem("Reproduzir item"));

            //playlist.DropDownItems.AddRange(_playlistContextMenuStrip);

            // formatação da caixa de texto
            richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, 12);
        }

        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 200,
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

        private void openMediaFile(String filePath)
        {            
            if ((filePath == null) || (filePath == String.Empty)) return;
            try
            {
                var media = new Media(_libVLC, new Uri(filePath));
                _mp.Play(media);
                media.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // atualiza playlist            
            atualizaPlaylist();
        }

        private void save2TXT()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "*.rtf|*.rtf";
            dlg.RestoreDirectory = true;

            if (_mp.Media != null)
                dlg.FileName = Path.GetFileNameWithoutExtension(_mp.Media.Mrl) + ".rtf";


            if (dlg.ShowDialog() == DialogResult.OK)
            {
                richTextBox1.SaveFile(dlg.FileName);
            }
        }

        public void insertMediaTimetoText()
        {
            if (_mp == null) return;
            if (_mp.Media == null) return;

            int index = richTextBox1.SelectionStart;
            int line = richTextBox1.GetLineFromCharIndex(index);
                        
            var ctime = _mp.Time; // posição da stream em milisegundos
            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < _mp.Length ? ctime : 0);

            string strText = "Posição " + ts.ToString(@"hh\:mm\:ss") + " - (" + 
                                  (_mp.Time >= 0? _mp.Time.ToString():"0") + ")\r\n";
            richTextBox1.AppendText(strText); // insere texto na posição do cursor
          
        }

        public void GetMediaTimefromText()
        {
            if (richTextBox1.Lines.Length <= 0) return;

            int index = richTextBox1.SelectionStart;
            int line = richTextBox1.GetLineFromCharIndex(index);
            MessageBox.Show("cursor at line " + line.ToString() + ": " + richTextBox1.Lines[line]);
        }

        public void pasteClipboard()
        {
            DataFormats.Format format1 = DataFormats.GetFormat(DataFormats.Bitmap);
            DataFormats.Format format2 = DataFormats.GetFormat(DataFormats.Text);

            //var cbformat = Clipboard.GetImage;
            //var cbtxt = Clipboard.GetText();

            // After verifying that the data can be pasted, paste
            if ((this.richTextBox1.CanPaste(format1)) || (this.richTextBox1.CanPaste(format2)))
            {
                richTextBox1.Paste();
            }
        }

        public void copytoClipboard()
        {
            richTextBox1.Copy();
        }

        public void cuttoClipboard()
        {
            richTextBox1.Cut();
        }

        private void atualizaPlaylist()
        {
            void ToolStripMenuItemClick(object sender, EventArgs e)
            {
                openMediaFile((sender as ToolStripMenuItem).Text);
            }

            void ToolStripMenuItemRClick()
            {

            }

            // atualiza itens na playList
            tsPlaylist.DropDownItems.Clear();
            foreach (var item in _playlist)
            {
                int idx = _playlist.IndexOf(item);
                tsPlaylist.DropDownItems.Add(_playlist[idx]);
                tsPlaylist.DropDownItems[idx].Click += new EventHandler(ToolStripMenuItemClick); // associa handler para click

                // verifica a mídia em reprodução atual para inserir marcação no item, se for o caso
                var list = new Uri(_mp.Media.Mrl); // transforma no formato uri para ser possível comparar
                var aUri = new Uri(tsPlaylist.DropDownItems[idx].Text);
                if (aUri == list) ((ToolStripMenuItem)tsPlaylist.DropDownItems[idx]).Checked = true;
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

        private void getMediaInfo(Media media)
        {
            string FromFourCC(UInt32 fourCC)
            {
                return string.Format(
                    "{0}{1}{2}{3}",
                    (char)(fourCC & 0xff),
                    (char)(fourCC >> 8 & 0xff),
                    (char)(fourCC >> 16 & 0xff),
                    (char)(fourCC >> 24 & 0xff));
            }

            if (media == null) return;

            String songMetadata = "Informações da mídia atual: \r\n";
            songMetadata += "URL: " + new Uri(media.Mrl) + "\r\n\r\n";

            var info = media.Tracks;
            int len = info.Length;

            foreach (var t in info)
            {
                MediaTrack track = t;
                songMetadata += "Track Id: " + track.Id.ToString() + "\r\n";
                songMetadata += "Tipo: " + track.TrackType.ToString() + "\r\n";

                if (track.TrackType == TrackType.Audio)
                    songMetadata += "Canais: " + track.Data.Audio.Channels.ToString() + "\r\n";
                else if (track.TrackType == TrackType.Video)
                {
                    songMetadata += "Orientação: " + track.Data.Video.Orientation.ToString() + "\r\n";
                    songMetadata += "Frame: " + track.Data.Video.Height.ToString() + " x " +
                                                track.Data.Video.Width.ToString() + "\r\n";
                    songMetadata += "Frame Rate N: " + track.Data.Video.FrameRateNum.ToString() + "\r\n";
                    songMetadata += "Frame Rate D: " + track.Data.Video.FrameRateDen.ToString() + "\r\n";
                }

                songMetadata += "Bitrate: " + track.Bitrate.ToString() + "\r\n";
                songMetadata += "Codec: " + FromFourCC(track.Codec) + "\r\n";

                songMetadata += "\r\n";
            }

            MessageBox.Show(songMetadata);
        }


        #region handlers menu e toolbar
        private void abrirToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var filePath = String.Empty;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var _plTemp = openFileDialog.FileNames;
                    _playlist.AddRange(openFileDialog.FileNames); // armazena nomes de arquivos selecionados na _playlist
                    _playlist = _playlist.Distinct().ToList(); // remove duplicados

                    filePath = openFileDialog.FileName; // pega primeiro da lista selecionada e inicia a reprodução
                    openMediaFile(filePath);
                }
            }
        }

        private void abrirURLToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openMediaURL();
        }

        private void informaçõesDaMídiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getMediaInfo(_mp.Media);
        }

        private void salvarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            save2TXT();
        }

        private void colarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteClipboard();
        }

        private void copiarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copytoClipboard();
        }

        private void recortarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cuttoClipboard();
        }

        #endregion

        #region eventos de controles de formulário e timer
        private void On_FormClosed(object sender, FormClosedEventArgs e)
        {
            _aTimer.Stop();
            _mp.Stop();
            _mp.Dispose();
            _libVLC.Dispose();
        }

        private void On_FormLoad(object sender, EventArgs e)
        {
            //var media = new Media(_libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            //_mp.Play(media);
            //media.Dispose();

            //openMediaFile();
        }

        private void On_FormKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space: // pausa simples
                    {
                        if (!richTextBox1.Focused)
                            _mp.SetPause(_mp.IsPlaying);
                        break;
                    }
                case Keys.Subtract: // diminui velocidade de reprodução
                    {
                        if (ModifierKeys == Keys.Control)
                        {
                            _mp.SetRate(_mp.Rate - 0.1f);
                        }
                        break;
                    }
                case Keys.Add: // aumenta velocidade de reprodução
                    {
                        if (ModifierKeys == Keys.Control)
                        {
                            _mp.SetRate(_mp.Rate + 0.1f);
                        }
                        break;
                    }
                case Keys.F1: // pausa/play com retorno
                    {
                        if (_mp.IsPlaying)
                            _mp.SetPause(true);
                        else
                        {
                            _mp.Time -= 3000;
                            _mp.SetPause(false);
                        }
                        break;
                    }
                case Keys.F2: // pausa/play simples
                    {
                        _mp.SetPause(_mp.IsPlaying);
                        break;
                    }
                case Keys.F7: // inserir tempo no texto
                    {
                        insertMediaTimetoText();
                        break;
                    }
                case Keys.F8: // ir para tempo a partir do texto
                    {
                        GetMediaTimefromText();
                        break;
                    }
                case Keys.F12: // salvar texto
                    {
                        save2TXT();
                        break;
                    }
            }
        }

        private void On_timer_Tick(object sender, EventArgs e)
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

            // atualiza progressbar
            progressBar1.Value = pos >= 0 & pos <= 1 ? (int)(pos * 100) : 0;

            // atualiza caption
            this.Text = _state.ToString() + " @rate " + _rate.ToString() +
                        " - " + ts.ToString(@"hh\:mm\:ss") +
                        " / " + tsTotal.ToString(@"hh\:mm\:ss");
        }

        #endregion

        
    }

}
