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

        private int _step = 3000;

        public Form2()
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();

            try
            {
                _libVLC = new LibVLC();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Exit();
            }

            // cria objeto Media Player e configura manipuladores de eventos
            _mp = new MediaPlayer(_libVLC);
            _mp.EndReached += new EventHandler<EventArgs>(On_EndReached);
            _mp.Stopped += new EventHandler<EventArgs>(On_Stopped);
            _mp.TimeChanged += new EventHandler<MediaPlayerTimeChangedEventArgs>(On_MediaPlayerTimerChanged);
            
            // cria um timer.
            _aTimer = new Timer();
            _aTimer.Tick += new EventHandler(On_TimerTick);
            _aTimer.Interval = 300;
            _aTimer.Enabled = true;

            //videoView1.MediaPlayer = _mp;
            // configura form
            KeyPreview = true;
            Load += new EventHandler(On_FormLoad);
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

        #region Funções utilitárias
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

        private void Save2TXT()
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
            if (MediaPlayerNotOK()) return;

            int index = richTextBox1.SelectionStart;
            int line = richTextBox1.GetLineFromCharIndex(index);

            var ctime = _mp.Time; // posição da stream em milisegundos
            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < _mp.Length ? ctime : 0);

            string strText = "Posição " + ts.ToString(@"hh\:mm\:ss") + " - (" +
                                  (_mp.Time >= 0 ? _mp.Time.ToString() : "0") + ")\r\n";
            richTextBox1.AppendText(strText); // insere texto na posição do cursor

        }

        public void GetMediaTimefromText()
        {
            if (richTextBox1.Lines.Length <= 0) return;

            int index = richTextBox1.SelectionStart;
            int line = richTextBox1.GetLineFromCharIndex(index);
            MessageBox.Show("cursor at line " + line.ToString() + ": " + richTextBox1.Lines[line]);
        }

        public void PasteFromClipboard(object sender, EventArgs e)
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

        public void CopytoClipboard(object sender, EventArgs e)
        {
            richTextBox1.Copy();
        }

        public void CuttoClipboard(object sender, EventArgs e)
        {
            richTextBox1.Cut();
        }

        private void AtualizaPlaylist()
        {
            void ToolStripMenuItemClick(object sender, EventArgs e)
            {
                OpenMediaFile((sender as ToolStripMenuItem).Text);
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

        private void OpenMediaFile(String filePath) // abre para reprodução de UM arquivo de mídia
        {
            if ((filePath == null) || (filePath == String.Empty)) return;
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Arquivo não encontrado! ");
                return;
            }

            try
            {
                var media = new Media(_libVLC, new Uri(filePath));
                
                // se tiver stream de vídeo cria outra janela? mas com foco no richtext
                //_mp.Hwnd = base.Handle; // handle do form principal. TO DO: Criar outra janela
                
                if (!_mp.Play(media))
                    MessageBox.Show("erro na reprodução!");
                media.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // atualiza playlist            
            AtualizaPlaylist();
        }

        private void PlayPause(object sender, EventArgs e)
        {
            if (_mp.State == VLCState.Stopped)
            {
                _mp.Play(); // reproduz do início
                return;
            }

            _mp.SetPause(_mp.IsPlaying);
        }

        private void Stop(object sender, EventArgs e)
        {
            _mp.Stop();
        }

        private void Backward(object sender, EventArgs e) // pagedown
        {            
            if ((_mp.State == VLCState.Paused) || (_mp.State == VLCState.Playing))
                _mp.Time -= _step;
        }

        private void Forward(object sender, EventArgs e) // pageup
        {
            if ((_mp.State == VLCState.Paused) || (_mp.State == VLCState.Playing))
            {
                var len = _mp.Length;
                var time = _mp.Time;
                _mp.Time += time + _step > len ? len - time : _step;
            }
        }

        private void GetPicture()
        {
            //const char *image_path="/home/vinay/Documents/snap.png";
            //int result = libvlc_video_take_snapshot(mp, 0, image_path, 0, 0);
            // parece que "file:" está atrapalhando...                       

            //obtém dimensões informações sobre o frame
            var vtrackidx = _mp.VideoTrack; // obtém índice da stream do fluxo de vídeo
            if (vtrackidx < 0) return;

            var info = _mp.Media.Tracks;
            long time = _mp.Time;

            uint w = _mp.Media.Tracks[vtrackidx].Data.Video.Width;
            uint h = _mp.Media.Tracks[vtrackidx].Data.Video.Height;

            String _path = _mp.Media.Mrl;
            _path = _path.Replace("file:///", ""); // retira o termo "file:///" que vem na string Mrl...
                                                   //_path = _path.Substring(8); 
            String image_path = Path.GetDirectoryName(_path) + "\\" + // constroi nome único
                                Path.GetFileNameWithoutExtension(_path) +
                                "-" + time.ToString() + ".png";

            // observar que qualquer falha no nome do arquivo a função não funciona, mesmo retornando OK
            if (_mp.TakeSnapshot(0, image_path, w, h))
                MessageBox.Show("Arquivo " + Path.GetFileNameWithoutExtension(_path)
                    + " salvo com sucesso na pasta " + Path.GetDirectoryName(_path));
        }

        private int CalcProgressBarRelativeMouse(object sender, EventArgs e)
        {
            // Get mouse position(x) minus the width of the progressbar (so beginning of the progressbar is mousepos = 0 //
            float absoluteMouse = (PointToClient(MousePosition).X - (sender as ToolStripProgressBar).Bounds.X);
            // Calculate the factor for converting the position (progbarWidth/100) //
            float calcFactor = (sender as ToolStripProgressBar).Width / (float)(sender as ToolStripProgressBar).Maximum;
            // In the end convert the absolute mouse value to a relative mouse value by dividing the absolute mouse by the calcfactor //
            float relativeMouse = absoluteMouse / calcFactor;

            return Convert.ToInt32(relativeMouse);
        }

        private bool MediaPlayerNotOK()
        {
            return ((_mp == null) || (_mp.Media == null));
        }

        #endregion

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
                    OpenMediaFile(filePath); // reproduz a mídia e atualiza a playlist da janela
                    //AtualizaPlaylist();
                }
            }
        }

        private void abrirURLToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var filePath = ShowDialog("Entre com a URL", "URL: ");
            if (filePath != string.Empty)
            {
                OpenMediaFile(filePath);
            }
        }

        private void informaçõesDaMídiaToolStripMenuItem_Click(object sender, EventArgs e)
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

            if (MediaPlayerNotOK()) return;
            Media media = _mp.Media;

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
                    songMetadata += "Frame Rate: " + ((float)track.Data.Video.FrameRateNum /
                                                            track.Data.Video.FrameRateDen).ToString("F2") + " fps \r\n";
                }

                songMetadata += "Bitrate: " + track.Bitrate.ToString() + "\r\n";
                songMetadata += "Codec: " + FromFourCC(track.Codec) + "\r\n";

                songMetadata += "\r\n";
            }

            MessageBox.Show(songMetadata);
        }

        private void salvarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save2TXT();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            if (MediaPlayerNotOK()) return;

            int pos = CalcProgressBarRelativeMouse(sender, e); // retorna um inteiro [0:100] com a posição relativa do mouse sobre a barra
            if (pos >= 0)
                _mp.Position = (float)pos / 100;
        }

        private void progressBar1_MouseMove(object sender, MouseEventArgs e)
        {
            if (MediaPlayerNotOK()) return;
            
            int pos = CalcProgressBarRelativeMouse(sender, e); // retorna um inteiro [0:100] com a posição relativa do mouse
            var iPos = (pos * _mp.Length / 100);

            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(iPos > 0 & iPos < _mp.Length ? iPos : 0);
            statusLabel1.Text = "Ir para posição: " + ts.ToString(@"hh\:mm\:ss");
        }

        private void carregarTextoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MediaPlayerNotOK()) return;

            var filePath = String.Empty;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "All files (*.rtf)|*.rtf";
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_mp.Media.Mrl);
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    if (File.Exists(filePath))
                        richTextBox1.LoadFile(filePath);
                }
            }
        }

        #endregion

        #region manipuladores de eventos de componentes, controles de formulário e timer
        private void On_EndReached(object sender, EventArgs e)
        {
            // verifica a mídia em reprodução atual para inserir marcação no item, se for o caso
            //var sfile = new Uri(_mp.Media.Mrl); // transforma no formato uri para ser possível comparar
            //MessageBox.Show("Final da stream: " + sfile.ToString());
            //_mp.Stop();
            //openMediaFile(sfile.ToString());
            //var aUri = new Uri(tsPlaylist.DropDownItems[idx].Text);
            //if (aUri == list) ((ToolStripMenuItem)tsPlaylist.DropDownItems[idx]).Checked = true;
            //_mp.Media = _mp.Media;
            //_mp.Stop();
        }

        private void On_Stopped(object sender, EventArgs e)
        {
            // atualiza caption
            //this.Text = _mp.State.ToString() + " @rate " + _mp.Rate.ToString();

            //// verifica a mídia em reprodução atual para inserir marcação no item, se for o caso
            //var sfile = new Uri(_mp.Media.Mrl); // transforma no formato uri para ser possível comparar
            //MessageBox.Show("Parou a reprodução do item: " + sfile.ToString());
            ////_mp.Stop();
            ////openMediaFile(sfile.ToString());
            ////var aUri = new Uri(tsPlaylist.DropDownItems[idx].Text);
            ////if (aUri == list) ((ToolStripMenuItem)tsPlaylist.DropDownItems[idx]).Checked = true;
            ////_mp.Media = _mp.Media;
            ////_mp.Stop();
        }

        private void On_TimerTick(object sender, EventArgs e)
        {
            if (MediaPlayerNotOK()) return;

            //return;
            //if ((_mp.State == VLCState.Paused) || (_mp.State == VLCState.Playing) || (_mp.State == VLCState.Stopped))
            //{
            var pos = _mp.Position; // obtém a posição da stream em percentual
            var length = _mp.Length; // tamanho total da string em milisegundos 
            var ctime = _mp.Time; // posição da stream em milisegundos
            var _state = _mp.State;
            var _rate = _mp.Rate;

            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < length ? ctime : 0);
            TimeSpan tsTotal = TimeSpan.FromMilliseconds(length > 0 ? length : 0);

            // atualiza progressbar
            int val = Convert.ToInt32(pos * 100);
            val = val >= 0 & val <= 100 ? val : 0;
            progressBar1.Value = val;
 
            // atualiza caption
            this.Text = _state.ToString() + " @rate " + _rate.ToString() +
                        " - " + ts.ToString(@"hh\:mm\:ss") +
                        " / " + tsTotal.ToString(@"hh\:mm\:ss");
            //}
        }

        public delegate void OnMediaPlayerTimerChangedDelegate(String txt, int v);
        private void On_MediaPlayerTimerChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            void SetFormCaption(String capt, int val)
            {
                Text = capt;
                progressBar1.Value = val;
            }

            // intervalo???
            return;
            //if (mediaPlayerNotOK()) return;
            //if ((_mp.State == VLCState.Paused) || (_mp.State == VLCState.Playing) || (_mp.State == VLCState.Stopped))
            //{
            var pos = _mp.Position; // obtém a posição da stream em percentual
            var length = _mp.Length; // tamanho total da string em milisegundos 
            var ctime = e.Time; // posição da stream em milisegundos
            var _state = _mp.State;
            var _rate = _mp.Rate;

            // converte para formato de hh:min:seg
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < length ? ctime : 0);
            TimeSpan tsTotal = TimeSpan.FromMilliseconds(length > 0 ? length : 0);

            // atualiza progressbar
            int val = Convert.ToInt32(pos * 100);
            val = val >= 0 & val <= 100 ? val : 0;

            String text = _state.ToString() + " @rate " + _rate.ToString() +
                        " - " + ts.ToString(@"hh\:mm\:ss") +
                        " / " + tsTotal.ToString(@"hh\:mm\:ss");

            if (InvokeRequired)
            {
                OnMediaPlayerTimerChangedDelegate callback = SetFormCaption;
                Invoke(callback, text, val);
            }

            //statusLabel1.Text = TimeSpan.FromMilliseconds(e.Time).ToString().Substring(0, 8);
            //}
        }

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
            string fpath = Directory.GetCurrentDirectory() + "/media/teste.mp3";
            _playlist.Add(fpath);
            OpenMediaFile(fpath);
            //openMediaFile("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
        }

        private void On_FormKeyDown(object sender, KeyEventArgs e)
        {
            if (MediaPlayerNotOK()) return;

            switch (e.KeyCode)
            {
                case Keys.Escape: // pausa simples
                    {
                        Stop(null, null);                        
                        break;
                    }
                case Keys.Space: // pausa simples
                    {
                        if (!richTextBox1.Focused)
                            PlayPause(null, null);
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
                        if (_mp.State == VLCState.Stopped)
                        {
                            _mp.Play(); // reproduz do início
                            break;
                        }

                        if (_mp.IsPlaying)
                            _mp.SetPause(true);
                        else
                        {
                            _mp.Time -= _step;
                            _mp.SetPause(false);
                        }
                        break;
                    }
                case Keys.F2: // pausa/play simples
                    {
                        PlayPause(null, null);
                        break;
                    }
                case Keys.F6: // take a snapshot
                    {
                        GetPicture();
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
                        Save2TXT();
                        break;
                    }
                case Keys.PageDown: // retorna xx milisegundos (to do: implementar avançar frame a frame!)
                    {
                        if (richTextBox1.Focused)
                            e.SuppressKeyPress = true;
                        Backward(null, null);                        
                        break;
                    }
                case Keys.PageUp: // avança xx milisegundos
                    {                        
                        if (richTextBox1.Focused)
                            e.SuppressKeyPress = true;
                        Forward(null, null);
                        break;
                    }
            }
        }
        
        #endregion
    }

}
