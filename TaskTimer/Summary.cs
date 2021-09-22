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

        /** Summary作成
         * 
         */
        public void Update(ObservableCollection<TaskKey> TaskKeys)
        {
            // 要素分の領域を確保して初期化
            data = new ObservableCollection<SummaryNode>();
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        if (item.time != 0)
                        {
                            data.Add(new SummaryNode(key.Alias, item.Item, item.timeDisp));
                        }
                    }
                }
            }
        }
    }

    class SummaryNode
    {


        public SummaryNode(string alias, string item, string time)
        {
            this.alias = alias;
            this.item = item;
            this.time = time;
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
