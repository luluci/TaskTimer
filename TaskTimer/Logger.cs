using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Threading;

namespace TaskTimer
{
    using LogList = List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)>;

    class Logger
    {
        private string tgtDir;
        private string baseFileName;
        private string daykey;
        private string logFile;
        private string logFileTemp;

        public bool hasLog;

        public List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias, int min)> SaveBuff;
        public Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias), int> LoadLog;

        public Logger(string tgtDirPath)
        {
            tgtDir = tgtDirPath;
            baseFileName = "log";
            // ターゲット日時からファイル名等作成
            UpdateDate();
            // ログファイルからの読み出しデータ有無
            hasLog = false;
        }

        private void MakeOutDir()
        {
            // フォルダチェック
            if (!Directory.Exists(tgtDir))
            {
                // 存在しない場合は作成する
                Directory.CreateDirectory(tgtDir);
            }
        }

        public void OpenOutDir()
        {
            MakeOutDir();
            System.Diagnostics.Process.Start("explorer.exe", tgtDir);
        }

        public void UpdateTgtDir(string tgtDirPath)
        {
            tgtDir = tgtDirPath;
            // ログファイル名作成
            logFile = Path.Combine(tgtDir, $"{baseFileName}.{daykey}.txt");
            logFileTemp = Path.Combine(tgtDir, $"{baseFileName}.{daykey}.tmp");
        }

        public void UpdateDate()
        {
            // 本日の日付取得
            // ログファイルキーとする
            daykey = Util.TargetDate.ToString("yyyyMMdd");
            // ログファイル名作成
            logFile = $@"{tgtDir}\{baseFileName}.{daykey}.txt";
            logFileTemp = $@"{tgtDir}\{baseFileName}.{daykey}.tmp";
        }

        public void Load()
        {
            // ログ読み出し情報初期化
            hasLog = false;
            LoadLog = new Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias), int>();
            // 同じ日付のログがあったらツールが途中終了したものとして、続きからカウントできるようにする。
            // 設定ファイルからロード
            // フォルダチェック
            if (!Directory.Exists(tgtDir))
            {
                // 存在しない場合は作成する
                Directory.CreateDirectory(tgtDir);
            }
            else
            {
                // logファイルチェックはディレクトリが存在する場合だけでいい
                // 同日ログファイルが存在するなら読み込む
                if (File.Exists(logFile))
                {
                    // ファイルから読み込み
                    Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+(\d+)$", RegexOptions.Compiled);
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
                                    min = int.Parse(match.Groups[8].ToString());
                                }
                                catch
                                {
                                    min = 0;
                                }
                                var key = (
                                    match.Groups[1].ToString(),
                                    match.Groups[2].ToString(),
                                    match.Groups[3].ToString(),
                                    match.Groups[4].ToString(),
                                    match.Groups[5].ToString(),
                                    match.Groups[6].ToString(),
                                    match.Groups[7].ToString()
                                );
                                if (LoadLog.ContainsKey(key))
                                {
                                    LoadLog[key] += min;
                                }
                                else
                                {
                                    // 同一アイテムが存在しなければ追加
                                    LoadLog.Add(key, min);
                                }
                            }
                        }
                    }
                    if (LoadLog.Count > 0)
                    {
                        // 1つ以上ログを取得したら復旧する
                        hasLog = true;
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
                        var dictkey = (key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item, item.ItemAlias);
                        if (LoadLog.TryGetValue(dictkey, out int min))
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
            SaveBuff = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias, int min)>(elem);
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        // 全部出力する
                        //if (item.time != 0)
                        {
                            SaveBuff.Add((key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item, item.ItemAlias, item.time));
                        }
                    }
                }
            }
        }

        public bool AskFileLock()
        {
            // ファイルがロックされていたら、ファイルを閉じることを促す
            return Util.AskFileLock(logFile, $"設定ファイル({logFile})を保存しています");
        }
        public bool CheckFileLock()
        {
            // ファイルがロックされているかどうかをチェック
            return !Util.IsFileLocked(logFile);
        }

        public async Task SaveAsync()
        {
            // 
            var log = logFile;
            var temp = logFileTemp;
            await Task.Run(async () =>
            {

                // ファイル書き込み
                using (var writer = new StreamWriter(temp))
                {
                    foreach (var key in SaveBuff)
                    {
                        //
                        var code = key.Code.Replace('\t', '_');
                        var name = key.Name.Replace('\t', '_');
                        var alias = key.Alias.Replace('\t', '_');
                        var subcode = key.SubCode.Replace('\t', '_');
                        var subalias = key.SubAlias.Replace('\t', '_');
                        var item = key.Item.Replace('\t', '_');
                        var itemalias = key.ItemAlias.Replace('\t', '_');
                        //
                        await writer.WriteLineAsync($"{code}\t{name}\t{alias}\t{subcode}\t{subalias}\t{item}\t{itemalias}\t{key.min}");
                    }
                }
                // 旧ファイルを削除
                File.Delete(log);
                // tmpファイルを新ファイルとしてリネーム
                File.Move(temp, log);
            });
        }
    }
}
