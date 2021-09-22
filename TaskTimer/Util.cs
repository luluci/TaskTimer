﻿using System;
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
        public const int countDiv = 10;
#else
        public const int countDiv = 60;
#endif

        public static readonly string reWord = @"[\w\+\-\.\@\:\+\*\(\)_&@・（）、。,/]+";

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
    }
}