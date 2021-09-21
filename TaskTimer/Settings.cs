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
        private string rootDir;
        private string inputDir;
        private string inputKeyFile;
        private string inputKeyFileTemp;
        private string inputSubKeyFile;
        private string inputSubKeyFileTemp;

        public List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item)> Keys;
        public List<(string Code, string Alias, string Item)> SubKeys;

        public Settings()
        {
            rootDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            inputDir = rootDir + @"\settings";
            inputKeyFile = inputDir + @"\key.txt";                          // Key * SubKey の全組み合わせを記憶
            inputKeyFileTemp = inputDir + @"\key.tmp";                      // Key * SubKey の全組み合わせを記憶
            inputSubKeyFile = inputDir + @"\subkey_template.txt";           // SubKeyのテンプレート設定ファイル
            inputSubKeyFileTemp = inputDir + @"\subkey_template.tmp";       // SubKeyのテンプレート設定保存時一時ファイル

            Keys = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item)>();
            SubKeys = new List<(string Code, string Alias, string Item)>();
        }

        public void Load()
        {
            // 設定ファイルからロード
            // フォルダチェック
            if (!Directory.Exists(inputDir))
            {
                // 存在しない場合は作成する
                Directory.CreateDirectory(inputDir);
            }
            // ファイルチェック
            // メインキーファイル
            if (File.Exists(inputKeyFile))
            {
                // ファイルが存在するならロード
                Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})$", RegexOptions.Compiled);
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
                                match.Groups[6].ToString()
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
                Keys.Add(("NewCode", "NewName", "NewAlias", "NewSubCode", "NewSubAlias", "NewItem"));
            }
            // サブキーファイル
            if (File.Exists(inputSubKeyFile))
            {
                //
                Regex re = new Regex($@"^({Util.reWord})\t+({Util.reWord})\t+({Util.reWord})$", RegexOptions.Compiled);
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
                                match.Groups[3].ToString()
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
                SubKeys.Add(("CodeA", "AliasA", "ItemA"));
                SubKeys.Add(("CodeB", "AliasB", "ItemB"));
                SubKeys.Add(("CodeC", "AliasC", "ItemC"));
                SubKeys.Add(("CodeD", "AliasD", "ItemD"));
                SubKeys.Add(("CodeE", "AliasE", "ItemE"));
                SubKeys.Add(("CodeF", "AliasF", "ItemF"));
                // ファイル書き込み
                using (var writer = File.CreateText(inputSubKeyFile))
                {
                    foreach (var subkey in SubKeys)
                    {
                        writer.WriteLine($"{subkey.Code}\t{subkey.Alias}");
                    }
                }
            }
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
            Keys = new List<(string Code, string Name, string Alias, string SubCode, string SubAlias, string Item)>(elem);
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        Keys.Add((key.Code, key.Name, key.Alias, subkey.Code, subkey.Alias, item.Item));
                    }
                }
            }
        }

        public async Task SaveAsync()
        {
            // ファイル書き込み
            // tmpファイルに一旦保存する
            using (var writer = new StreamWriter(inputKeyFileTemp))
            {
                foreach (var key in Keys)
                {
                    writer.WriteLine($"{key.Code}\t{key.Name}\t{key.Alias}\t{key.SubCode}\t{key.SubAlias}\t{key.Item}");
                }
            }
            // 旧ファイルを削除
            File.Delete(inputKeyFile);
            // tmpファイルを新ファイルとしてリネーム
            File.Move(inputKeyFileTemp, inputKeyFile);
        }
    }
}
