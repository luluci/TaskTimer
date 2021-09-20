using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

using static Value;
// グローバル定数
static class Value
{
    public const int countDiv = 10;
}

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

        public Timer timer;
        private Action updateTaskMain;
        private Action updateTaskSub;
        public WindowViewModel()
        {
            // ChildからのNotify用コールバック
            updateTaskMain = () =>
            {
                _updateSelectTaskMain();
                NotifyPropertyChanged(nameof(SelectTask));
            };
            updateTaskSub = () =>
            {
                _updateSelectTaskSub();
                NotifyPropertyChanged(nameof(SelectTask));
            };

            // 
            timer = new Timer();
            baseCount = timer.BaseCountTime();

            //[Test]
            // テスト用初期値設定
            this.key = new ObservableCollection<TaskKey>();
            this.key.Add(new TaskKey("alias1", "code1", "name1", "1", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "2", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("エイリアス3", "コード3", "名前3", "3", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("長い名前の表示名４", "CODE4:AAAAAAAAA-AA", "長い名前のタスク名4", "4", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "5", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "6", updateTaskMain, updateTaskSub));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "7", updateTaskMain, updateTaskSub));

            // Subが最初から表示されてしまうので、初期状態で最初のタスクを選択しておく。
            this.SelectedIndex = 0;
            this.SelectedIndexSub = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        private string _selectTask;
        private string _selectTaskMain;
        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                //_updateSelectTaskMain();
                NotifyPropertyChanged(nameof(SelectedIndex));
                //NotifyPropertyChanged(nameof(SelectTask));
                // delay時間更新
                timer.CountDelay();
                UpdateDelayCount();
            }
        }

        private string _selectTaskSub;
        private int _selectedIndexSub;
        public int SelectedIndexSub
        {
            get { return _selectedIndexSub; }
            set
            {
                _selectedIndexSub = value;
                //_updateSelectTaskSub();
                NotifyPropertyChanged(nameof(SelectedIndexSub));
                //NotifyPropertyChanged(nameof(SelectTask));
                // delay時間更新
                timer.CountDelay();
                UpdateDelayCount();
            }
        }

        private void _updateSelectTaskMain()
        {
            //_selectTaskMain = key[_selectedIndex].code + " / " + key[_selectedIndex].name;
            _selectTaskMain = key[_selectedIndex].code + " / " + key[_selectedIndex].name + " / " + key[_selectedIndex].alias;
            _selectTask = _selectTaskMain + " " + _selectTaskSub;
        }

        private void _updateSelectTaskSub()
        {
            _selectTaskSub = "[" + key[_selectedIndex].SubKey[_selectedIndexSub].code + "]";
            _selectTask = _selectTaskMain + " " + _selectTaskSub;
        }
        
        public string SelectTask
        {
            get
            {
                return _selectTask;
            }
        }


        public void addTaskMain()
        {
            this.key.Add(new TaskKey("New", "New", "New", "8", updateTaskMain, updateTaskSub));
        }

        public void addTaskSub()
        {
            this.key[_selectedIndex].SubKey.Add(new TaskKeySub("New", "New", updateTaskSub));
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
            this.timer.Ellapse(1);
            // タスクへの計上判定
            if (this.timer.reqTaskCount)
            {
                var min = this.timer.taskCounter / countDiv;
                this.key[_selectedIndex].SubKey[_selectedIndexSub].TimerEllapse(min);
                this.key[_selectedIndex].SubKey[_selectedIndexSub].MakeDispTime();
                this.timer.CountRestart();
            }
            // 全体時間更新
            UpdateBaseCount();
            // delay時間更新
            UpdateDelayCount();
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

        public void TimerStart()
        {
            this.timer.CountStart();
        }

        public void TimerStop()
        {

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
        private Action _updateSelectTaskSub = null;

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

        public TaskKey(string alias, string code, string name, string id, Action _main, Action _sub)
        {
            // SubKey初期値
            this.subkey = new ObservableCollection<TaskKeySub>();
            this.subkey.Add(new TaskKeySub("sub" + id + "-1", "code_sub" + id + "-1", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-2", "code_sub" + id + "-2", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-3", "code_sub" + id + "-3", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-4", "code_sub" + id + "-4", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-5", "code_sub" + id + "-5", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-6", "code_sub" + id + "-6", _sub));
            this.subkey.Add(new TaskKeySub("sub" + id + "-7", "code_sub" + id + "-7", _sub));

            this.alias = alias;
            this.code = code;
            this.name = name;
            _updateSelectTaskMain = _main;
            _updateSelectTaskSub = _sub;
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

        private Action _updateSelectTaskSub = null;
        public int time;
        public string timeDisp;
        

        public string alias;
        public string Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        public string code;
        public string Code
        {
            get { return code; }
            set
            {
                code = value;
                //_updateSelectTaskSub?.Invoke();
                //NotifyPropertyChanged(nameof(Code));
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
            set { timeDisp = value; }
        }


        public TaskKeySub(string alias, string code, Action _sub)
        {
            this.alias = alias;
            this.code = code;
            _updateSelectTaskSub = _sub;
            this.time = 0;
            this.MakeDispTime();
        }
        
    }

    class Timer
    {
        private int baseCounter;    // 全体経過時間
        public int taskCounter;    // タスク経過時間
        private int delayCounter;   // タスク切り替え後の正式に計上するまでの猶予時間
        private bool isFirstCount;  // カウント開始から一度もタスクに計上してない
        public bool reqTaskCount;  // タスクへの計上要求

        public Timer()
        {
            baseCounter = 0;
            taskCounter = 0;
            delayCounter = 0;
            isFirstCount = true;
        }

        public void CountStart()
        {
            delayCounter = 0;
            isFirstCount = true;
            reqTaskCount = false;
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

            if (isFirstCount)
            {
                // 初回は同じタスクのまま5分経過したら計上
                if (delayCounter >= 5 * countDiv)
                {
                    reqTaskCount = true;
                    isFirstCount = false;
                }
            }
            else
            {
                // 2回目以降は同じタスクのまま1分経過したら計上
                if (delayCounter >= 1 * countDiv)
                {
                    reqTaskCount = true;
                }
            }
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
                delay = 5 * countDiv - delayCounter;
            }
            else
            {
                delay = 1 * countDiv - delayCounter;
            }
            var span = new TimeSpan(0, 0, delay);
            return span.ToString(@"hh\:mm\:ss");
        }
    }
}
