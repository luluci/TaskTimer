using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TaskTimer;


namespace TaskTimer
{
    class Settings
    {
        private string tgtDir;
        private string inputKeyFile;
        private string inputKeyFileTemp;
        private string inputSubKeyFile;
        private string inputSubKeyFileTemp;
        private bool settingFileLoaded;

        public List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias)> Keys;
        public List<(string Code, string Alias, string Item, string ItemAlias)> SubKeys;

        public Settings(string tgtDirPath)
        {
            tgtDir = tgtDirPath;
            inputKeyFile = tgtDir + @"\key.txt";                          // Key * SubKey の全組み合わせを記憶
            inputKeyFileTemp = tgtDir + @"\key.tmp";                      // Key * SubKey の全組み合わせを記憶
            inputSubKeyFile = tgtDir + @"\subkey_template.txt";           // SubKeyのテンプレート設定ファイル
            inputSubKeyFileTemp = tgtDir + @"\subkey_template.tmp";       // SubKeyのテンプレート設定保存時一時ファイル
            settingFileLoaded = false;

            Keys = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias)>();
            SubKeys = new List<(string Code, string Alias, string Item, string ItemAlias)>();
        }

        public void Load()
        {
            // keyファイル、subkye_templateファイルを読み込み済みならスキップ
            if (settingFileLoaded) return;

            // 設定ファイルからロード
            // フォルダチェック
            if (!Directory.Exists(tgtDir))
            {
                // 存在しない場合は作成する
                Directory.CreateDirectory(tgtDir);
            }
            // ファイルチェック
            // メインキーファイル
            if (File.Exists(inputKeyFile))
            {
                // ファイルが存在するならロード
                Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})$", RegexOptions.Compiled);
                // ファイルが存在するならロード
                using (var reader = new StreamReader(inputKeyFile))
                {
                    string buff;
                    while (!reader.EndOfStream)
                    {
                        buff = reader.ReadLine();
                        var match = re.Match(buff);
                        if (match.Success)
                        {
                            Keys.Add((
                                match.Groups[1].ToString(),
                                match.Groups[2].ToString(),
                                match.Groups[3].ToString(),
                                match.Groups[4].ToString(),
                                match.Groups[5].ToString(),
                                match.Groups[6].ToString(),
                                match.Groups[7].ToString()
                            ));
                        }
                    }
                }
                // 結果チェック
                if (Keys.Count == 0)
                {
                    throw new Exception($"Keys dont loaded. File[{inputKeyFile}]");
                }
            }
            else
            {
                // ファイルが存在しなければベース作成
                File.CreateText(inputKeyFile);
                // ダミーでKey作成
                //Keys.Add(("NewCode", "NewName", "NewAlias", "NewSubCode", "NewSubAlias", "NewItem", "NewItemAlias"));
            }
            // サブキーファイル
            if (File.Exists(inputSubKeyFile))
            {
                //
                Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})$", RegexOptions.Compiled);
                // ファイルが存在するならロード
                using (var reader = new StreamReader(inputSubKeyFile))
                {
                    string buff;
                    while (!reader.EndOfStream)
                    {
                        buff = reader.ReadLine();
                        var match = re.Match(buff);
                        if (match.Success)
                        {
                            SubKeys.Add((
                                match.Groups[1].ToString(),
                                match.Groups[2].ToString(),
                                match.Groups[3].ToString(),
                                match.Groups[4].ToString()
                            ));
                        }
                    }
                }
                // 結果チェック
                if (SubKeys.Count == 0)
                {
                    throw new Exception($"SubKeys dont loaded. File[{inputSubKeyFile}]");
                }
            }
            else
            {
                // ファイルが存在しなければテンプレートでファイル作成
                // テンプレートでSubKey作成
                SubKeys.Add(("CodeA", "AliasA", "ItemA", "ItemA_1"));
                SubKeys.Add(("CodeB", "AliasB", "ItemB", "ItemB_1"));
                SubKeys.Add(("CodeC", "AliasC", "ItemC", "ItemC_1"));
                SubKeys.Add(("CodeD", "AliasD", "ItemD", "ItemD_1"));
                // ファイル書き込み
                using (var writer = File.CreateText(inputSubKeyFile))
                {
                    foreach (var subkey in SubKeys)
                    {
                        writer.WriteLine($"{subkey.Code}\t{subkey.Alias}\t{subkey.Item}\t{subkey.ItemAlias}");
                    }
                }
            }

            // 一応最後にロード済み設定
            settingFileLoaded = true;
        }

        public void Update(ObservableCollection<TaskKey> TaskKeys)
        {
            // 要素の数をカウント
            int elem = 0;
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    elem += subkey.Item.Count;
                }
            }
            // 要素分の領域を確保して初期化
            Keys = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item, string ItemAlias)>(elem);
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        Keys.Add((key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item, item.ItemAlias));
                    }
                }
            }
        }

        public bool AskFileLock()
        {
            // ファイルがロックされていたら、ファイルを閉じることを促す
            return Util.AskFileLock(inputKeyFile, $"設定ファイル({inputKeyFile})を保存しています");
        }
        public bool CheckFileLock()
        {
            // ファイルがロックされているかどうかをチェック
            return !Util.IsFileLocked(inputKeyFile);
        }

        public async Task SaveAsync()
        {
            // ファイル書き込み
            // tmpファイルに一旦保存する
            using (var writer = new StreamWriter(inputKeyFileTemp))
            {
                foreach (var key in Keys)
                {
                    await writer.WriteLineAsync($"{key.Code}\t{key.Name}\t{key.Alias}\t{key.SubCode}\t{key.SubAlias}\t{key.Item}\t{key.ItemAlias}");
                }
            }
            // 旧ファイルを削除
            File.Delete(inputKeyFile);
            // tmpファイルを新ファイルとしてリネーム
            File.Move(inputKeyFileTemp, inputKeyFile);
        }
    }
}
