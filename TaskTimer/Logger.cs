using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace TaskTimer
{
    class Logger
    {
        private string rootDir;
        private string logDir;
        private string baseFileName;
        private string daykey;
        private string logFile;
        private string logFileTemp;

        public bool reqRestore;

        public List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)> Logs;
        public Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item), int> RestoreLogs;

        public Logger()
        {
            rootDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            logDir = rootDir + @"\log";
            baseFileName = "log";
            // 本日の日付取得
            // ログファイルキーとする
            DateTime dt = DateTime.Now;
            daykey = dt.ToString("yyyyMMdd");
            // ログファイル名作成
            logFile = $@"{logDir}\{baseFileName}.{daykey}.txt";
            logFileTemp = $@"{logDir}\{baseFileName}.{daykey}.tmp";
            // ログからの復旧有無
            reqRestore = false;
        }

        public void Load()
        {
            RestoreLogs = new Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item), int>();
            // 同じ日付のログがあったらツールが途中終了したものとして、続きからカウントできるようにする。
            // 設定ファイルからロード
            // フォルダチェック
            if (!Directory.Exists(logDir))
            {
                // 存在しない場合は作成する
                Directory.CreateDirectory(logDir);
            }
            else
            {
                // logファイルチェックはディレクトリが存在する場合だけでいい
                // 同日ログファイルが存在するなら読み込む
                if (File.Exists(logFile))
                {
                    // ファイルから読み込み
                    string reStrWord = @"[\w\+\-\.\@\:]+";
                    Regex re = new Regex($@"^({reStrWord})\t+({reStrWord})\t+({reStrWord})\t+({reStrWord})\t+({reStrWord})\t+({reStrWord})\t+(\d+)$", RegexOptions.Compiled);
                    using (var reader = new StreamReader(logFile))
                    {
                        string buff;
                        while (!reader.EndOfStream)
                        {
                            buff = reader.ReadLine();
                            var match = re.Match(buff);
                            if (match.Success)
                            {
                                int min;
                                try
                                {
                                    min = int.Parse(match.Groups[7].ToString());
                                }
                                catch
                                {
                                    min = 0;
                                }
                                RestoreLogs.Add(
                                    (
                                        match.Groups[1].ToString(),
                                        match.Groups[2].ToString(),
                                        match.Groups[3].ToString(),
                                        match.Groups[4].ToString(),
                                        match.Groups[5].ToString(),
                                        match.Groups[6].ToString()
                                    ),
                                    min
                                );
                            }
                        }
                    }
                    if (RestoreLogs.Count > 0)
                    {
                        // 1つ以上ログを取得したら復旧する
                        reqRestore = true;
                    }
                }
            }
        }

        public int Restore(ObservableCollection<TaskKey> TaskKeys)
        {
            int sum = 0;
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        var dictkey = (key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item);
                        if (RestoreLogs.TryGetValue(dictkey, out int min))
                        {
                            item.time = min;
                            item.MakeDispTime();
                            sum += min;
                        }
                    }
                }
            }
            return sum;
        }

        public void Update(ObservableCollection<TaskKey> TaskKeys)
        {
            // 要素の数をカウント
            int elem = 0;
            foreach (var key in TaskKeys)
            {
                elem += key.SubKey.Count;
            }
            // 要素分の領域を確保して初期化
            Logs = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)>(elem);
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        if (item.time != 0)
                        {
                            Logs.Add((key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item, item.time));
                        }
                    }
                }
            }
        }

        public async Task SaveAsync()
        {
            // ファイル書き込み
            using (var writer = new StreamWriter(logFileTemp))
            {
                foreach (var key in Logs)
                {
                    writer.WriteLine($"{key.Code}\t{key.Name}\t{key.Alias}\t{key.SubCode}\t{key.SubAlias}\t{key.Item}\t{key.min}");
                }
            }
            // 旧ファイルを削除
            File.Delete(logFile);
            // tmpファイルを新ファイルとしてリネーム
            File.Move(logFileTemp, logFile);
        }
    }
}
