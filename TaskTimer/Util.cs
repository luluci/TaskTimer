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
        public static readonly string reWord = @"[\w\+\-\.\@\:\+\*\(\)_&@・（）、。,/]+";


        static public bool IsFileLocked(string path)
        {
            try
            {
                // 書き込みモードでファイルを開けるか確認
                using (FileStream fp = File.Open(path, FileMode.Open, FileAccess.Write))
                {
                    // 開ける
                    return true;
                }
            }
            catch
            {
                // 開けない
                return false;
            }
        }
    }
}
