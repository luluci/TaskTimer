using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace TaskTimer
{
    class Config
    {
        private string configFilePath;
        private JsonItem json;

        public Config()
        {
            // パス設定
            configFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + @".json";
        }

        public string LogDir
        {
            get { return json.LogDir; }
            set {
                json.LogDir = value;
            }
        }
        public string LogDirPath
        {
            get
            {
                if (json.LogDir.Length == 0)
                {
                    return Util.rootDirPath + @"\log";
                }
                else
                {
                    return GetAbsPath(json.LogDir);
                }
            }
        }

        public string SummaryDir
        {
            get { return json.SummaryDir; }
            set { json.SummaryDir = value; }
        }
        public string SummaryDirPath
        {
            get
            {
                if (json.SummaryDir.Length == 0)
                {
                    return Util.rootDirPath + @"\summary";
                }
                else
                {
                    return GetAbsPath(json.SummaryDir);
                }
            }
        }

        public string SettingsDir
        {
            get { return json.SettingsDir; }
            set { json.SettingsDir = value; }
        }
        public string SettingsDirPath
        {
            get
            {
                if (json.SettingsDir.Length == 0)
                {
                    return Util.rootDirPath + @"\settings";
                }
                else
                {
                    return GetAbsPath(json.SettingsDir);
                }
            }
        }

        private string GetAbsPath(string path)
        {
            if (System.IO.Path.IsPathRooted(path))
            {
                var uri = new Uri(path);
                if (uri.IsUnc)
                {
                    // UNCパスの場合
                    return path;
                }
                else
                {
                    // 絶対パス
                    return path;
                }
            }
            else
            {
                // 相対パス
                var baseuri = new Uri(Util.rootDirPath + "\\");
                var absuri = new Uri(baseuri, path);
                return absuri.LocalPath;
            }
        }

        /** 初回起動用に同期的に動作する
         * 
         */
        public async Task Load()
        {
            // 設定ロード
            if (File.Exists(configFilePath))
            {
                // ファイルが存在する
                //
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                };
                //
                using (var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read))
                {
                    // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                    json = await JsonSerializer.DeserializeAsync<JsonItem>(stream, options).ConfigureAwait(false);
                    //json = await JsonSerializer.DeserializeAsync<JsonItem>(stream, options);
                    //var task = JsonSerializer.DeserializeAsync<JsonItem>(stream, options);
                    //task.AsTask().Wait();
                    //json = task.Result;
                }
            }
            else
            {
                // ファイルが存在しない
                json = new JsonItem {
                    LogDir = "",
                    SummaryDir = "",
                    SettingsDir = "",
                };
            }
        }

        public bool AskFileLock()
        {
            // ファイルがロックされていたら、ファイルを閉じることを促す
            return Util.AskFileLock(configFilePath, $"設定ファイル({configFilePath})を保存しています");
        }
        public bool CheckFileLock()
        {
            // ファイルがロックされているかどうかをチェック
            return !Util.IsFileLocked(configFilePath);
        }

        public async Task SaveAsync()
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            //
            string jsonStr = JsonSerializer.Serialize(json, options);
            //
            using (var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
            {
                // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                //await JsonSerializer.SerializeAsync(stream, json, options).ConfigureAwait(false);
                await JsonSerializer.SerializeAsync(stream, json, options);
                //var task = JsonSerializer.SerializeAsync(stream, json, options);
                //task.Wait();
            }
        }
    }

    public class JsonItem
    {
        [JsonPropertyName("log_dir")]
        public string LogDir { get; set; }

        [JsonPropertyName("summary_dir")]
        public string SummaryDir { get; set; }

        [JsonPropertyName("settings_dir")]
        public string SettingsDir { get; set; }

    }
}
