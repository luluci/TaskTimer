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
        private string tgtDir;
        private string tgtDirPath;
        private string configFileName;
        private string configFilePath;
        private JsonItem json;

        public Config()
        {
            // パス設定
            tgtDir = @"settings";
            tgtDirPath = Util.rootDirPath + @"\" + tgtDir;
            configFileName = @"config.json";
            configFilePath = tgtDirPath + @"\" + configFileName;
        }

        public async Task LoadAsync()
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
                };
            }
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
            using (var stream = new FileStream(configFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                await JsonSerializer.SerializeAsync(stream, json, options).ConfigureAwait(false);
                //await JsonSerializer.SerializeAsync(stream, json, options);
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

    }
}
