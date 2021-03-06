using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

using TaskTimer;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using System.Windows;

namespace TaskTimer
{
    enum TaskClass
    {
        MainKey,
        SubKey,
        Item,
    }

    enum TaskViewOpe
    {
        Up,
        Down,
        Delete,
    }

    class WindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }


        private ObservableCollection<TaskKey> key;
        public ObservableCollection<TaskKey> Key
        {
            get { return key; }
            set { key = value; }
        }

        public Config config;
        public Settings settings;
        public Logger logger;
        private bool hasChangeLog;          // ログ変更有無(TaskKey, TaskKeySub, TaskItemの変化有無)
        private Summary summary;
        public Timer timer;
        private Action updateTaskMain;
        private Action updateTaskSub;
        private Action<int, int> updateItem;
        private Task logSaveTask = null;
        private Task settingSaveTask = null;
        private TaskClass selectTaskClass;
        private bool isTargetDateChanged = false;
        private DispatcherTimer ticker;
        private Task configSaveTask = null;
        private EdgeCtrl EdgeCtrl = null;
        private ExcelCtrl ExcelCtrl = null;
        private Font Font;

        public WindowViewModel()
        {

            // 最初にconfigをロード
            config = new Config();
            //await config.Load();
            config.Load().Wait();   // UIスレッドを進めたくないので同期的に実行
            //
            
            Font = new Font("Meiryo UI");
            //
            settings = new Settings(config.SettingsDirPath);
            settings.Load();
            if (settings.Keys.Count == 0)
            {
                // 何も要素が無ければ初期化
                settings.Keys.Add(("Rule20%", "Rule20%", "Rule20%", "Rule20%", "Rule20%", "Rule20%", "Appli1"));
                settings.Keys.Add(("Rule20%", "Rule20%", "Rule20%", "Rule20%", "Rule20%", "Rule20%", "Appli2"));
            }
            //
            logger = new Logger(config.LogDirPath);
            logger.Load();
            hasChangeLog = false;

            // ChildからのNotify用コールバック
            updateTaskMain = () =>
            {
                // ログ更新あり
                hasChangeLog = true;
                //_updateSelectTaskMain();
                //NotifyPropertyChanged(nameof(SelectTask));
            };
            updateTaskSub = () =>
            {
                // ログ更新あり
                hasChangeLog = true;
            };
            updateItem = (int oldtime, int newtime) =>
            {
                // ログ更新あり
                hasChangeLog = true;
                //_updateSelectTaskSub();
                //NotifyPropertyChanged(nameof(SelectTask));
                int diff = newtime - oldtime;
                SummaryAdd(diff);
            };

            //
            summary = new Summary(config.SummaryDirPath);
            SummaryAdd(0);

            // 
            timer = new Timer();
            baseCount = timer.BaseCountTime();

            // Logに全要素を吐き出す前提
            // ロードしたLogファイルが存在すればLogに必要な情報はすべてあるのでLogの展開のみ行う
            // インスタンス作成
            this.key = new ObservableCollection<TaskKey>();
            LoadTask();

            // 要素の移動(Up,Down,Deleteボタンを押下したときの操作対象)に関する設定
            selectTaskClass = TaskClass.MainKey;            // 操作対象：MainKey
            UpdateTaskElementDisp();

            // Ticker設定
            // 1秒基準でカウント
            ticker = new DispatcherTimer();
            ticker.Interval = new TimeSpan(0, 0, 1);
            ticker.Tick += new EventHandler(timer_Tick);

            //
            EdgeCtrl = new EdgeCtrl();
            ExcelCtrl = new ExcelCtrl();
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            // 1秒経過を通知
            await TimerEllapse(1);
        }


        public string SelectFont
        {
            get { return Font.Select; }
        }
        public int SelectFontIndex
        {
            get { return Font.FontIndex; }
            set
            {
                Font.FontIndex = value;
                NotifyPropertyChanged(nameof(SelectFontIndex));
                NotifyPropertyChanged(nameof(SelectFont));
            }
        }
        public FontItem[] FontList
        {
            get { return Font.FontList; }
        }


        private void LoadTask()
        {
            // インスタンス初期化
            this.key.Clear();
            // タスク展開
            if (logger.hasLog)
            {
                // Logファイルから読みだしたデータがあればログを展開
                LoadLog();
            }
            else
            {
                // Logファイルから読みだしたデータが無ければsettingsからタスクを展開
                LoadSettings();
            }
            // 初期状態で最初のタスクを選択しておく。
            this.SelectedIndex = 0;
            this.SelectedIndexSub = 0;
            this.selectedIndexItem = 0;
        }

        private void LoadSettings()
        {
            // 設定ロード
            (string, string, string) mainCurrKey = ("", "", "");
            (string, string, string) mainNextKey;
            (string, string) subCurrKey = ("", "");
            (string, string) subNextKey;
            TaskKey taskMain = new TaskKey("", "", "", updateTaskMain);    // ダミーで初期化
            TaskKeySub taskSub = new TaskKeySub("", "", updateTaskSub);
            // settingからタスク設定をロード
            foreach (var keys in settings.Keys)
            {
                mainNextKey = (keys.Code, keys.Name, keys.Alias);
                subNextKey = (keys.SubCode, keys.SubAlias);
                if (mainCurrKey.Equals(mainNextKey))
                {
                    // MainKeyが同じならSubKeyチェック
                    if (subCurrKey.Equals(subNextKey))
                    {
                        // SubKeyが同じならItem追加
                        taskSub.Item.Add(new TaskItem(keys.ItemAlias, keys.Item, updateItem));
                    }
                    else
                    {
                        // SubKeyが異なればオブジェクト追加
                        // SubKey追加
                        taskSub = new TaskKeySub(keys.SubAlias, keys.SubCode, updateTaskSub);
                        taskMain.SubKey.Add(taskSub);
                        // Item追加
                        taskSub.Item.Add(new TaskItem(keys.ItemAlias, keys.Item, updateItem));
                        // SubKey更新
                        subCurrKey = subNextKey;
                    }
                }
                else
                {
                    // MainKeyが異なれば新しくオブジェクト追加
                    taskMain = new TaskKey(keys.Alias, keys.Code, keys.Name, updateTaskMain);
                    this.key.Add(taskMain);
                    // SubKey追加
                    taskSub = new TaskKeySub(keys.SubAlias, keys.SubCode, updateTaskSub);
                    taskMain.SubKey.Add(taskSub);
                    // Item追加
                    taskSub.Item.Add(new TaskItem(keys.ItemAlias, keys.Item, updateItem));
                    // MainKey更新
                    mainCurrKey = mainNextKey;
                    // SubKey更新
                    subCurrKey = subNextKey;
                }
            }
        }

        private void LoadLog()
        {
            // ログの内容を展開
            //int sum = logger.Expand(key);
            int sum = 0;
            // 設定ロード
            (string, string, string) mainCurrKey = ("", "", "");
            (string, string, string) mainNextKey;
            (string, string) subCurrKey = ("", "");
            (string, string) subNextKey;
            TaskKey taskMain = new TaskKey("", "", "", updateTaskMain);    // ダミーで初期化
            TaskKeySub taskSub = new TaskKeySub("", "", updateTaskSub);
            TaskItem taskItem;
            foreach (var item in logger.LoadLog)
            {
                mainNextKey = (item.Key.Code, item.Key.Name, item.Key.Alias);
                subNextKey = (item.Key.SubCode, item.Key.SubAlias);
                if (mainCurrKey.Equals(mainNextKey))
                {
                    // MainKeyが同じならSubKeyチェック
                    if (subCurrKey.Equals(subNextKey))
                    {
                        // SubKeyが同じならItem追加
                        taskItem = new TaskItem(item.Key.ItemAlias, item.Key.Item, updateItem);
                        taskItem.Time = item.Value;
                        taskSub.Item.Add(taskItem);
                    }
                    else
                    {
                        // SubKeyが異なればオブジェクト追加
                        // SubKey追加
                        taskSub = new TaskKeySub(item.Key.SubAlias, item.Key.SubCode, updateTaskSub);
                        taskMain.SubKey.Add(taskSub);
                        // Item追加
                        taskItem = new TaskItem(item.Key.ItemAlias, item.Key.Item, updateItem);
                        taskItem.Time = item.Value;
                        taskSub.Item.Add(taskItem);
                        // SubKey更新
                        subCurrKey = subNextKey;
                    }
                }
                else
                {
                    // MainKeyが異なれば新しくオブジェクト追加
                    taskMain = new TaskKey(item.Key.Alias, item.Key.Code, item.Key.Name, updateTaskMain);
                    this.key.Add(taskMain);
                    // SubKey追加
                    taskSub = new TaskKeySub(item.Key.SubAlias, item.Key.SubCode, updateTaskSub);
                    taskMain.SubKey.Add(taskSub);
                    // Item追加
                    taskItem = new TaskItem(item.Key.ItemAlias, item.Key.Item, updateItem);
                    taskItem.Time = item.Value;
                    taskSub.Item.Add(taskItem);
                    // MainKey更新
                    mainCurrKey = mainNextKey;
                    // SubKey更新
                    subCurrKey = subNextKey;
                }

                // 総和加算
                sum += item.Value;
            }
            // ログ時間を展開
            summary.Set(0);
            SummaryAdd(sum);
        }

        public async Task Close()
        {
            try
            {
                await CloseImpl();
            }
            catch
            {
                throw;
            }
        }
        public async Task CloseImpl()
        {
            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: CloseImpl START");
            //
            ticker.Stop();

            // Config出力
            if (configSaveTask == null || configSaveTask.IsCompleted)
            {
                // アプリ終了時のファイル保存はファイルロックの対処を確認する
                var check = config.AskFileLock();
                if (check)
                {
                    configSaveTask = config.SaveAsync();
                    await configSaveTask;
                    //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: await configSaveTask FINISH");
                }
            }
            ClosingSaveConfig = true;

            // Setting出力
            // running中のタスクがあるならスキップ
            if (settingSaveTask == null || settingSaveTask.IsCompleted)
            {
                // Keyはツール起動当日のものだけ保存する
                if (Util.CheckTargetDateIsToday())
                {
                    // アプリ終了時のファイル保存はファイルロックの対処を確認する
                    var check = settings.AskFileLock();
                    if (check)
                    {
                        this.settings.Update(this.key);
                        settingSaveTask = this.settings.SaveAsync();
                        await settingSaveTask;
                        //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: await settingSaveTask FINISH");
                    }
                }
            }
            ClosingSaveSettings = true;

            // ログ出力
            // running中のタスクがあるならスキップ
            if (logSaveTask == null || logSaveTask.IsCompleted)
            {
                // アプリ終了時のファイル保存はファイルロックの対処を確認する
                var check = logger.AskFileLock();
                if (check)
                {
                    this.logger.Update(this.key);
                    logSaveTask = this.logger.SaveAsync();
                    await logSaveTask;
                    //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: await logSaveTask FINISH");
                }
            }
            ClosingSaveLog = true;

            // EdgeCtrl終了待機
            if (EdgeCtrl.IsRunning()) await EdgeCtrl.WaitAsync();
            EdgeCtrl.Dispose();
            ClosingSaveEdge = true;

            // Excel Export終了待機
            if (ExcelCtrl.IsRunning()) await ExcelCtrl.WaitAsync();
            ClosingSaveExcel = true;

            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: CloseImpl FINISH");
        }

        private bool closingSaveConfig = false;
        public bool ClosingSaveConfig {
            get { return closingSaveConfig; }
            set
            {
                closingSaveConfig = value;
                NotifyPropertyChanged(nameof(ClosingSaveConfig));
            }
        }
        private bool closingSaveSettings = false;
        public bool ClosingSaveSettings
        {
            get { return closingSaveSettings; }
            set
            {
                closingSaveSettings = value;
                NotifyPropertyChanged(nameof(ClosingSaveSettings));
            }
        }
        private bool closingSaveLog = false;
        public bool ClosingSaveLog
        {
            get { return closingSaveLog; }
            set
            {
                closingSaveLog = value;
                NotifyPropertyChanged(nameof(ClosingSaveLog));
            }
        }
        private bool closingSaveEdge = false;
        public bool ClosingSaveEdge
        {
            get { return closingSaveEdge; }
            set
            {
                closingSaveEdge = value;
                NotifyPropertyChanged(nameof(ClosingSaveEdge));
            }
        }
        private bool closingSaveExcel = false;
        public bool ClosingSaveExcel
        {
            get { return closingSaveExcel; }
            set
            {
                closingSaveExcel = value;
                NotifyPropertyChanged(nameof(ClosingSaveExcel));
            }
        }


        private bool enableTargetDate = true;
        public bool EnableTargetDate {
            get { return enableTargetDate; }
            set
            {
                enableTargetDate = value;
                NotifyPropertyChanged(nameof(EnableTargetDate));
            }
        }

        public DateTime TargetDate
        {
            get
            {
                return Util.TargetDate;
            }
            set
            {
                if (!Util.TargetDate.Date.Equals(value.Date))
                {
                    isTargetDateChanged = true;
                }
                Util.TargetDate = value;
            }
        }

        public async Task TargetDateChanged()
        {
            // SetterでUtil.TargetDate更新後にコールされる
            // Setter内で処理したくなかったのでこちらで
            // 実際に日時が変わったときのみ処理
            if (isTargetDateChanged)
            {
                EnableTargetDate = false;
                // フラグを下す
                isTargetDateChanged = false;
                // タイマを止める
                TimerStop();
                // SummaryViewをクリア
                InitSummary();
                // 現在ログを保存しておく
                await SaveLogAsync();
                // 指定日時のログを展開する
                logger.UpdateDate();
                await Task.Run(() =>
                {
                    logger.Load();
                });
                LoadTask();
                EnableTargetDate = true;
            }
        }

        public async void TargetLogDirChanged()
        {
            // LogDirが変更されたタイミングでコール
            // 
            // タイマを止める
            TimerStop();
            // SummaryViewをクリア
            InitSummary();
            // 現在ログを保存しておく
            await SaveLogAsync();
            // LogDir更新
            logger.UpdateTgtDir(config.LogDir);
            // ログを展開する
            await Task.Run(() =>
            {
                logger.Load();
            });
            LoadTask();
        }

        public string LogDir
        {
            get { return config.LogDir; }
            set {
                bool changed = (config.LogDir != value);
                config.LogDir = value;
                if (changed)
                {
                    TargetLogDirChanged();
                }
            }
        }
        public string SummaryDir
        {
            get { return config.SummaryDir; }
            set {
                config.SummaryDir = value;
                summary.UpdateTgtDir(config.SummaryDir);
            }
        }
        public string SettingsDir
        {
            get { return config.SettingsDir; }
            set {
                config.SettingsDir = value;
                settings.UpdateTgtDir(config.SettingsDir);
            }
        }


        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                //_updateSelectTaskMain();
                NotifyPropertyChanged(nameof(SelectedIndex));
                //NotifyPropertyChanged(nameof(SelectTask));
                // delay時間更新
                timer.CountDelay();
                UpdateDelayCount();
            }
        }
        
        private int selectedIndexSub;
        public int SelectedIndexSub
        {
            get { return selectedIndexSub; }
            set
            {
                selectedIndexSub = value;
                //_updateSelectTaskSub();
                NotifyPropertyChanged(nameof(SelectedIndexSub));
                //NotifyPropertyChanged(nameof(SelectTask));
                // delay時間更新
                timer.CountDelay();
                UpdateDelayCount();
            }
        }

        private int selectedIndexItem;
        public int SelectedIndexItem
        {
            get { return selectedIndexItem; }
            set
            {
                selectedIndexItem = value;
                NotifyPropertyChanged(nameof(SelectedIndexItem));
                // delay時間更新
                timer.CountDelay();
                UpdateDelayCount();
            }
        }

        private int selectedIndexSummary;
        public int SelectedIndexSummary
        {
            get { return selectedIndexSummary; }
            set
            {
                selectedIndexSummary = value;
                this.summary.SelectedIndex = value;
            }
        }

        public void addTaskMain()
        {
            // ログ更新あり
            hasChangeLog = true;
            // 新しいタスク追加
            var newtask = new TaskKey("NewAlias", "NewCode", "NewName", updateTaskMain);
            this.key.Add(newtask);
            // テンプレート展開
            (string, string) subCurrKey = ("", "");
            (string, string) subNextKey;
            TaskKeySub taskSub = new TaskKeySub("", "", updateTaskSub);
            foreach (var subkey in settings.SubKeys)
            {
                subNextKey = (subkey.Code, subkey.Alias);
                // subKeyチェック
                if (subCurrKey.Equals(subNextKey))
                {
                    // SubKeyが同じならItem追加
                    taskSub.Item.Add(new TaskItem(subkey.ItemAlias, subkey.Item, updateItem));
                }
                else
                {
                    // SubKeyが異なればオブジェクト追加
                    // SubKey追加
                    taskSub = new TaskKeySub(subkey.Alias, subkey.Code, updateTaskSub);
                    newtask.SubKey.Add(taskSub);
                    // Item追加
                    taskSub.Item.Add(new TaskItem(subkey.ItemAlias, subkey.Item, updateItem));
                    // SubKey更新
                    subCurrKey = subNextKey;
                }
            }
        }

        public void addTaskSub()
        {
            // ログ更新あり
            hasChangeLog = true;
            // SubKey追加
            TaskKeySub taskSub = new TaskKeySub("NewAlias", "NewCode", updateTaskSub);
            this.key[selectedIndex].SubKey.Add(taskSub);
            // Item追加
            taskSub.Item.Add(new TaskItem("NewItemAlias", "NewItem", updateItem));
        }

        public void addTaskItem()
        {
            // ログ更新あり
            hasChangeLog = true;
            // Item追加
            this.key[selectedIndex].SubKey[selectedIndexSub].Item.Add(new TaskItem("NewItemAlias", "NewItem", updateItem));
        }

        private string newTaskElementDisp;
        public string NewTaskElementDisp
        {
            get { return newTaskElementDisp; }
            set
            {
                newTaskElementDisp = value;
                NotifyPropertyChanged(nameof(NewTaskElementDisp));
            }
        }
        private string cloneTaskElementDisp;
        public string CloneTaskElementDisp
        {
            get { return cloneTaskElementDisp; }
            set
            {
                cloneTaskElementDisp = value;
                NotifyPropertyChanged(nameof(CloneTaskElementDisp));
            }
        }

        private void UpdateTaskElementDisp()
        {
            switch (selectTaskClass)
            {
                case TaskClass.MainKey:
                    NewTaskElementDisp = "new Task";
                    CloneTaskElementDisp = "clone Task";
                    break;

                case TaskClass.SubKey:
                    NewTaskElementDisp = "new SubTask";
                    CloneTaskElementDisp = "clone SubTask";
                    break;

                case TaskClass.Item:
                    NewTaskElementDisp = "new Item";
                    CloneTaskElementDisp = "clone Item";
                    break;

                default:
                    // 想定外のコマンド
                    break;
            }
        }

        public void NewTaskElement()
        {
            switch (selectTaskClass)
            {
                case TaskClass.MainKey:
                    addTaskMain();
                    break;

                case TaskClass.SubKey:
                    addTaskSub();
                    break;

                case TaskClass.Item:
                    addTaskItem();
                    break;

                default:
                    // 想定外のコマンド
                    break;
            }
        }

        public void CloneTaskElement()
        {
            switch (selectTaskClass)
            {
                case TaskClass.MainKey:
                    CloneTaskElementMain();
                    break;

                case TaskClass.SubKey:
                    CloneTaskElementSub();
                    break;

                case TaskClass.Item:
                    CloneTaskElementItem();
                    break;

                default:
                    // 想定外のコマンド
                    break;
            }
        }
        private void CloneTaskElementMain()
        {
            // クローンを作成
            var elem = (TaskKey)key[selectedIndex].Clone();
            // クローンを追加
            key.Insert(selectedIndex + 1, elem);
        }
        private void CloneTaskElementSub()
        {
            // クローンを作成
            var elem = (TaskKeySub)key[selectedIndex].SubKey[selectedIndexSub].Clone();
            // クローンを追加
            key[selectedIndex].SubKey.Insert(selectedIndexSub + 1, elem);
        }
        private void CloneTaskElementItem()
        {
            // クローンを作成
            var elem = (TaskItem)key[selectedIndex].SubKey[selectedIndexSub].Item[selectedIndexItem].Clone();
            // クローンを追加
            key[selectedIndex].SubKey[selectedIndexSub].Item.Insert(selectedIndexItem + 1, elem);
        }

        private string baseCount;
        public string BaseCount
        {
            get { return baseCount; }
        }
        private string delayCount;
        public string DelayCount
        {
            get { return delayCount; }
        }

        public async Task TimerEllapse(int sec)
        {
            // sec秒経過
            this.timer.Ellapse(sec);
            // タスクへの計上判定
            if (this.timer.reqTaskCount)
            {
                var min = this.timer.PullMin();
                this.key[selectedIndex].SubKey[selectedIndexSub].Item[selectedIndexItem].TimerEllapse(min);
                this.key[selectedIndex].SubKey[selectedIndexSub].Item[selectedIndexItem].MakeDispTime();
                this.timer.CountRestart();
                this.SummaryAdd(min);
                // ログ更新あり
                hasChangeLog = true;
            }
            // 全体時間更新
            UpdateBaseCount();
            // delay時間更新
            UpdateDelayCount();
            // 自動保存処理
            await AutoSave();
        }

        public void TimerStart()
        {
            ticker.Start();
            this.timer.CountStart();
            buttonTimerOn = "タイマ停止";
            NotifyPropertyChanged(nameof(ButtonTimerOn));
        }

        public void TimerStop()
        {
            ticker.Stop();
            buttonTimerOn = "タイマ開始";
            NotifyPropertyChanged(nameof(ButtonTimerOn));
        }

        private void UpdateBaseCount()
        {
            baseCount = timer.BaseCountTime();
            NotifyPropertyChanged(nameof(BaseCount));
        }

        private void UpdateDelayCount()
        {
            // delay時間更新
            delayCount = timer.DelayCountTime();
            NotifyPropertyChanged(nameof(DelayCount));
        }

        private async Task AutoSave()
        {
            if (timer.ReqAutoSave())
            {
                // 自動セーブ時はファイルがロックされていたらスキップ
                var check = logger.CheckFileLock();
                if (check)
                {
                    await SaveLogAsync();
                }
            }
        }

        private async Task SaveLogAsync()
        {
            if (hasChangeLog)
            {
                // 自動保存要求とログ変化があれば
                // タスクなし または 完了済みなら自動保存する
                // running中のタスクがあるならスキップ
                if (logSaveTask == null || logSaveTask.IsCompleted)
                {
                    // ログ更新保存済み
                    hasChangeLog = false;
                    // UIスレッド内でログを転送
                    this.logger.Update(this.key);
                    // Taskにファイル書き込みを投げる
                    logSaveTask = logger.SaveAsync();
                    await logSaveTask;
                }
            }
        }


        public async Task OnClick_ManualSave()
        {
            // ログ更新保存済みならスキップ
            if (!hasChangeLog) return;

            // タスクなし または 完了済みならSetting/log手動出力
            // running中のタスクがあるならスキップ
            // 基本的にsettingはタスク終了時のみ保存するので問題ない。アンドで両方チェックしてしまう
            if ((logSaveTask == null || logSaveTask.IsCompleted) && (settingSaveTask == null || settingSaveTask.IsCompleted))
            {
                // GUI無効化
                EnableManualSave = false;
                if (AskManualSave())
                {
                    // ログ更新保存済み
                    hasChangeLog = false;
                    // ログ作成
                    // UIスレッド内でログを転送
                    if (Util.CheckTargetDateIsToday())
                    {
                        // 今日を対象としたkeyの変化のみ保存する
                        // 過去のタスクは保存しない
                        // 未来のタスクなんてわからないので保存する必要なし
                        this.settings.Update(this.key);
                    }
                    this.logger.Update(this.key);
                    // ログ保存
                    logSaveTask = ManualSaveAsync();
                    settingSaveTask = logSaveTask;
                    await logSaveTask;
                    // GUI有効化
                    EnableManualSave = true;
                }
                else
                {
                    EnableManualSave = true;
                }
            }
        }
        private bool AskManualSave()
        {
            // 手動保存対象のファイルがロックされていないかチェック
            // 手動保存ではファイルロックの解放を確認する
            {
                var result = settings.AskFileLock();
                if (!result) return false;
            }
            {
                var result = logger.AskFileLock();
                if (!result) return false;
            }
            return true;
        }
        public async Task ManualSaveAsync()
        {
            // ログ保存
            //await this.settings.SaveAsync();
            //await this.logger.SaveAsync();
            Func<Task> func = async () =>
            {
                await this.settings.SaveAsync();
                await this.logger.SaveAsync();
            };
            await Task.Run(func);
        }

        public bool enableManualSave = true;
        public bool EnableManualSave
        {
            get { return enableManualSave; }
            set
            {
                enableManualSave = value;
                NotifyPropertyChanged(nameof(EnableManualSave));
            }
        }


        public async Task OnClick_ManualSaveConfig()
        {
            // Config出力
            if (configSaveTask == null || configSaveTask.IsCompleted)
            {
                // アプリ終了時のファイル保存はファイルロックの対処を確認する
                var check = config.AskFileLock();
                if (check)
                {
                    configSaveTask = config.SaveAsync();
                    await configSaveTask;
                }
            }
        }



        private void SummaryAdd(int sec)
        {
            this.summary.Add(sec);
            timeSummary = Util.Min2Time(summary.timeAll);
            NotifyPropertyChanged(nameof(TimeSummary));
        }

        private string timeSummary;
        public string TimeSummary
        {
            get { return timeSummary; }
        }

        public ObservableCollection<SummaryNode> SummaryView
        {
            get { return summary.Data; }
        }

        public void MakeSummary(SummaryDispFormat form)
        {
            summary.Update(key, form);
            NotifyPropertyChanged(nameof(SummaryView));
        }

        public void InitSummary()
        {
            summary.ListClear();
            NotifyPropertyChanged(nameof(SummaryView));
        }

        public void OnMouseLeftButtonUp_Summary()
        {
            // index取得
            (int key, int subkey, int item) = summary.GetIndex(selectedIndexSummary);
            if (key >= 0)
            {
                //
                SelectedIndex = key;
                NotifyPropertyChanged(nameof(SelectedIndex));
                SelectedIndexSub = subkey;
                NotifyPropertyChanged(nameof(SelectedIndexSub));
                SelectedIndexItem = item;
                NotifyPropertyChanged(nameof(SelectedIndexItem));
            }

        }

        public async Task SaveSummary(SummarySaveFormat form)
        {
            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: SaveSummary START");
            // GUI無効化
            EnableSaveSummary1All = false;
            EnableSaveSummary1NonZero = false;
            EnableSaveSummary2All = false;
            EnableSaveSummary2NonZero = false;
            // ファイルロック確認
            var check = summary.AskFileLock(form);
            if (check)
            {
                // ログバッファ作成
                summary.MakeLog(key, form);
                // ログ保存
                // Summary保存
                //await summary.SaveAsync(form);
                await SaveSummaryAsync(form);
            }
            // GUI有効化
            EnableSaveSummary1All = true;
            EnableSaveSummary1NonZero = true;
            EnableSaveSummary2All = true;
            EnableSaveSummary2NonZero = true;
            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: SaveSummary FINISH");
        }
        public async Task SaveSummaryAsync(SummarySaveFormat form)
        {
            Func<Task> func = async () =>
            {
                // ログ保存
                // Summary保存
                await summary.SaveAsync(form);
            };
            await Task.Run(func);
        }

        public async Task OnClick_SaveSummary1All()
        {
            await SaveSummary(SummarySaveFormat.CodeNameSubAll);
        }

        public async Task OnClick_SaveSummary1NonZero()
        {
            await SaveSummary(SummarySaveFormat.CodeNameSubNonZero);
        }

        public async Task OnClick_SaveSummary2All()
        {
            await SaveSummary(SummarySaveFormat.CodeNameAliasSubItemAll);
        }

        public async Task OnClick_SaveSummary2NonZero()
        {
            await SaveSummary(SummarySaveFormat.CodeNameAliasSubItemNonZero);
        }

        public void OnClick_OpenSummaryDir()
        {
            summary.OpenOutDir();
        }

        public void OnClick_OpenLogDir()
        {
            logger.OpenOutDir();
        }

        public bool enableSaveSummary1All = true;
        public bool EnableSaveSummary1All
        {
            get { return enableSaveSummary1All; }
            set
            {
                enableSaveSummary1All = value;
                NotifyPropertyChanged(nameof(EnableSaveSummary1All));
            }
        }

        public bool enableSaveSummary1NonZero = true;
        public bool EnableSaveSummary1NonZero
        {
            get { return enableSaveSummary1NonZero; }
            set
            {
                enableSaveSummary1NonZero = value;
                NotifyPropertyChanged(nameof(EnableSaveSummary1NonZero));
            }
        }

        public bool enableSaveSummary2All = true;
        public bool EnableSaveSummary2All
        {
            get { return enableSaveSummary2All; }
            set
            {
                enableSaveSummary2All = value;
                NotifyPropertyChanged(nameof(EnableSaveSummary2All));
            }
        }

        public bool enableSaveSummary2NonZero = true;
        public bool EnableSaveSummary2NonZero
        {
            get { return enableSaveSummary2NonZero; }
            set
            {
                enableSaveSummary2NonZero = value;
                NotifyPropertyChanged(nameof(EnableSaveSummary2NonZero));
            }
        }

        public void OnButtonClick_TimerOn()
        {
            if (ticker.IsEnabled)
            {
                // 現在カウント中なら
                TimerStop();
            } 
            else
            {
                // 現在カウント中でないなら
                TimerStart();
            }
        }
        private string buttonTimerOn = "タイマ開始";
        public string ButtonTimerOn
        {
            get { return buttonTimerOn; }
        }


        public void SelectTaskView(TaskClass cls)
        {
            selectTaskClass = cls;
            UpdateTaskElementDisp();
        }

        public void TaskViewOpe(TaskViewOpe ope)
        {
            switch (selectTaskClass)
            {
                case TaskClass.MainKey:
                    taskViewOpeMain(ope);
                    break;

                case TaskClass.SubKey:
                    taskViewOpeSub(ope);
                    break;

                case TaskClass.Item:
                    taskViewOpeItem(ope);
                    break;

                default:
                    // 想定外のコマンド
                    break;
            }
        }
        private void taskViewOpeMain(TaskViewOpe ope)
        {
            switch (ope)
            {
                case TaskTimer.TaskViewOpe.Up:
                    //
                    if (selectedIndex > 0)
                    {
                        taskSwapMainKey(selectedIndex, selectedIndex - 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Down:
                    //
                    if (selectedIndex < key.Count - 1)
                    {
                        taskSwapMainKey(selectedIndex, selectedIndex + 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Delete:
                    // 必ず1つ以上のタスクが存在するようにチェック
                    if (key.Count > 1)
                    {
                        // 固定タスクは削除不可
                        if (!IsFixTask(key[selectedIndex].Alias))
                        {
                            taskDeleteMainKey(selectedIndex);
                        }
                        else
                        {
                            MessageBox.Show($"'{key[selectedIndex].Alias}' は固定タスクに指定されています");
                        }
                    }
                    break;
                default:
                    // 想定外のコマンド
                    break;
            }
        }
        private void taskViewOpeSub(TaskViewOpe ope)
        {
            switch (ope)
            {
                case TaskTimer.TaskViewOpe.Up:
                    //
                    if (selectedIndexSub > 0)
                    {
                        taskSwapSubKey(selectedIndexSub, selectedIndexSub - 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Down:
                    //
                    if (selectedIndexSub < key[selectedIndex].SubKey.Count - 1)
                    {
                        taskSwapSubKey(selectedIndexSub, selectedIndexSub + 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Delete:
                    // 必ず1つ以上のタスクが存在するようにチェック
                    if (key[selectedIndex].SubKey.Count > 1)
                    {
                        taskDeleteSubKey(selectedIndexSub);
                    }
                    break;
                default:
                    // 想定外のコマンド
                    break;
            }
        }
        private void taskViewOpeItem(TaskViewOpe ope)
        {
            switch (ope)
            {
                case TaskTimer.TaskViewOpe.Up:
                    //
                    if (selectedIndexItem > 0)
                    {
                        taskSwapItem(selectedIndexItem, selectedIndexItem - 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Down:
                    //
                    if (selectedIndexItem < key[selectedIndex].SubKey[selectedIndexSub].Item.Count - 1)
                    {
                        taskSwapItem(selectedIndexItem, selectedIndexItem + 1);
                    }
                    break;
                case TaskTimer.TaskViewOpe.Delete:
                    // 必ず1つ以上のタスクが存在するようにチェック
                    if (key[selectedIndex].SubKey[selectedIndexSub].Item.Count > 1)
                    {
                        taskDeleteItem(selectedIndexItem);
                    }
                    break;
                default:
                    // 想定外のコマンド
                    break;
            }
        }

        private void taskSwapMainKey(int curr, int tgt)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key.Move(curr, tgt);
            SelectedIndex = tgt;
        }
        private void taskDeleteMainKey(int curr)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key.RemoveAt(curr);
        }

        private void taskSwapSubKey(int curr, int tgt)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key[selectedIndex].SubKey.Move(curr, tgt);
            SelectedIndexSub = tgt;
        }
        private void taskDeleteSubKey(int curr)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key[selectedIndex].SubKey.RemoveAt(curr);
        }

        private void taskSwapItem(int curr, int tgt)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key[selectedIndex].SubKey[selectedIndexSub].Item.Move(curr, tgt);
            SelectedIndexItem = tgt;
        }
        private void taskDeleteItem(int curr)
        {
            // ログ更新あり
            hasChangeLog = true;
            // 
            key[selectedIndex].SubKey[selectedIndexSub].Item.RemoveAt(curr);
        }


        private bool IsFixTask(string taskAlias)
        {
            // Aliasが固定タスクに指定されているかどうか判定する
            // 固定タスクは削除不可
            return (config.FixTask.Length != 0 && config.FixTask.Equals(taskAlias));
        }


        private bool excelExportEnable = true;
        public bool ExcelExportEnable
        {
            get { return excelExportEnable; }
            set
            {
                excelExportEnable = value;
                NotifyPropertyChanged(nameof(ExcelExportEnable));
            }
        }
        public string ExcelPath
        {
            get
            {
                return config.ExcelPath;
            }
            set
            {
                config.ExcelPath = value;
            }
        }
        public async Task OnButtonClick_ExcelExport()
        {
            ExcelExportEnable = false;
            await ExcelCtrl.Export(config.ExcelPath, key, Util.TargetDate);
            ExcelExportEnable = true;
        }



        public string AutoPilotUrl
        {
            get
            {
                return config.AutoPilotUrl;
            }
            set
            {
                config.AutoPilotUrl = value;
            }
        }
        public string AutoPilotId
        {
            get
            {
                return config.AutoPilotId;
            }
            set
            {
                config.AutoPilotId = value;
            }
        }
        public string AutoPilotPassword
        {
            get
            {
                return config.AutoPilotPassword;
            }
            set
            {
                config.AutoPilotPassword = value;
            }
        }
        private bool webNavigateEnable = false;
        public bool WebNavigateEnable
        {
            get { return webNavigateEnable; }
            set
            {
                webNavigateEnable = value;
                NotifyPropertyChanged(nameof(WebNavigateEnable));
            }
        }

        public async Task OnButtonClick_EdgeCtrlLaunch()
        {
            bool result = await EdgeCtrl.Init();
            WebNavigateEnable = result;
        }
        public async Task OnButtonClick_EdgeCtrlNavigate()
        {
            WebNavigateEnable = false;
            await EdgeCtrl.Navigate(AutoPilotUrl, AutoPilotId, AutoPilotPassword);
            WebNavigateEnable = true;
        }


        private bool isWaiting = false;
        public bool IsWaiting
        {
            get { return isWaiting; }
            set {
                isWaiting = value;
                NotifyPropertyChanged(nameof(IsWaiting));
            }
        }

        public async Task StartWaiting()
        {
            IsWaiting = true;
            await Task.Delay(5000);
            IsWaiting = false;
        }
    }

    class TaskKey : ICloneable
    {
        private ObservableCollection<TaskKeySub> subkey;
        public ObservableCollection<TaskKeySub> SubKey
        {
            get { return subkey; }
            set { subkey = value; }
        }

        public string alias;
        public string Alias {
            get { return alias; }
            set
            {
                alias = value;
                _update?.Invoke();
            }
        }

        public string code;
        public string Code
        {
            get { return code; }
            set {
                code = value;
                _update?.Invoke();
            }
        }

        public string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                _update?.Invoke();
            }
        }

        private Action _update = null;

        public TaskKey(string alias, string code, string name, Action _main)
        {
            // SubKey初期値
            this.subkey = new ObservableCollection<TaskKeySub>();

            this.alias = alias;
            this.code = code;
            this.name = name;
            _update = _main;
        }

        public virtual object Clone()
        {
            // 自分のクローンを作成して返す
            var elem = new TaskKey(alias, code, name, _update);
            // DeepCopy
            foreach (var it in this.subkey)
            {
                elem.subkey.Add((TaskKeySub)it.Clone());
            }

            return elem;


            //return elem;
        }
    }

    class TaskKeySub : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        private ObservableCollection<TaskItem> item;
        public ObservableCollection<TaskItem> Item
        {
            get { return item; }
            set { item = value; }
        }

        public string alias;
        public string Alias
        {
            get { return alias; }
            set {
                alias = value;
                NotifyPropertyChanged(nameof(Alias));
            }
        }

        public string code;
        public string Code
        {
            get { return code; }
            set
            {
                code = value;
                NotifyPropertyChanged(nameof(Code));
            }
        }


        private Action _update = null;

        public TaskKeySub(string alias, string code, Action _update)
        {
            this.item = new ObservableCollection<TaskItem>();
            this.alias = alias;
            this.code = code;
            this._update = _update;
        }

        public virtual object Clone()
        {
            var elem = new TaskKeySub(alias, code, _update);
            // DeepCopy
            foreach (var it in this.item)
            {
                elem.item.Add((TaskItem)it.Clone());
            }

            return elem;
        }
    }

    class TaskItem : INotifyPropertyChanged, ICloneable
    {
        public string itemAlias;
        public string item;
        public int time;
        public string timeDisp;
        private Action<int, int> _updateTimeItem = null;

        public TaskItem(string alias, string item, Action<int, int> _item)
        {
            this.itemAlias = alias;
            this.item = item;
            this.time = 0;
            this._updateTimeItem = _item;
            this.MakeDispTime();
        }

        public virtual object Clone()
        {
            var elem = new TaskItem(itemAlias, item, _updateTimeItem);
            // DeepCopy
            // timeはコピーしない

            return elem;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        public string ItemAlias
        {
            get { return itemAlias; }
            set
            {
                itemAlias = value;
                NotifyPropertyChanged(nameof(ItemAlias));
            }
        }

        public string Item
        {
            get { return item; }
            set {
                item = value;
                NotifyPropertyChanged(nameof(Item));
            }
        }

        public int Time
        {
            get { return time; }
            set
            {
                time = value;
                MakeDispTime();
            }
        }
        public void TimerEllapse(int min)
        {
            time += min;
        }
        public void MakeDispTime()
        {
            timeDisp = Util.Min2Time(time);
            NotifyPropertyChanged(nameof(TimeDisp));
        }

        public string TimeDisp
        {
            get { return timeDisp; }
            set
            {
                Match match;
                match = Util.reTimeWithColon.Match(value);
                if (match.Success)
                {
                    try
                    {
                        // HH:MMをminに直す
                        GroupCollection groups = match.Groups;
                        int hour = Util.GetRegGroup2Min(groups[1]);
                        int min = Util.GetRegGroup2Min(groups[2]);
                        int oldtime = this.time;
                        this.time = hour * 60 + min;
                        _updateTimeItem(oldtime, this.time);
                        // 正常に解析出来たら文字列に反映
                        timeDisp = value;
                        NotifyPropertyChanged(nameof(TimeDisp));
                    }
                    catch
                    {
                        // 解析失敗で無視
                    }
                }
                else
                {
                    match = Util.reTimeWithoutColon.Match(value);
                    if (match.Success)
                    {
                        try
                        {
                            // HH:MMをminに直す
                            GroupCollection groups = match.Groups;
                            int hour = Util.GetRegGroup2Min(groups["hr"]);
                            int min = Util.GetRegGroup2Min(groups["min"]);
                            int oldtime = this.time;
                            this.time = hour * 60 + min;
                            _updateTimeItem(oldtime, this.time);
                            // 正常に解析出来たら文字列に反映
                            timeDisp = $"{hour:00}:{min:00}";
                            NotifyPropertyChanged(nameof(TimeDisp));
                        }
                        catch
                        {
                            // 解析失敗で無視
                        }
                    }
                    else
                    {
                        // NG文字列は無視
                        //timeDisp = value;
                    }
                }
            }
        }
    }

    class Timer
    {
        private int baseCounter;        // 全体経過時間
        public int taskCounter;         // タスク経過時間
        private int delayCounter;       // タスク切り替え後の正式に計上するまでの猶予時間
        private bool isFirstCount;      // カウント開始から一度もタスクに計上してない
        public bool reqTaskCount;       // タスクへの計上要求
        public int autosaveCounter;     // 自動保存カウンタ
        public bool reqAutoSave;        // 自動保存要求

        public Timer()
        {
            baseCounter = 0;
            taskCounter = 0;
            delayCounter = 0;
            isFirstCount = true;
            autosaveCounter = 0;
            reqAutoSave = false;
        }

        public void CountStart()
        {
            delayCounter = 0;
            isFirstCount = true;
            reqTaskCount = false;
            autosaveCounter = 0;
            reqAutoSave = false;
        }
        public void CountRestart()
        {
            // タスクに計上後、再スタート
            taskCounter = 0;
            delayCounter = 0;
            reqTaskCount = false;
        }
        public void CountDelay()
        {
            // タスク計上のdelay設定
            // 選択タスクを変更したとき、delayを最初からやり直す
            delayCounter = 0;
            reqTaskCount = false;
        }

        public void Ellapse(int sec)
        {
            baseCounter += sec;
            taskCounter += sec;
            delayCounter += sec;
            autosaveCounter += sec;

            if (isFirstCount)
            {
                // 初回は同じタスクのまま5分経過したら計上
                if (delayCounter >= 5 * Util.countDiv)
                {
                    reqTaskCount = true;
                    isFirstCount = false;
                }
            }
            else
            {
                // 2回目以降は同じタスクのまま1分経過したら計上
                if (delayCounter >= 1 * Util.countDiv)
                {
                    reqTaskCount = true;
                }
            }
            // 30分ごとに自動保存する
            if (autosaveCounter >= 30 * Util.countDiv)
            {
                autosaveCounter = 0;
                reqAutoSave = true;
            }
        }

        public int PullMin()
        {
            // 管理している経過時間から(分)を取り出して渡す
            // 経過時間(分)を計算
            int result = this.taskCounter / Util.countDiv;
            // 取り出した分をカウンタから差し引く
            this.taskCounter -= result * Util.countDiv;
            // 経過時間(分)を返す
            return result;
        }

        public bool ReqAutoSave()
        {
            var result = reqAutoSave;
            reqAutoSave = false;
            return result;
        }

        public string BaseCountTime()
        {
            return Util.Sec2Time(baseCounter);
        }

        public string DelayCountTime()
        {
            var delay = 0;
            if (isFirstCount)
            {
                delay = 5 * Util.countDiv - delayCounter;
            }
            else
            {
                delay = 1 * Util.countDiv - delayCounter;
            }
            return Util.Sec2Time(delay);
        }
    }

}
