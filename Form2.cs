using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using Microsoft.VisualBasic;
using System.Diagnostics;
using static System.Windows.Forms.AxHost;
using System.Drawing.Text;

namespace analise_libvlc
{
    public partial class Form2 : Form
    {

        #region variáveis globais
        private readonly LibVLC _libVLC; // engine
        private MediaPlayer _mp; // mediaplayer
        private Timer _aTimer; // timer
        private List<String> _playlist; // armazena playlist
        private ContextMenuStrip _playlistContextMenuStrip; // 
        private int _step = 3000; // passo do player
        private long[] _interval = new long[2] { 0, 0 }; // armazenar trecho selecionado
        private bool _mouseInProgressBar = false;

        #endregion

        #region Inicialização
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
            // Note: You may create multiple mediaplayers from a single LibVLC instance
            _mp = new MediaPlayer(_libVLC);
            _mp.EndReached += new EventHandler<EventArgs>(On_EndReached);
            _mp.Stopped += new EventHandler<EventArgs>(On_Stopped);
            _mp.TimeChanged += new EventHandler<MediaPlayerTimeChangedEventArgs>(On_MediaPlayerTimerChanged);
            _mp.Forward += new EventHandler<EventArgs>(On_MediaPlayerForward);
            _mp.Backward += new EventHandler<EventArgs>(On_MediaPlayerBackward);
            _mp.PositionChanged += new EventHandler<MediaPlayerPositionChangedEventArgs>(On_PositionChanged);

            // cria um timer para exibição de instante de tempo e outras funções para o usuário.
            _aTimer = new Timer();
            _aTimer.Tick += new EventHandler(On_TimerTick);
            _aTimer.Interval = 300;
            _aTimer.Enabled = true;

            // cria a playlist para armazenar os caminhos para cada arquivo de mídia a ser reproduzido
            _playlist = new List<string>(1);

            // configura form principal
            KeyPreview = true;  // permite responder a eventos de teclado
            AllowDrop = true;  // permite arrastar e soltar

            Load += new EventHandler(On_FormLoad);
            FormClosed += new FormClosedEventHandler(On_FormClosed);
            KeyDown += new KeyEventHandler(On_FormKeyDown);
            MouseWheel += new MouseEventHandler(On_MouseWheel);
            MouseMove += new MouseEventHandler(On_MouseMove);
            DragOver += new DragEventHandler(On_FormDragOver);
            DragDrop += new DragEventHandler(On_FormDragDrop);

            // handler para lista de opções de velocidade            
            void ToolStripMenuItemClick(object sender, EventArgs e)
            {
                if ((sender as ToolStripMenuItem).Text != String.Empty)
                    _mp.SetRate(float.Parse((sender as ToolStripMenuItem).Text));
            }

            // atualiza itens na tsRate
            String[] rateList = { "0,5", "1,0", "1,5", "2,0" };
            tsRate.DropDownItems.Clear();
            for (int i = 0; i < rateList.Length; i++)
            {
                tsRate.DropDownItems.Add(rateList[i]); //
                tsRate.DropDownItems[i].Click += new EventHandler(ToolStripMenuItemClick); // associa handler para click
            }

            // menus de contexto
            _playlistContextMenuStrip = new ContextMenuStrip();
            _playlistContextMenuStrip.Items.Add(new ToolStripMenuItem("Apagar item"));
            _playlistContextMenuStrip.Items.Add(new ToolStripMenuItem("Reproduzir item"));

            // formatação da caixa de texto
            rtTextBox.Font = new Font(rtTextBox.Font.FontFamily, 12);

            // handler para lista de opções de _step            
            void hStepComboBoxClick(object sender, EventArgs e)
            {
                if ((sender as ToolStripComboBox).SelectedIndex >= 0)
                    this._step = int.Parse((sender as ToolStripComboBox).Text);
            }

            String[] stepList = { "30", "500", "1000", "2000", "3000" }; // valores em milisegundos
            cbStepList.Items.Clear();
            for (int i = 0; i < stepList.Length; i++)
            {
                cbStepList.Items.Add(stepList[i]); //
            }
            cbStepList.SelectedIndex = 4;// padrão 3000
            cbStepList.SelectedIndexChanged += new EventHandler(hStepComboBoxClick); // associa handler para click

        }

