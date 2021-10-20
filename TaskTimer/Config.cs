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


        public string FixTask
        {
            get
            {
                return json.FixTask;
            }
            set
            {
                json.FixTask = value;
            }
        }

        public string ExcelPath
        {
            get
            {
                return json.ExcelPath;
            }
            set
            {
                json.ExcelPath = value;
            }
        }


        public string AutoPilotUrl {
            get
            {
                return json.AutoPilotUrl;
            }
            set
            {
                json.AutoPilotUrl = value;
            }
        }
        public string AutoPilotId
        {
            get
            {
                return json.AutoPilotId;
            }
            set
            {
                json.AutoPilotId = value;
            }
        }
        public string AutoPilotPassword
        {
            get
            {
                if (json.AutoPilotPassword == "") return json.AutoPilotPassword;
                else return Decrypt(json.AutoPilotPassword);
            }
            set
            {
                json.AutoPilotPassword = Encrypt(value);
            }
        }


        private string Encrypt(string text)
        {
            using (var rijndael = new System.Security.Cryptography.RijndaelManaged())
            {
                //myRijndael.BlockSize = 128;
                //myRijndael.KeySize = 128;
                rijndael.Mode = System.Security.Cryptography.CipherMode.CBC;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                rijndael.IV = Convert.FromBase64String(json.SecurityAesIv);
                rijndael.Key = Convert.FromBase64String(json.SecurityAesKey);

                var encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;

                using (MemoryStream mStream = new MemoryStream())
                {
                    using (var ctStream = new System.Security.Cryptography.CryptoStream(mStream, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return Convert.ToBase64String(encrypted);
            }
        }
        private string Decrypt(string text)
        {
            using (var rijndael = new System.Security.Cryptography.RijndaelManaged())
            {
                //myRijndael.BlockSize = 128;
                //myRijndael.KeySize = 128;
                rijndael.Mode = System.Security.Cryptography.CipherMode.CBC;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                rijndael.IV = Convert.FromBase64String(json.SecurityAesIv);
                rijndael.Key = Convert.FromBase64String(json.SecurityAesKey);

                var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(Convert.FromBase64String(text)))
                {
                    using (var ctStream = new System.Security.Cryptography.CryptoStream(mStream, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadLine();
                        }
                    }
                }
                return plain;
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
                    FixTask = "",
                    ExcelPath = "",
                    AutoPilotUrl = "",
                    AutoPilotId = "",
                    AutoPilotPassword = "",
                };
            }
            // 不足設定を更新する
            LoadVersionUp();
        }

        private void LoadVersionUp()
        {
            if (json.FixTask == null)
            {
                json.FixTask = "";
            }
            if (json.ExcelPath == null)
            {
                json.ExcelPath = "";
            }
            if (json.AutoPilotUrl == null)
            {
                json.AutoPilotUrl = "";
            }
            if (json.AutoPilotId == null)
            {
                json.AutoPilotId = "";
            }
            if (json.AutoPilotPassword == null)
            {
                json.AutoPilotPassword = "";
            }
            if (json.SecurityAesIv == null)
            {
                using (var myRijndael = new System.Security.Cryptography.RijndaelManaged())
                {
                    myRijndael.GenerateIV();
                    json.SecurityAesIv = Convert.ToBase64String(myRijndael.IV);
                    //json.SecurityAesIv = myRijndael.IV.ToString();
                }
            }
            if (json.SecurityAesKey == null)
            {
                using (var myRijndael = new System.Security.Cryptography.RijndaelManaged())
                {
                    myRijndael.GenerateKey();
                    json.SecurityAesKey = Convert.ToBase64String(myRijndael.Key);
                    //json.SecurityAesKey = myRijndael.Key.ToString();
                }
            }
        }

        private string MakeRandom()
        {
            byte[] random = new byte[16];

            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {

                //rng.GetBytes(random);
                rng.GetNonZeroBytes(random);
            }

            return random.ToString();
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

        // 固定タスク：削除不可とする
        [JsonPropertyName("fix_task")]
        public string FixTask { get; set; }

        [JsonPropertyName("excel_path")]
        public string ExcelPath { get; set; }

        [JsonPropertyName("autopilot_url")]
        public string AutoPilotUrl { get; set; }

        [JsonPropertyName("autopilot_id")]
        public string AutoPilotId { get; set; }

        [JsonPropertyName("autopilot_password")]
        public string AutoPilotPassword { get; set; }

        [JsonPropertyName("security_aes_iv")]
        public string SecurityAesIv { get; set; }

        [JsonPropertyName("security_aes_key")]
        public string SecurityAesKey { get; set; }
    }
}
