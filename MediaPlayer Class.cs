using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using System.Threading;

namespace Net.Media
{
    //定義替代變數
    using libvlc_media_t = System.IntPtr;
    using libvlc_media_player_t = System.IntPtr;
    using libvlc_instance_t = System.IntPtr;

    class MediaPlayer
    {
        #region 全域性變數
        //陣列轉換為指標
        internal struct PointerToArrayOfPointerHelper
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public IntPtr[] pointers;
        }

        //vlc庫啟動引數配置
        private static string pluginPath = System.Environment.CurrentDirectory + "\\plugins\\";
        private static string plugin_arg = "--plugin-path=" + pluginPath;
        //用於播放節目時，轉錄節目
        //private static string program_arg = "--sout=#duplicate{dst=std{access=file,mux=ts,dst=d:/test.ts}}";
        private static string[] arguments = { "-I", "dummy", "--ignore-config", "--video-title", plugin_arg };//, program_arg };

        #region 結構體
        public struct libvlc_media_stats_t
        {
            /* Input */
            public int i_read_bytes;
            public float f_input_bitrate;

            /* Demux */
            public int i_demux_read_bytes;
            public float f_demux_bitrate;
            public int i_demux_corrupted;
            public int i_demux_discontinuity;

            /* Decoders */
            public int i_decoded_video;
            public int i_decoded_audio;

            /* Video Output */
            public int i_displayed_pictures;
            public int i_lost_pictures;

            /* Audio output */
            public int i_played_abuffers;
            public int i_lost_abuffers;

            /* Stream output */
            public int i_sent_packets;
            public int i_sent_bytes;
            public float f_send_bitrate;
        }
        #endregion

        #endregion

