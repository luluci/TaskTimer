using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TaskTimer
{
    using ExportDataNode = Dictionary<(string subcode, string subalias), int>;
    using ExportData = Dictionary<(string code, string name, string alias), Dictionary<(string subcode, string subalias), int>>;

    class ExcelCtrl
    {
        public ExcelCtrl()
        {
            // https://github.com/ClosedXML/ClosedXML/wiki
        }


        public async Task Export(string path, ObservableCollection<TaskKey> TaskKeys, DateTime time)
        {
            // 現データをバッファに展開
            var data = MakeExportData(TaskKeys);
            // Excelに書き出し
            await Task.Run(() =>
            {
                ExportExcel(path, data, time);
            });
        }

        private ExportData MakeExportData(ObservableCollection<TaskKey> TaskKeys)
        {
            var data = new ExportData();

            (string code, string name, string alias) key1;
            (string subcode, string subalias) key2;
            // 最新のタスク設定を取得
            foreach (var key in TaskKeys)
            {
                key1 = (key.Code, key.Name, key.Alias);
                foreach (var subkey in key.SubKey)
                {
                    foreach (var item in subkey.Item)
                    {
                        // 各コードとアイテムの対応付けを調整する場合はここでkeyの作り方を変える
                        key2 = (subkey.Code, item.Item);

                        // ゼロ以外だけコンテナに入れればいい
                        if (item.time != 0)
                        {
                            // MainKeyを登録
                            ExportDataNode itemDict;
                            if (data.ContainsKey(key1))
                            {
                                itemDict = data[key1];
                            }
                            else
                            {
                                itemDict = new ExportDataNode();
                                data.Add(key1, itemDict);
                            }
                            // SubKeyを登録
                            if (itemDict.ContainsKey(key2))
                            {
                                itemDict[key2] += item.Time;
                            }
                            else
                            {
                                itemDict.Add(key2, item.Time);
                            }
                        }
                    }
                }
            }

            return data;
        }

        private void ExportExcel(string path, ExportData data, DateTime time)
        {
            try
            {
                using (var workbook = new XLWorkbook(path))
                {
                    foreach (var node in data)
                    {
                        // 対象タスクのシートが存在するかチェック
                        IXLWorksheet worksheet;
                        if (workbook.Worksheets.Contains(node.Key.alias))
                        {
                            worksheet = workbook.Worksheet(node.Key.alias);
                        }
                        else
                        {
                            // 存在しない場合
                            worksheet = workbook.Worksheet("base").CopyTo(node.Key.alias);
                        }
                        // worksheetに展開
                        ExportExcelSheet(worksheet, node.Value, time);
                    }

                    workbook.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {

            }
        }

        private void ExportExcelSheet(IXLWorksheet worksheet, ExportDataNode node, DateTime time)
        {
            // worksheetに展開
            IXLCell cell;
            int row, col;
            // 日時選択
            const int DateCellBeginRow = 5;
            const int DateCellBeginCol = 3;
            row = DateCellBeginRow;
            col = DateCellBeginCol;
            // ログ日時がセルに存在するかチェック
            while (!worksheet.Cell(row, col).IsEmpty())
            {
                cell = worksheet.Cell(row, col);
                var date = cell.GetValue<DateTime>();
                if (time.Date.Equals(date.Date))
                {
                    break;
                }

                col++;
            }
            if (worksheet.Cell(row, col).IsEmpty())
            {
                // ここでemptyだとセルに存在しなかったので作成
                cell = worksheet.Cell(row, col);
                cell.Value = time.Date;
                cell.Style.DateFormat.Format = "MM/DD";
            }
            // ログデータを展開
            const int LogCellBeginRow = 6;
            const int LogSubCodeCol = 1;
            const int LogItemCol = 2;
            string subcode = "";
            string item = "";
            row = LogCellBeginRow;
            while (!worksheet.Cell(row, LogItemCol).IsEmpty())
            {
                // Code/Item取得
                cell = worksheet.Cell(row, LogSubCodeCol);
                if (!cell.IsEmpty())
                {
                    subcode = cell.GetValue<string>();
                }
                cell = worksheet.Cell(row, LogItemCol);
                if (!cell.IsEmpty())
                {
                    item = cell.GetValue<string>();
                }
                // ログ展開
                if (node.TryGetValue((subcode, item), out int value))
                {
                    cell = worksheet.Cell(row, col);
                    cell.Value = value;
                }

                row++;
            }
        }
    }
}
