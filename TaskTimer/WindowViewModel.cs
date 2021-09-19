using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public WindowViewModel()
        {

            //[Test]
            // テスト用初期値設定
            this.key = new ObservableCollection<TaskKey>();
            this.key.Add(new TaskKey("alias1", "code1", "name1", "1"));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "2"));
            this.key.Add(new TaskKey("エイリアス3", "コード3", "名前3", "3"));
            this.key.Add(new TaskKey("長い名前の表示名４", "CODE4:AAAAAAAAA-AA", "長い名前のタスク名4", "4"));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "5"));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "6"));
            this.key.Add(new TaskKey("alias2", "code2", "name2", "7"));

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
                //_selectTaskMain = key[value].code + " / " + key[value].name;
                _selectTaskMain = key[value].code + " / " + key[value].name + " / " + key[value].alias;
                _selectTask = _selectTaskMain + " " + _selectTaskSub;
                NotifyPropertyChanged(nameof(SelectedIndex));
                NotifyPropertyChanged(nameof(SelectTask));
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
                _selectTaskSub = "[" + key[_selectedIndex].SubKey[value].code + "]";
                _selectTask = _selectTaskMain + " " + _selectTaskSub;
                NotifyPropertyChanged(nameof(SelectedIndexSub));
                NotifyPropertyChanged(nameof(SelectTask));
            }
        }
        
        public string SelectTask
        {
            get
            {
                return _selectTask;
            }
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

        public string alias;
        public string code;
        public string name;

        public string Alias {
            get { return alias; }
            set
            {
                alias = value;
            }
        }

        public TaskKey(string alias, string code, string name, string id)
        {
            // SubKey初期値
            this.subkey = new ObservableCollection<TaskKeySub>();
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-1", "code_sub" + id + "-1"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-2", "code_sub" + id + "-2"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-3", "code_sub" + id + "-3"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-4", "code_sub" + id + "-4"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-5", "code_sub" + id + "-5"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-6", "code_sub" + id + "-6"));
            this.subkey.Add(new TaskKeySub("alias_sub" + id + "-7", "code_sub" + id + "-7"));

            this.alias = alias;
            this.code = code;
            this.name = name;
        }
        
    }

    class TaskKeySub
    {
        public string alias;
        public string code;

        public string Alias
        {
            get { return alias; }
            set { alias = value; }
        }


        public TaskKeySub(string alias, string code)
        {
            this.alias = alias;
            this.code = code;
        }
        
    }
}