        #endregion

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
                rtTextBox.SaveFile(dlg.FileName);
            }
        }

        public void insertMediaTimetoText()
        {
            if (MediaPlayerNotOK()) return;

            var ctime = _mp.Time; // posição da stream em milisegundos e converte parA Timespan
            TimeSpan ts = TimeSpan.FromMilliseconds(ctime > 0 & ctime < _mp.Length ? ctime : 0);

            string strText = "Posição " + ts.ToString(@"hh\:mm\:ss") + " - (" +
                                  (_mp.Time >= 0 ? _mp.Time.ToString() : "0") + ")\r\n";

            rtTextBox.AppendText(strText); // insere texto na posição atual do cursor

        }

        public void insertStrInText(string str) // para inserir referência de falantes e outros textos
        {
            if (MediaPlayerNotOK()) return;

            rtTextBox.AppendText(str); // insere texto na posição atual do cursor
        }

        public void GoToMediaTimefromLineText()
        {
            if (MediaPlayerNotOK()) return;
            if (rtTextBox.Lines.Length <= 0) return;

            int index = rtTextBox.SelectionStart; // obtém a posição do caracter
            int line = rtTextBox.GetLineFromCharIndex(index); // obtém o indice da linha
            var sLine = rtTextBox.Lines[line]; // obtém o texto da linha
            int startIndex = sLine.LastIndexOf('(') + 1;
            int endIndex = sLine.LastIndexOf(')');

            // extrai valor em milisegundos da linha
            var iTime = sLine.Substring(startIndex, endIndex - startIndex);

            _mp.Time = Convert.ToInt64(iTime); // posiciona a mídia no instante desejado

        }

        public void PasteFromClipboard(object sender, EventArgs e)
        {
            DataFormats.Format format1 = DataFormats.GetFormat(DataFormats.Bitmap);
            DataFormats.Format format2 = DataFormats.GetFormat(DataFormats.Text);

            //var cbformat = Clipboard.GetImage;
            //var cbtxt = Clipboard.GetText();

            // After verifying that the data can be pasted, paste
            if ((this.rtTextBox.CanPaste(format1)) || (this.rtTextBox.CanPaste(format2)))
            {
                rtTextBox.Paste();
            }
        }

        public void CopytoClipboard(object sender, EventArgs e)
        {
            rtTextBox.Copy();
        }

        public void CuttoClipboard(object sender, EventArgs e)
        {
            rtTextBox.Cut();
        }

        private void AtualizatsPlaylist()
        {
            // manipulador do evento click a ser ligado no novo item
            void ToolStripMenuItemClick(object sender, MouseEventArgs e)
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        OpenMediaFile((sender as ToolStripMenuItem).Text);
                        break;

                    case MouseButtons.Right:
                        // exclui item e depois atualiza novamente a tsplaylist
                        var idx = _playlist.IndexOf((sender as ToolStripMenuItem).Text);

                        // não remover a mídia em reprodução atual!                        
                        if (new Uri(_mp.Media.Mrl) ==
                            new Uri(tsPlaylist.DropDownItems[idx].Text))
                            break;

                        _playlist.RemoveAt(idx);

                        AtualizatsPlaylist();
                        break;
                }
            }

            // atualiza itens na tsPlayList (tsPlaylist é o componente do Form)
            tsPlaylist.DropDownItems.Clear();
            tsPlaylist.ToolTipText = "Lista de reprodução para acesso rápido";
            foreach (var item in _playlist)
            {
                int idx = _playlist.IndexOf(item);
                tsPlaylist.DropDownItems.Add(_playlist[idx]);
                tsPlaylist.DropDownItems[idx].MouseUp += new MouseEventHandler(ToolStripMenuItemClick); // associa handler para click
                tsPlaylist.DropDownItems[idx].ToolTipText = "Para deletar clique com botão direito do mouse";

                if (new Uri(_mp.Media.Mrl) == new Uri(tsPlaylist.DropDownItems[idx].Text))
                    ((ToolStripMenuItem)tsPlaylist.DropDownItems[idx]).Checked = true;
            }
        }

        private void OpenMediaFile(String filePath) // abre para reprodução de UM arquivo de mídia
        {
            try
            {
                //var media = new Media(_libVLC, new Uri(filePath));
                var media = new Media(_libVLC, filePath, FromType.FromPath);

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
            AtualizatsPlaylist();
            if (!rtTextBox.Focused) rtTextBox.Focus(); // não está funcionando para vídeo!
        }

        private void PlayPause(object sender, EventArgs e)
        {
            // houve necessidade de implementar com os argumentos sender e "e" porque essa função foi ligada
            // ao evendo do botão Play/Pause de forma dinâmica, na inicialização do Form

            if (!_mp.IsPlaying)
            {
                _mp.Play(); // coloca em estado de reprodução para possibilitar o seek e/ou pause
            }

            // Todo: quando chega ao final do fluxo (State == Ended), só depois que clica em Stop ele é capaz de reproduzir de novo
            // corrigir!
            _mp.SetPause(_mp.IsPlaying);
        }

        private void Stop(object sender, EventArgs e)
        {
            _mp.Stop();
        }

        private void customSetPos(long pos)
        {
            if (_mp.State == VLCState.Stopped)
            {
                _mp.Play(); //otherwise not seekable for some silly reason                
                _mp.Pause(); // não está funcionando
                _mp.Time = pos;
            }
            else
            {
                _mp.Time = pos;
            }
        }

        private void Backward(object sender, EventArgs e) // pagedown
        {
            customSetPos(_mp.Time - _step);
        }

        private void Forward(object sender, EventArgs e) // pageup
        {
            customSetPos(_mp.Time + _step);
        }

        private void GetPicture()
        {
            // const char *image_path="/home/vinay/Documents/snap.png";
            // int result = libvlc_video_take_snapshot(mp, 0, image_path, 0, 0);
            // parece que "file:" está atrapalhando...
            if (MediaPlayerNotOK()) return;

            //obtém dimensões informações sobre o frame
            var vtrackidx = _mp.VideoTrack; // obtém índice da stream do fluxo de vídeo
            if (vtrackidx < 0) return; // se não houver stream de vídeo, retorna

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
            {
                // pega novamente o arquivo e coloca na clipboard?
                Image img = Image.FromFile(image_path);
                Clipboard.SetImage(img);
                if (rtTextBox.Focused) rtTextBox.Paste();

                MessageBox.Show("Arquivo " + Path.GetFileNameWithoutExtension(_path)
                    + " salvo com sucesso na pasta " + Path.GetDirectoryName(_path));
            }
        }

        private bool MediaPlayerNotOK()
        {
            return ((_mp == null) || (_mp.Media == null));
        }

        private bool SourceFileNotOK(string sourcefile)
        {
            return !File.Exists(sourcefile);
        }

        private void extractToWav(string sourcefile) // experimental
        {

            if (SourceFileNotOK(sourcefile)) return;

            string destinationfile = Path.GetDirectoryName(sourcefile);
            destinationfile += "\\" + Path.GetFileNameWithoutExtension(sourcefile);

            _libVLC.Log += (send, m) => Console.WriteLine($"[{m.Level}] {m.Module}:{m.Message}");

            Media media = new Media(_libVLC, sourcefile, FromType.FromPath);

            media.AddOption(":no-sout-video");
            media.AddOption(":sout-audio");
            media.AddOption(":sout-keep");
            media.AddOption(":sout=#transcode{acodec=s16l,ab=128,channels=1,samplerate=24000}:" +
                            "std{access=file,mux=wav,dst=" + destinationfile + ".wav" + "}");

            MediaPlayer mPlayer = new MediaPlayer(media) { EnableHardwareDecoding = true };

            mPlayer.Play(media); // salva arquivo WAV na mesma pasta da origem
            media.Dispose();

            MessageBox.Show("Arquivo WAV salvo em: " + destinationfile + ".wav");

        }

        #endregion

        #region handlers de menu e botõesd da toolbar
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
                }
            }
        }

        private void extrairWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var filePath = String.Empty;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName; 
                    extractToWav(filePath); // extrai a stream de áudio para WAV 
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
            int CalcProgressBarRelativeMouse(object sender)
            {
                // Get mouse position(x) minus the width of the progressbar (so beginning of the progressbar is mousepos = 0 //
                float absoluteMouse = (PointToClient(MousePosition).X - (sender as ToolStripProgressBar).Bounds.X);
                // Calculate the factor for converting the position (progbarWidth/100) //
                float calcFactor = (sender as ToolStripProgressBar).Width / (float)(sender as ToolStripProgressBar).Maximum;
                // In the end convert the absolute mouse value to a relative mouse value by dividing the absolute mouse by the calcfactor //
                float relativeMouse = absoluteMouse / calcFactor;

                return Convert.ToInt32(relativeMouse);
            }

            int pos = CalcProgressBarRelativeMouse(sender); // retorna um inteiro [0:100] com a posição relativa do mouse sobre a barra
            if (pos >= 0)
                _mp.Position = (float)pos / 100;
        }

        private void progressBar1_MouseMove(object sender, MouseEventArgs e)
        {
            if (MediaPlayerNotOK()) return;

            int CalcProgressBarRelativeMouse(object sender)
            {
                // Get mouse position(x) minus the width of the progressbar (so beginning of the progressbar is mousepos = 0 //
                float absoluteMouse = (PointToClient(MousePosition).X - (sender as ToolStripProgressBar).Bounds.X);
                // Calculate the factor for converting the position (progbarWidth/100) //
                float calcFactor = (float)(sender as ToolStripProgressBar).Width / (sender as ToolStripProgressBar).Maximum;
                // In the end convert the absolute mouse value to a relative mouse value by dividing the absolute mouse by the calcfactor //
                float relativeMouse = absoluteMouse / calcFactor;

                return Convert.ToInt32(relativeMouse);
            }

            // retorna um inteiro [0:100] com a posição relativa do mouse sobre a progressbar
            int pos = CalcProgressBarRelativeMouse(sender);
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
                        rtTextBox.LoadFile(filePath);
                }
            }
        }

        private void guiaDeTeclasDeAtalhoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String[] str =
            {
                "Funções:\n",
"Playlist: Botão esquerdo seleciona e reproduz / botão direito apaga item da playlist\n",
"Teclado:\n",
"ESC: Parar\n",
"F1: Play / Pausa com retorno de step milisegundos\n",
"F2: Play / Pausa\n",
"F3:\n",
"F4:\n",
"F5:\n",
"F6: Inserir snapshot do vídeo no texto\n",
"F7: Inserir instante de tempo atual no texto\n",
"F8: Ir para instante de tempo sob o cursor do mouse\n",
"F9:\n",
"F10:\n",
"F11:\n",
"F12: Salvar texto no disco\n",
"PAGEUP: Avançar de step milisegundos\n",
"PAGEDOWN: Retroceder de step milisegundos\n",
"CTRL + Seta direita: Avançar step milisegundos\n",
"CTRL + Seta esquerda: Retroceder step milisegundos\n",
};
            MessageBox.Show(String.Join("", str));
        }

        #endregion

        #region manipuladores de eventos de componentes internos
        private void On_EndReached(object sender, EventArgs e)
        {
            // passar para a próxima mídia da playlist?
        }

        private void On_Stopped(object sender, EventArgs e)
        {
            // fazer alguma coisa?            
        }

        private void On_TimerTick(object sender, EventArgs e)
        {
            //
        }

        delegate void OnMediaPlayerPositionChangedDelegate(String txt, int val);
        private void UpdateCaptionAndProgressBarDelegateMethod(string message, int val) // Cria método para o delegate declarado.
        {
            this.Text = message; // atualiza caption do Form
            progressBar1.Value = val; // atualiza barra de progresso
        }

        private void On_PositionChanged(object sender, EventArgs e)
        {
            if (MediaPlayerNotOK()) return;

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
            //progressBar1.Value = val;

            // atualiza caption
            string text = _state.ToString() + " @rate " + _rate.ToString() +
                        " - " + ts.ToString(@"hh\:mm\:ss") +
                        " / " + tsTotal.ToString(@"hh\:mm\:ss");

            // Atualiza caption do form principal e barra de progresso através de delegate (contorna problemas de thread).
            this.BeginInvoke(new OnMediaPlayerPositionChangedDelegate(UpdateCaptionAndProgressBarDelegateMethod), text, val); // chama método assincronicamente

        }

        private void On_MediaPlayerTimerChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            // corrigir problemas... funcionando com o componente Timer
        }

        private void On_MediaPlayerForward(object sender, EventArgs e)
        {
            // implementar ações
        }

        private void On_MediaPlayerBackward(object sender, EventArgs e)
        {
            //implementar ações
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
            //string fpath = Directory.GetCurrentDirectory() + "/media/teste.mp3";
            //_playlist.Add(fpath);
            //OpenMediaFile(fpath);

            OpenMediaFile("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
        }

        private void On_MouseWheel(object sender, MouseEventArgs e)
        {
            // verifica se mouse está fora do richtext, para avançar ou retroceder com a roda do mouse

            if (e.Delta != 0)
                customSetPos(_mp.Time + _step * e.Delta / (Math.Abs(e.Delta)));
        }

        private void On_MouseMove(object sender, MouseEventArgs e)
        {
            // verifica sob qual componente o mouse está posicionado e tomar ações
        }

        private void On_FormDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void On_FormDragDrop(object sender, DragEventArgs e)
        {
            // soltar fora do richtext!
            string[] arquivos = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (arquivos != null && arquivos.Any())
            {
                MessageBox.Show(string.Join("\n", arquivos));
                _playlist.AddRange(arquivos);
                AtualizatsPlaylist();
            }
        }

        private void On_FormKeyDown(object sender, KeyEventArgs e)
        {
            // função para teste do pressionamento da tecla CTRL
            static bool IsControlDown()
            {
                return (Control.ModifierKeys & Keys.Control) != 0;
            }

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
                        if (!rtTextBox.Focused)
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
                        // se estiver reproduzindo, volta _step milissegundos e aguarda para reproduzir
                        if (_mp.IsPlaying)
                            customSetPos(_mp.Time - _step);
                        PlayPause(null, null);

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
                        GoToMediaTimefromLineText();
                        break;
                    }
                case Keys.F12: // salvar texto
                    {
                        Save2TXT();
                        break;
                    }
                case Keys.PageDown: // retorna xx milisegundos (to do: implementar avançar frame a frame!)
                    {
                        if (rtTextBox.Focused)
                        {
                            e.SuppressKeyPress = true;
                            Backward(null, null);
                        }
                        break;
                    }
                case Keys.PageUp: // avança xx milisegundos
                    {
                        if (rtTextBox.Focused)
                        {
                            e.SuppressKeyPress = true;
                            Forward(null, null);
                        }
                        break;
                    }
                case Keys.Right: // CTRl + seta direita
                    {
                        if (IsControlDown() && rtTextBox.Focused)
                        {
                            e.SuppressKeyPress = true;
                            Forward(null, null);
                        }
                        break;
                    }
                case Keys.Left: // CTRl + seta esqueda
                    {
                        if (IsControlDown() && rtTextBox.Focused)
                        {
                            e.SuppressKeyPress = true;
                            Backward(null, null);
                        }
                        break;
                    }
                    //case Keys.D1: // CTRl + seta esqueda
                    //    {
                    //        if (IsControlDown() && rtTextBox.Focused)
                    //        {
                    //            e.SuppressKeyPress = true;
                    //            insertStrInText("M1");
                    //        }
                    //        break;
                    //    }
                    //case Keys.D2: // CTRl + seta esqueda
                    //    {
                    //        if (IsControlDown() && rtTextBox.Focused)
                    //        {
                    //            e.SuppressKeyPress = true;
                    //            insertStrInText("M2");
                    //        }
                    //        break;
                    //    }
                    //case Keys.D3: // CTRl + seta esqueda
                    //    {
                    //        if (IsControlDown() && rtTextBox.Focused)
                    //        {
                    //            e.SuppressKeyPress = true;
                    //            insertStrInText("M3");
                    //        }
                    //        break;
                    //    }
            }
        }

        #endregion

        #region experimental

        /*
         * para acessar os comandos disponíveis, digitar no cmd? vlc –-help  
         */

        private byte[] getThumbnail(Media media, int i_width, int i_height) // experimental
        {
            media.AddOption(":no-audio");
            media.AddOption(":no-spu");
            media.AddOption(":no-osd");
            media.AddOption(":input-fast-seek");

            return getThumbnail(media, i_width, i_height);
        }

        // to do: atualizar o label dinamicamente. quando estiver sob a progressbar e quando estiver fora
        private void progressBar1_MouseEnter(object sender, EventArgs e)
        {
            _mouseInProgressBar = true;
        }

        private void progressBar1_MouseLeave(object sender, EventArgs e)
        {
            _mouseInProgressBar = false;
        }

        #endregion


    }

}
