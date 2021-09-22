using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

using TaskTimer;

namespace TaskTimer
{

    class WindowViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TaskKey> key;
        public ObservableCollection<TaskKey> Key
        {
            get { return key; }
            set { key = value; }
        }

        public Settings settings;
        public Logger logger;
        private Summary summary;
        public Timer timer;
        private Action updateTaskMain;
        private Action<int, int> updateItem;
        private Task autosaveTask = null;

        public WindowViewModel()
        {
            //
            settings = new Settings();
            settings.Load();
            //
            logger = new Logger();
            logger.Load();

            // ChildからのNotify用コールバック
            updateTaskMain = () =>
            {
                //_updateSelectTaskMain();
                //NotifyPropertyChanged(nameof(SelectTask));
            };
            updateItem = (int oldtime, int newtime) =>
            {
                //_updateSelectTaskSub();
                //NotifyPropertyChanged(nameof(SelectTask));
                int diff = newtime - oldtime;
                SummaryAdd(diff);
            };

            //
            summary = new Summary();
            SummaryAdd(0);

            // 
            timer = new Timer();
            baseCount = timer.BaseCountTime();

            LoadSettings();
            LoadLog();

            /*
            //[Test]
            // テスト用初期値設定
            this.key = new ObservableCollection<TaskKey>();
            this.key.Add(new TaskKey("alias1", "code1", "name1", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("エイリアス3", "コード3", "名前3", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("長い名前の表示名４", "CODE4:AAAAAAAAA-AA", "長い名前のタスク名4", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", updateTaskMain, updateTaskSub));
            */

            // Subが最初から表示されてしまうので、初期状態で最初のタスクを選択しておく。
            this.SelectedIndex = 0;
            this.SelectedIndexSub = 0;
            this.selectedIndexItem = 0;
        }

