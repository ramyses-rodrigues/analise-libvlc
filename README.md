# analise-libvlc
Projeto criado para trabalhos com transcrição e análise de conteúdo de materiais audiovisuais
<p>Necessário instalar no Visual Studio, via Nuget: </p>
<p>1) LibVCLSharp (https://github.com/videolan/libvlcsharp)</p>
<p>2) VideoLAN.LibVLCSharp.Windows </p>
<p> Em implementação: instalar FFMPEG.net</p>
<p><p>
A compilação automaticamente gerará a pasta LibVLC no diretório de saída, contendo os binários (.dll) do VLC.
<p>Funções:</p>
Playlist: Botão esquerdo seleciona e reproduz / botão direito apaga item da playlist
<p>Teclado: </p>
<p>ESC: Parar </p>
<p>F1: Reprodução/Pausa com retorno de step milisegundos ou Reproduz seleção </p>
<p>F2: Reprodução/Pausa </p>
<p>F3:  </p>
<p>F4:  </p>
<p>F5:  </p>
<p>F6: Inserir snapshot do vídeo no texto </p>
<p>F7: Inserir instante de tempo atual no texto </p>
<p>F8: Ir para instante de tempo (ou seleção)  na linha sob o cursor do texto </p>
<p>F9:  </p>
<p>F10: </p>
<p>F11: </p>
<p>F12: Salvar texto no disco </p>
<p>PAGEUP: Avançar de step milisegundos </p>
<p>PAGEDOWN: Retroceder de step milisegundos </p>
<p>CTRL + SETA esq: Retroceder de step milisegundos </p>
<p>CTRL + SETA dir: Avançar de step milisegundos </p>

Em implementação:
<p>SHIFT + SETA esq: Selecionar trecho (intervalo de tempo) para trás </p>
<p>SHIFT + SETA dir: Selecionar trecho (intervalo de tempo) para frente </p>
<p> Funções do FFMPEG: conversão de formato; extração de streams; captura de frames, etc </p>
