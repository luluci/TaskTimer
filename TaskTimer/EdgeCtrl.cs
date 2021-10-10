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
        }

        public void Dispose()
        {
            if (driver != null) driver.Dispose();
            if (service != null) service.Dispose();

            driver = null;
            service = null;
        }

        public async Task Init()
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
                    // Driver作成
                    driver = new EdgeDriver(service, options);
                });

                //ユーザーID
                //driver.FindElement(By.Name("pid")).SendKeys("userId");
                //パスワード
                //driver.FindElement(By.Name("password")).SendKeys("pw");

                //ログインボタン
                //IWebElement findbuttom = driver.FindElement(By.Name("btnname"));
                //ログインボタンをクリック
                //findbuttom.Click();
            }
            catch (Exception ex)
            {
                Dispose();
                MessageBox.Show(ex.ToString());
            }
        }
        
        public async Task Navigate(string url)
        {
            try
            {
                await Task.Run(() =>
                {
                    // サイトを開く
                    driver.Navigate().GoToUrl(url);
                });

                //ユーザーID
                //driver.FindElement(By.Name("pid")).SendKeys("userId");
                //パスワード
                //driver.FindElement(By.Name("password")).SendKeys("pw");

                //ログインボタン
                //IWebElement findbuttom = driver.FindElement(By.Name("btnname"));
                //ログインボタンをクリック
                //findbuttom.Click();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