        #region 公開函式
        /// <summary>
        /// 建立VLC播放資源索引
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static libvlc_instance_t Create_Media_Instance()
        {
            libvlc_instance_t libvlc_instance = IntPtr.Zero;
            IntPtr argvPtr = IntPtr.Zero;

            try
            {
                if (arguments.Length == 0 ||
                    arguments == null)
                {
                    return IntPtr.Zero;
                }

                //將string陣列轉換為指標
                argvPtr = StrToIntPtr(arguments);
                if (argvPtr == null || argvPtr == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                //設定啟動引數
                libvlc_instance = SafeNativeMethods.libvlc_new(arguments.Length, argvPtr);
                if (libvlc_instance == null || libvlc_instance == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                return libvlc_instance;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 釋放VLC播放資源索引
        /// </summary>
        /// <param name="libvlc_instance">VLC 全域性變數</param>
        public static void Release_Media_Instance(libvlc_instance_t libvlc_instance)
        {
            try
            {
                if (libvlc_instance != IntPtr.Zero ||
                    libvlc_instance != null)
                {
                    SafeNativeMethods.libvlc_release(libvlc_instance);
                }

                libvlc_instance = IntPtr.Zero;
            }
            catch (Exception)
            {
                libvlc_instance = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 建立VLC播放器
        /// </summary>
        /// <param name="libvlc_instance">VLC 全域性變數</param>
        /// <param name="handle">VLC MediaPlayer需要繫結顯示的窗體控制程式碼</param>
        /// <returns></returns>
        public static libvlc_media_player_t Create_MediaPlayer(libvlc_instance_t libvlc_instance, IntPtr handle)
        {
            libvlc_media_player_t libvlc_media_player = IntPtr.Zero;

            try
            {
                if (libvlc_instance == IntPtr.Zero ||
                    libvlc_instance == null ||
                    handle == IntPtr.Zero ||
                    handle == null)
                {
                    return IntPtr.Zero;
                }

                //建立播放器
                libvlc_media_player = SafeNativeMethods.libvlc_media_player_new(libvlc_instance);
                if (libvlc_media_player == null || libvlc_media_player == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                //設定播放視窗            
                SafeNativeMethods.libvlc_media_player_set_hwnd(libvlc_media_player, (int)handle);

                return libvlc_media_player;
            }
            catch
            {
                SafeNativeMethods.libvlc_media_player_release(libvlc_media_player);

                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 釋放媒體播放器
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        public static void Release_MediaPlayer(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player != IntPtr.Zero ||
                    libvlc_media_player != null)
                {
                    if (SafeNativeMethods.libvlc_media_player_is_playing(libvlc_media_player))
                    {
                        SafeNativeMethods.libvlc_media_player_stop(libvlc_media_player);
                    }

                    SafeNativeMethods.libvlc_media_player_release(libvlc_media_player);
                }

                libvlc_media_player = IntPtr.Zero;
            }
            catch (Exception)
            {
                libvlc_media_player = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 播放網路媒體
        /// </summary>
        /// <param name="libvlc_instance">VLC 全域性變數</param>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <param name="url">網路視訊URL，支援http、rtp、udp等格式的URL播放</param>
        /// <returns></returns>
        public static bool NetWork_Media_Play(libvlc_instance_t libvlc_instance, libvlc_media_player_t libvlc_media_player, string url)
        {
            IntPtr pMrl = IntPtr.Zero;
            libvlc_media_t libvlc_media = IntPtr.Zero;

            try
            {
                if (url == null ||
                    libvlc_instance == IntPtr.Zero ||
                    libvlc_instance == null ||
                    libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                pMrl = StrToIntPtr(url);
                if (pMrl == null || pMrl == IntPtr.Zero)
                {
                    return false;
                }

                //播放網路檔案
                libvlc_media = SafeNativeMethods.libvlc_media_new_location(libvlc_instance, pMrl);

                if (libvlc_media == null || libvlc_media == IntPtr.Zero)
                {
                    return false;
                }

                //將Media繫結到播放器上
                SafeNativeMethods.libvlc_media_player_set_media(libvlc_media_player, libvlc_media);

                //釋放libvlc_media資源
                SafeNativeMethods.libvlc_media_release(libvlc_media);
                libvlc_media = IntPtr.Zero;

                if (0 != SafeNativeMethods.libvlc_media_player_play(libvlc_media_player))
                {
                    return false;
                }

                //休眠指定時間
                Thread.Sleep(500);

                return true;
            }
            catch (Exception)
            {
                //釋放libvlc_media資源
                if (libvlc_media != IntPtr.Zero)
                {
                    SafeNativeMethods.libvlc_media_release(libvlc_media);
                }
                libvlc_media = IntPtr.Zero;

                return false;
            }
        }

        /// <summary>
        /// 暫停或恢復視訊
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <returns></returns>
        public static bool MediaPlayer_Pause(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_can_pause(libvlc_media_player))
                {
                    SafeNativeMethods.libvlc_media_player_pause(libvlc_media_player);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <returns></returns>
        public static bool MediaPlayer_Stop(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                SafeNativeMethods.libvlc_media_player_stop(libvlc_media_player);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 快進
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <returns></returns>
        public static bool MediaPlayer_Forward(libvlc_media_player_t libvlc_media_player)
        {
            double time = 0;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_is_seekable(libvlc_media_player))
                {
                    time = SafeNativeMethods.libvlc_media_player_get_time(libvlc_media_player) / 1000.0;
                    if (time == -1)
                    {
                        return false;
                    }

                    SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)((time + 30) * 1000));

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 快退
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <returns></returns>
        public static bool MediaPlayer_Back(libvlc_media_player_t libvlc_media_player)
        {
            double time = 0;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_is_seekable(libvlc_media_player))
                {
                    time = SafeNativeMethods.libvlc_media_player_get_time(libvlc_media_player) / 1000.0;
                    if (time == -1)
                    {
                        return false;
                    }

                    if (time - 30 < 0)
                    {
                        SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)(1 * 1000));
                    }
                    else
                    {
                        SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)((time - 30) * 1000));
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// VLC MediaPlayer是否在播放
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <returns></returns>
        public static bool MediaPlayer_IsPlaying(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                return SafeNativeMethods.libvlc_media_player_is_playing(libvlc_media_player);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 錄製快照
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer變數</param>
        /// <param name="path">快照要存放的路徑</param>
        /// <param name="name">快照儲存的檔名稱</param>
        /// <returns></returns>
        public static bool TakeSnapShot(libvlc_media_player_t libvlc_media_player, string path, string name)
        {
            try
            {
                string snap_shot_path = null;

                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                snap_shot_path = path + "\\" + name;

                if (0 == SafeNativeMethods.libvlc_video_take_snapshot(libvlc_media_player, 0, snap_shot_path.ToCharArray(), 0, 0))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 獲取資訊
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <returns></returns>
        public static bool GetMedia(libvlc_media_player_t libvlc_media_player)
        {
            libvlc_media_t media = IntPtr.Zero;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                media = SafeNativeMethods.libvlc_media_player_get_media(libvlc_media_player);
                if (media == IntPtr.Zero || media == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 獲取已經顯示的圖片數
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <returns></returns>
        public static int GetDisplayedPictures(libvlc_media_player_t libvlc_media_player)
        {
            libvlc_media_t media = IntPtr.Zero;
            libvlc_media_stats_t media_stats = new libvlc_media_stats_t();
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return 0;
                }

                media = SafeNativeMethods.libvlc_media_player_get_media(libvlc_media_player);
                if (media == IntPtr.Zero || media == null)
                {
                    return 0;
                }

                if (1 == SafeNativeMethods.libvlc_media_get_stats(media, ref media_stats))
                {
                    return media_stats.i_displayed_pictures;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 設定全屏
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <param name="isFullScreen"></param>
        public static bool SetFullScreen(libvlc_media_player_t libvlc_media_player, int isFullScreen)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                SafeNativeMethods.libvlc_set_fullscreen(libvlc_media_player, isFullScreen);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region 私有函式
        //將string []轉換為IntPtr
        public static IntPtr StrToIntPtr(string[] args)
        {
            try
            {
                IntPtr ip_args = IntPtr.Zero;

                PointerToArrayOfPointerHelper argv = new PointerToArrayOfPointerHelper();
                argv.pointers = new IntPtr[11];

                for (int i = 0; i < args.Length; i++)
                {
                    argv.pointers[i] = Marshal.StringToHGlobalAnsi(args[i]);
                }

                int size = Marshal.SizeOf(typeof(PointerToArrayOfPointerHelper));
                ip_args = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(argv, ip_args, false);

                return ip_args;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        //將string轉換為IntPtr
        private static IntPtr StrToIntPtr(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return IntPtr.Zero;
                }

                IntPtr pMrl = IntPtr.Zero;
                byte[] bytes = Encoding.UTF8.GetBytes(url);

                pMrl = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, pMrl, bytes.Length);
                Marshal.WriteByte(pMrl, bytes.Length, 0);

                return pMrl;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }
        #endregion

        #region 匯入庫函式
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            // 建立一個libvlc例項，它是引用計數的
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_instance_t libvlc_new(int argc, IntPtr argv);

            // 釋放libvlc例項
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_release(libvlc_instance_t libvlc_instance);

            //獲取libvlc的版本
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern String libvlc_get_version();

            //從視訊來源(例如http、rtsp)構建一個libvlc_meida
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_new_location(libvlc_instance_t libvlc_instance, IntPtr path);

            //從本地檔案路徑構建一個libvlc_media
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_new_path(libvlc_instance_t libvlc_instance, IntPtr path);

            //釋放libvlc_media
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_release(libvlc_media_t libvlc_media_inst);

            // 建立一個空的播放器
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_player_t libvlc_media_player_new(libvlc_instance_t libvlc_instance);

            //從libvlc_media構建播放器
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_player_t libvlc_media_player_new_from_media(libvlc_media_t libvlc_media);

            //釋放播放器資源
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_release(libvlc_media_player_t libvlc_mediaplayer);

            // 將視訊(libvlc_media)繫結到播放器上
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_media(libvlc_media_player_t libvlc_media_player, libvlc_media_t libvlc_media);

            // 設定影象輸出的視窗
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_hwnd(libvlc_media_player_t libvlc_mediaplayer, Int32 drawable);

            //播放器播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_player_play(libvlc_media_player_t libvlc_mediaplayer);

            //播放器暫停
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_pause(libvlc_media_player_t libvlc_mediaplayer);

            //播放器停止
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_stop(libvlc_media_player_t libvlc_mediaplayer);

            // 解析視訊資源的媒體資訊(如時長等)
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_parse(libvlc_media_t libvlc_media);

            // 返回視訊的時長(必須先呼叫libvlc_media_parse之後，該函式才會生效)
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int64 libvlc_media_get_duration(libvlc_media_t libvlc_media);

            // 當前播放時間
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int64 libvlc_media_player_get_time(libvlc_media_player_t libvlc_mediaplayer);

            // 設定播放時間
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_time(libvlc_media_player_t libvlc_mediaplayer, Int64 time);

            // 獲取音量
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_audio_get_volume(libvlc_media_player_t libvlc_media_player);

            //設定音量
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_audio_set_volume(libvlc_media_player_t libvlc_media_player, int volume);

            // 設定全屏
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_set_fullscreen(libvlc_media_player_t libvlc_media_player, int isFullScreen);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_get_fullscreen(libvlc_media_player_t libvlc_media_player);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_toggle_fullscreen(libvlc_media_player_t libvlc_media_player);

            //判斷播放時是否在播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_is_playing(libvlc_media_player_t libvlc_media_player);

            //判斷播放時是否能夠Seek
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_is_seekable(libvlc_media_player_t libvlc_media_player);

            //判斷播放時是否能夠Pause
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_can_pause(libvlc_media_player_t libvlc_media_player);

            //判斷播放器是否可以播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_player_will_play(libvlc_media_player_t libvlc_media_player);

            //進行快照
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_video_take_snapshot(libvlc_media_player_t libvlc_media_player, int num, char[] filepath, int i_width, int i_height);

            //獲取Media資訊
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_player_get_media(libvlc_media_player_t libvlc_media_player);

            //獲取媒體資訊
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_get_stats(libvlc_media_t libvlc_media, ref libvlc_media_stats_t lib_vlc_media_stats);
        }
        #endregion
    }
}
