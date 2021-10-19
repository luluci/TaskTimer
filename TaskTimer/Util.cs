using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;

namespace TaskTimer
{
    static class Util
    {
        // 時間カウント分解能
#if DEBUG
        public const int countDiv = 1;
#else
        public const int countDiv = 60;
#endif

        public static readonly string rootDirPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

        public static readonly DateTime CurrentDate = DateTime.Now;     // 今日の日時。起動時の日時に依存して固定とする
        public static DateTime TargetDate = DateTime.Now;               // カレンダーで指定する表示対象の日時

        public static readonly string reWord = @"[\w\+\-\.\@\:\+\*\(\)_ !\?&@・（）、。,/]+";

        public static readonly Regex reTimeWithColon = new Regex(@"^(\d+):(\d+)$", RegexOptions.Compiled);
        public static readonly Regex reTimeWithoutColon = new Regex(@"(?:^(?<min>\d{1,2})$|^(?<hr>\d+)(?<min>\d\d)$)", RegexOptions.Compiled);

        static public bool CheckTargetDateIsToday()
        {
            return TargetDate.Date.Equals(CurrentDate.Date);
        }
        static public bool CheckTargetDateIsNotPast()
        {
            // ターゲット >= 現在
            return TargetDate.Date.CompareTo(CurrentDate.Date) >= 0;
        }

        static public bool AskFileLock(string path, string title)
        {
            bool result = false;
            if (IsFileLocked(path))
            {
                // ファイルがロックされていたら解放を促す
                bool lockchecked;
                do
                {
                    var msgresult = System.Windows.MessageBox.Show("ファイルが開かれています。\r\n閉じたら[OK], 保存しないなら[Cancel]", title, System.Windows.MessageBoxButton.OKCancel);
                    if (msgresult == System.Windows.MessageBoxResult.OK)
                    {
                        // OKが選択されたら再度ロックチェック
                        if (IsFileLocked(path))
                        {
                            // まだロックされていたらループ
                            lockchecked = false;
                        }
                        else
                        {
                            result = true;
                            lockchecked = true;
                        }
                    }
                    else
                    {
                        // キャンセルされたら終了
                        result = false;
                        lockchecked = true;
                    }
                } while (!lockchecked);
            }
            else
            {
                // ファイルがロックされていなければ正常終了
                result = true;
            }
            return result;
        }

        static public bool CheckFileOpen(string path)
        {
            if (IsFileLocked(path))
            {
                // ファイルを開けないなら確認
                var result = System.Windows.MessageBox.Show("ファイルが開かれています。\r\n閉じたら[OK], 保存しないなら[Cancel]", "!?", System.Windows.MessageBoxButton.OKCancel);
                
                if (result == System.Windows.MessageBoxResult.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // ファイルを開けるならOK
                return true;
            }
        }

        static public bool IsFileLocked(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                // 書き込みモードでファイルを開けるか確認
                using (FileStream fp = File.Open(path, FileMode.Open, FileAccess.Write))
                {
                    // 開ける
                    return false;
                }
            }
            catch
            {
                // 開けない
                return true;
            }
        }

        static public string Sec2Time(int sec)
        {
            var span = new TimeSpan(0, 0, sec);
            return span.ToString(@"hh\:mm\:ss");
        }

        static public string Min2Time(int min)
        {
            int hr = min / 60;
            int mm = min % 60;
            return $"{hr:00}:{mm:00}";
        }

        static public int GetRegGroup2Min(System.Text.RegularExpressions.Group group)
        {
            string buff = group.ToString();
            if (buff.Length == 0)
            {
                return 0;
            }
            else
            {
                try
                {
                    return int.Parse(buff);
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
