using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace TaskTimer
{
    class Summary
    {
        public int timeAll;

        public Summary()
        {
            timeAll = 0;
            data = new ObservableCollection<SummaryNode>();
        }

        public void Add(int min)
        {
            timeAll += min;
        }

        private ObservableCollection<SummaryNode> data;
        public ObservableCollection<SummaryNode> Data
        {
            get { return data; }
            set { data = value; }
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
            }
        }

        /** Summary作成
         * 
         */
        public void Update(ObservableCollection<TaskKey> TaskKeys)
        {
            // 要素分の領域を確保して初期化
            data = new ObservableCollection<SummaryNode>();
            // 最新のタスク設定を取得
            int keyIndex = 0;
            foreach (var key in TaskKeys)
            {
                int subkeyIndex = 0;
                foreach (var subkey in key.SubKey)
                {
                    int itemIndex = 0;
                    foreach (var item in subkey.Item)
                    {
                        if (item.time != 0)
                        {
                            data.Add(new SummaryNode(key.Alias, item.Item, item.timeDisp, keyIndex, subkeyIndex, itemIndex));
                        }

                        itemIndex++;
                    }

                    subkeyIndex++;
                }

                keyIndex++;
            }
        }

        public (int, int, int) GetIndex(int idx)
        {
            return data[idx].GetIndex();
        }
    }

    class SummaryNode
    {
        private int keyIndex;
        private int subkeyIndex;
        private int itemIndex;

        public SummaryNode(string alias, string item, string time, int keyIndex, int subkeyIndex, int itemIndex)
        {
            this.alias = alias;
            this.item = item;
            this.time = time;
            this.keyIndex = keyIndex;
            this.subkeyIndex = subkeyIndex;
            this.itemIndex = itemIndex;
        }

        public (int,int,int) GetIndex()
        {
            return (keyIndex, subkeyIndex, itemIndex);
        }

        private string alias;
        public string Alias
        {
            get { return alias; }
        }

        private string item;
        public string Item
        {
            get { return item; }
        }

        private string time;
        public string Time
        {
            get { return time; }
        }
    }
}
