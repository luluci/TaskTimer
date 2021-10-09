using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

namespace TaskTimer
{
    public enum SummarySaveFormat
    {
        CodeNameSubAll,                 // MainCode/MainName/SubCode でレコードをすべて出力
        CodeNameSubNonZero,             // MainCode/MainName/SubCode で非ゼロ値のレコードを出力
        CodeNameAliasSubItemAll,        // MainCode/MainName/MainAlias/SubCode/Item でレコードをすべて出力
        CodeNameAliasSubItemNonZero,    // MainCode/MainName/MainAlias/SubCode/Item で非ゼロ値のレコードを出力
    }

    class Summary
    {
        // ファイル情報
        private string rootDir;
        private string tgtDir;
        private string baseFileName;
        private string daykey;
        private string summaryFileType1;
        private string summaryFileType1Temp;
        private string summaryFileType2;
        private string summaryFileType2Temp;
        // 
        public int timeAll;
        private string logdummy;

        public Summary(string tgtDirPath)
        {
            // ファイル情報
            tgtDir = tgtDirPath;
            baseFileName = "summary";
            // 本日の日付取得
            // ログファイルキーとする
            DateTime dt = DateTime.Now;
            daykey = dt.ToString("yyyyMMdd");
            // ログファイル名作成
            summaryFileType1 = $@"{tgtDir}\{baseFileName}.type1.{daykey}.txt";
            summaryFileType1Temp = $@"{tgtDir}\{baseFileName}.type1.{daykey}.tmp";
            summaryFileType2 = $@"{tgtDir}\{baseFileName}.type2.{daykey}.txt";
            summaryFileType2Temp = $@"{tgtDir}\{baseFileName}.type2.{daykey}.tmp";

            timeAll = 0;
            logdummy = "";
            data = new ObservableCollection<SummaryNode>();
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

        public void Add(int min)
        {
            timeAll += min;
        }

        private Dictionary<(string, string, string, string, string), int> log;
        public void MakeLog(ObservableCollection<TaskKey> TaskKeys, SummarySaveFormat format)
        {
            log = new Dictionary<(string, string, string, string, string), int>();
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        switch (format)
                        {
                            case SummarySaveFormat.CodeNameSubAll:
                                MakeLogCodeNameSub(log, key, subkey, item);
                                break;
                            case SummarySaveFormat.CodeNameSubNonZero:
                                if (item.time != 0)
                                {
                                    MakeLogCodeNameSub(log, key, subkey, item);
                                }
                                break;
                            case SummarySaveFormat.CodeNameAliasSubItemAll:
                                MakeLogCodeNameAliasSubItem(log, key, subkey, item);
                                break;
                            case SummarySaveFormat.CodeNameAliasSubItemNonZero:
                                if (item.time != 0)
                                {
                                    MakeLogCodeNameAliasSubItem(log, key, subkey, item);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void MakeLogCodeNameSub(
            Dictionary<(string, string, string, string, string), int> log,
            TaskKey key, TaskKeySub subkey, TaskItem item
        )
        {
            var dickey = (key.Code, key.Name, subkey.Code, logdummy, logdummy);
            if (log.ContainsKey(dickey))
            {
                if (log.TryGetValue(dickey, out int min))
                {
                    min += item.time;
                }
            }
            else
            {
                log.Add(dickey, item.time);
            }
        }
        private void MakeLogCodeNameAliasSubItem(
            Dictionary<(string, string, string, string, string), int> log,
            TaskKey key, TaskKeySub subkey, TaskItem item
        )
        {
            var dickey = (key.Code, key.Name, key.Alias, subkey.Code, item.Item);
            if (log.ContainsKey(dickey))
            {
                if (log.TryGetValue(dickey, out int min))
                {
                    min += item.time;
                }
            }
            else
            {
                log.Add(dickey, item.time);
            }
        }

        private string GetSummaryFile(SummarySaveFormat format)
        {
            switch (format)
            {
                case SummarySaveFormat.CodeNameSubAll:
                case SummarySaveFormat.CodeNameSubNonZero:
                    return summaryFileType1;
                case SummarySaveFormat.CodeNameAliasSubItemAll:
                case SummarySaveFormat.CodeNameAliasSubItemNonZero:
                    return summaryFileType2;
                default:
                    return "";
            }
        }
        public bool AskFileLock(SummarySaveFormat format)
        {
            // ファイルがロックされていたら、ファイルを閉じることを促す
            var output = GetSummaryFile(format);
            return Util.AskFileLock(output, $"設定ファイル({output})を保存しています");
        }
        public bool CheckFileLock(SummarySaveFormat format)
        {
            // ファイルがロックされているかどうかをチェック
            var output = GetSummaryFile(format);
            return !Util.IsFileLocked(output);
        }

        public async Task SaveAsync(SummarySaveFormat format)
        {
            // フォルダチェック
            MakeOutDir();
            // 保存
            string outputTemp;
            string output;
            switch (format)
            {
                case SummarySaveFormat.CodeNameSubAll:
                case SummarySaveFormat.CodeNameSubNonZero:
                    outputTemp = summaryFileType1Temp;
                    output = summaryFileType1;
                    // ファイル書き込み
                    using (var writer = new StreamWriter(outputTemp))
                    {
                        foreach (var record in log)
                        {
                            writer.WriteLine($"{record.Key.Item1}\t{record.Key.Item2}\t{record.Key.Item3}\t{Util.Min2Time(record.Value)}");
                        }
                    }
                    break;
                case SummarySaveFormat.CodeNameAliasSubItemAll:
                case SummarySaveFormat.CodeNameAliasSubItemNonZero:
                    outputTemp = summaryFileType2Temp;
                    output = summaryFileType2;
                    // ファイル書き込み
                    using (var writer = new StreamWriter(outputTemp))
                    {
                        foreach (var record in log)
                        {
                            writer.WriteLine($"{record.Key.Item1}\t{record.Key.Item2}\t{record.Key.Item3}\t{record.Key.Item4}\t{record.Key.Item5}\t{Util.Min2Time(record.Value)}");
                        }
                    }
                    break;
                default:
                    return;
            }
            // 旧ファイルを削除
            File.Delete(output);
            // tmpファイルを新ファイルとしてリネーム
            File.Move(outputTemp, output);
        }

        private ObservableCollection<SummaryNode> data;
        public ObservableCollection<SummaryNode> Data
        {
            get { return data; }
            set { data = value; }
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
            }
        }

        /** Summary作成
         * 
         */
        public void Update(ObservableCollection<TaskKey> TaskKeys)
        {
            // 要素分の領域を確保して初期化
            data = new ObservableCollection<SummaryNode>();
            // 最新のタスク設定を取得
            int keyIndex = 0;
            foreach (var key in TaskKeys)
            {
                int subkeyIndex = 0;
                foreach (var subkey in key.SubKey)
                {
                    int itemIndex = 0;
                    foreach (var item in subkey.Item)
                    {
                        if (item.time != 0)
                        {
                            data.Add(new SummaryNode(key.Alias, item.Item, item.timeDisp, keyIndex, subkeyIndex, itemIndex));
                        }

                        itemIndex++;
                    }

                    subkeyIndex++;
                }

                keyIndex++;
            }
        }

        public (int, int, int) GetIndex(int idx)
        {
            if (data.Count > idx && idx >= 0)
            {
                return data[idx].GetIndex();
            }
            else
            {
                return (-1, -1, -1);
            }
        }
    }

    class SummaryNode
    {
        private int keyIndex;
        private int subkeyIndex;
        private int itemIndex;

        public SummaryNode(string alias, string item, string time, int keyIndex, int subkeyIndex, int itemIndex)
        {
            this.alias = alias;
            this.item = item;
            this.time = time;
            this.keyIndex = keyIndex;
            this.subkeyIndex = subkeyIndex;
            this.itemIndex = itemIndex;
        }

        public (int,int,int) GetIndex()
        {
            return (keyIndex, subkeyIndex, itemIndex);
        }

        private string alias;
        public string Alias
        {
            get { return alias; }
        }

        private string item;
        public string Item
        {
            get { return item; }
        }

        private string time;
        public string Time
        {
            get { return time; }
        }
    }
}