        private void LoadSettings()
        {
            // インスタンス作成
            this.key = new ObservableCollection<TaskKey>();
            // 設定ロード
            (string, string, string) mainCurrKey = ("", "", "");
            (string, string, string) mainNextKey;
            (string, string) subCurrKey = ("", "");
            (string, string) subNextKey;
            TaskKey taskMain = new TaskKey("", "", "", updateTaskMain);    // ダミーで初期化
            TaskKeySub taskSub = new TaskKeySub("", "");
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
                        taskSub.Item.Add(new TaskItem(keys.Item, updateItem));
                    }
                    else
                    {
                        // SubKeyが異なればオブジェクト追加
                        // SubKey追加
                        taskSub = new TaskKeySub(keys.SubAlias, keys.SubCode);
                        taskMain.SubKey.Add(taskSub);
                        // Item追加
                        taskSub.Item.Add(new TaskItem(keys.Item, updateItem));
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
                    taskSub = new TaskKeySub(keys.SubAlias, keys.SubCode);
                    taskMain.SubKey.Add(taskSub);
                    // Item追加
                    taskSub.Item.Add(new TaskItem(keys.Item, updateItem));
                    // MainKey更新
                    mainCurrKey = mainNextKey;
                    // SubKey更新
                    subCurrKey = subNextKey;
                }
            }
        }

        private void LoadLog()
        {
            if (logger.reqRestore)
            {
                // 復旧要求があれば
                int sum = logger.Restore(key);
                // 
                SummaryAdd(sum);
            }
        }

        public void Close()
        {
            // Setting出力
            {
                this.settings.Update(this.key);
                var task = this.settings.SaveAsync();
                task.Wait();
            }
            // ログ出力
            {
                this.logger.Update(this.key);
                var task = this.logger.SaveAsync();
                task.Wait();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        private string _selectTask;
        private string _selectTaskMain;
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

        private string _selectTaskSub;
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

        public void addTaskMain()
        {
            // 新しいタスク追加
            var newtask = new TaskKey("NewAlias", "NewCode", "NewName", updateTaskMain);
            this.key.Add(newtask);
            // テンプレート展開
            (string, string) subCurrKey = ("", "");
            (string, string) subNextKey;
            TaskKeySub taskSub = new TaskKeySub("", "");
            foreach (var subkey in settings.SubKeys)
            {
                subNextKey = (subkey.Code, subkey.Alias);
                // subKeyチェック
                if (subCurrKey.Equals(subNextKey))
                {
                    // SubKeyが同じならItem追加
                    taskSub.Item.Add(new TaskItem(subkey.Item, updateItem));
                }
                else
                {
                    // SubKeyが異なればオブジェクト追加
                    // SubKey追加
                    taskSub = new TaskKeySub(subkey.Alias, subkey.Code);
                    newtask.SubKey.Add(taskSub);
                    // Item追加
                    taskSub.Item.Add(new TaskItem(subkey.Item, updateItem));
                    // SubKey更新
                    subCurrKey = subNextKey;
                }
            }
        }

        public void addTaskSub()
        {
            // SubKey追加
            TaskKeySub taskSub = new TaskKeySub("NewAlias", "NewCode");
            this.key[selectedIndex].SubKey.Add(taskSub);
            // Item追加
            taskSub.Item.Add(new TaskItem("NewItem", updateItem));
        }

        public void addTaskItem()
        {
            this.key[selectedIndex].SubKey[selectedIndexSub].Item.Add(new TaskItem("NewItem", updateItem));
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

        public void TimerEllapse(int sec)
        {
            // sec秒経過
            this.timer.Ellapse(sec);
            // タスクへの計上判定
            if (this.timer.reqTaskCount)
            {
                var min = this.timer.taskCounter / Util.countDiv;
                this.key[selectedIndex].SubKey[selectedIndexSub].Item[selectedIndexItem].TimerEllapse(min);
                this.key[selectedIndex].SubKey[selectedIndexSub].Item[selectedIndexItem].MakeDispTime();
                this.timer.CountRestart();
                this.SummaryAdd(min);
            }
            // 全体時間更新
            UpdateBaseCount();
            // delay時間更新
            UpdateDelayCount();
            // 自動保存処理
            AutoSave();
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

        private void AutoSave()
        {
            if (timer.ReqAutoSave())
            {
                // 自動保存要求があれば
                // タスクなし または 完了済みなら自動保存する
                // running中のタスクがあるならスキップ
                if (autosaveTask == null || autosaveTask.IsCompleted)
                {
                    // UIスレッド内でログを転送
                    this.logger.Update(this.key);
                    // asyncにファイル書き込みを投げる
                    autosaveTask = this.logger.SaveAsync();
                }
            }
        }

        public void TimerStart()
        {
            this.timer.CountStart();
        }

        public void TimerStop()
        {

        }

        private void SummaryAdd(int sec)
        {
            this.summary.Add(sec);
            var span = new TimeSpan(0, summary.timeAll, 0);
            timeSummary = span.ToString(@"hh\:mm");
            NotifyPropertyChanged(nameof(TimeSummary));
        }

        private string timeSummary;
        public string TimeSummary
        {
            get { return timeSummary; }
        }

        public void OnButtonClick_TimerOn(bool isCounting)
        {
            if (isCounting)
            {
                // 現在カウント中なら
                buttonTimerOn = "タイマ停止";
            } 
            else
            {
                // 現在カウント中でないなら
                buttonTimerOn = "タイマ開始";
            }
            NotifyPropertyChanged(nameof(ButtonTimerOn));
        }
        private string buttonTimerOn = "タイマ開始";
        public string ButtonTimerOn
        {
            get { return buttonTimerOn; }
        }
    }

    class TaskKey
    {
        private ObservableCollection<TaskKeySub> subkey;
        public ObservableCollection<TaskKeySub> SubKey
        {
            get { return subkey; }
            set { subkey = value; }
        }

        private Action _updateSelectTaskMain = null;
        private Action<int, int> _updateSelectTaskSub = null;

        public string alias;
        public string Alias {
            get { return alias; }
            set
            {
                alias = value;
                //_updateSelectTaskMain?.Invoke();
            }
        }

        public string code;
        public string Code
        {
            get { return code; }
            set {
                code = value;
                //_updateSelectTaskMain?.Invoke();
            }
        }

        public string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                //_updateSelectTaskMain?.Invoke();
            }
        }

        public TaskKey(string alias, string code, string name, Action _main)
        {
            // SubKey初期値
            this.subkey = new ObservableCollection<TaskKeySub>();
            /*
            this.subkey.Add(new TaskKeySub("sub" + id + "-1", "code_sub" + id + "-1", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-2", "code_sub" + id + "-2", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-3", "code_sub" + id + "-3", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-4", "code_sub" + id + "-4", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-5", "code_sub" + id + "-5", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-6", "code_sub" + id + "-6", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-7", "code_sub" + id + "-7", _sub));
            */

            this.alias = alias;
            this.code = code;
            this.name = name;
            _updateSelectTaskMain = _main;
        }
    }

    class TaskKeySub : INotifyPropertyChanged
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

        /*
        private async Task test1()
        {
            await test2();
        }
        private async Task test2()
        {
            await Task.Delay(10 * 1000);
            System.Windows.MessageBox.Show("delayed！");
        }
        */

        public TaskKeySub(string alias, string code)
        {
            this.item = new ObservableCollection<TaskItem>();
            this.alias = alias;
            this.code = code;
        }
        
    }

    class TaskItem : INotifyPropertyChanged
    {
        public string item;
        public int time;
        public string timeDisp;
        private readonly Regex reTimeWithColon;
        private readonly Regex reTimeWithoutColon;
        private Action<int, int> _updateTimeItem = null;

        public TaskItem(string item, Action<int, int> _item)
        {
            this.item = item;
            this.time = 0;
            this._updateTimeItem = _item;
            this.MakeDispTime();
            this.reTimeWithColon = new Regex(@"^(\d+):(\d+)$", RegexOptions.Compiled);
            this.reTimeWithoutColon = new Regex(@"^(\d+)(\d\d)$", RegexOptions.Compiled);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        public string Item
        {
            get { return item; }
            set {
                item = value;
                NotifyPropertyChanged(nameof(Item));
            }
        }

        public void TimerEllapse(int min)
        {
            time += min;
        }
        public void MakeDispTime()
        {
            var span = new TimeSpan(0, time, 0);
            timeDisp = span.ToString(@"hh\:mm");
            NotifyPropertyChanged(nameof(TimeDisp));
        }

        public string TimeDisp
        {
            get { return timeDisp; }
            set
            {
                Match match;
                match = reTimeWithColon.Match(value);
                if (match.Success)
                {
                    try
                    {
                        // HH:MMをminに直す
                        GroupCollection groups = match.Groups;
                        int hour = int.Parse(groups[1].ToString());
                        int min = int.Parse(groups[2].ToString());
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
                    match = reTimeWithoutColon.Match(value);
                    if (match.Success)
                    {
                        try
                        {
                            // HH:MMをminに直す
                            GroupCollection groups = match.Groups;
                            int hour = int.Parse(groups[1].ToString());
                            int min = int.Parse(groups[2].ToString());
                            int oldtime = this.time;
                            this.time = hour * 60 + min;
                            _updateTimeItem(oldtime, this.time);
                            // 正常に解析出来たら文字列に反映
                            timeDisp = groups[1].ToString() + ":" + groups[2].ToString();
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

        public bool ReqAutoSave()
        {
            var result = reqAutoSave;
            reqAutoSave = false;
            return result;
        }

        public string BaseCountTime()
        {
            var span = new TimeSpan(0, 0, baseCounter);
            return span.ToString(@"hh\:mm\:ss");
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
            var span = new TimeSpan(0, 0, delay);
            return span.ToString(@"hh\:mm\:ss");
        }
    }

    class Summary
    {
        public int timeAll;

        public Summary()
        {
            timeAll = 0;
        }

        public void Add(int min)
        {
            timeAll += min;
        }
    }
}
