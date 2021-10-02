﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace TaskTimer
{
    using LogList = List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)>;

    class Logger
    {
        private string rootDir;
        private string logDir;
        private string baseFileName;
        private string daykey;
        private string logFile;
        private string logFileTemp;

        public bool hasLog;

        public List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)> SaveBuff;
        public Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item), int> LoadLog;

        public Logger()
        {
            rootDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            logDir = rootDir + @"\log";
            baseFileName = "log";
            // ターゲット日時からファイル名等作成
            UpdateDate();
            // ログファイルからの読み出しデータ有無
            hasLog = false;
        }

        public void UpdateDate()
        {
            // 本日の日付取得
            // ログファイルキーとする
            daykey = Util.TargetDate.ToString("yyyyMMdd");
            // ログファイル名作成
            logFile = $@"{logDir}\{baseFileName}.{daykey}.txt";
            logFileTemp = $@"{logDir}\{baseFileName}.{daykey}.tmp";
        }

        public void Load()
        {
            LoadLog = new Dictionary<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item), int>();
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
                    Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+(\d+)$", RegexOptions.Compiled);
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
                                var key = (
                                    match.Groups[1].ToString(),
                                    match.Groups[2].ToString(),
                                    match.Groups[3].ToString(),
                                    match.Groups[4].ToString(),
                                    match.Groups[5].ToString(),
                                    match.Groups[6].ToString()
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
                        var dictkey = (key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item);
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
            SaveBuff = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, int min)>(elem);
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
                            SaveBuff.Add((key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item, item.time));
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
                foreach (var key in SaveBuff)
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
