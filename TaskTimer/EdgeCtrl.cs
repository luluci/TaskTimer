using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TaskTimer
{
    class EdgeCtrl : IDisposable
    {
        private EdgeDriverService service = null;
        private EdgeOptions options = null;
        private EdgeDriver driver = null;

        public EdgeCtrl()
        {
            // Edgeのバージョンに合わせてドライバをダウンロードする。
            // "msedgedriver.exe"を"TaskTimer.exe"と同じフォルダに配置する。
            // https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/

            // Selenium
            // https://www.seleniumqref.com/api/webdriver_gyaku.html
        }

        public void Dispose()
        {
            if (driver != null) driver.Dispose();
            if (service != null) service.Dispose();

            driver = null;
            service = null;
        }

        public async Task<bool> Init()
        {
            try
            {
                await Task.Run(() =>
                {
                    // ドライバー起動時に表示されるコンソール画面を非表示にする
                    service = EdgeDriverService.CreateChromiumService();
                    service.HideCommandPromptWindow = true;
                    // EdgeChromium版を使用
                    options = new EdgeOptions();
                    options.UseChromium = true;
                    options.PageLoadStrategy = OpenQA.Selenium.PageLoadStrategy.Normal;     // loadイベントが発生したときに処理が戻る？
                    // Driver作成
                    driver = new EdgeDriver(service, options);
                });
                return true;
            }
            catch (Exception ex)
            {
                Dispose();
                MessageBox.Show(ex.ToString());
                return false;
            }
        }
        
        public async Task Navigate(string url, string id, string password)
        {
            try
            {
                await Task.Run(() =>
                {
                    AutoPilot(url, id, password);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void AutoPilot(string url, string id, string password)
        {
            // サイトを開く
            driver.Navigate().GoToUrl(url);

            // title取得
            //var title = driver.FindElement(By.TagName("title"));
            //MessageBox.Show(driver.Title);

            if (driver.Title == "Google")
            {
                // 検索ボックスにテキスト設定
                driver.FindElement(By.Name("q")).SendKeys("test");
            }
            else if (driver.Title == "ログイン - ニコニコ")
            {
                // ID/Password入力
                driver.FindElement(By.Id("input__mailtel")).SendKeys(id);
                driver.FindElement(By.Id("input__password")).SendKeys(password);
                // ログインボタン
                var loginbtn = driver.FindElement(By.Id("login__submit"));
                loginbtn.Click();       // Clickもloadイベント発火で処理が戻ってくるっぽい
                //
                MessageBox.Show(driver.Title);
            }
            else if (driver.Title == "test_table")
            {
                var result = new StringBuilder();
                var table = driver.FindElements(By.XPath("//*[@id='test_table_1']/tbody/tr"));
                foreach (var row in table)
                {
                    var cols = row.FindElements(By.TagName("td"));
                    foreach (var col in cols)
                    {
                        result.Append($"({col.Text}), ");
                    }
                    result.Append("\r\n");
                }
                MessageBox.Show(result.ToString());
            }

            
        }
    }
}
