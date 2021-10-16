using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;

namespace TaskTimer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        WindowViewModel vm;

        private bool isClosing = false;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                this.vm = new WindowViewModel();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"アプリ起動で例外発生：{ex.Message}\r\nアプリを終了します。");
                Application.Current.Shutdown();
                return;
            }

            this.DataContext = this.vm;
            
            this.Closing += async (s, e) =>
            {
                if (isClosing)
                {
                    // 終了処理中は終了させない
                    e.Cancel = true;
                }
                else
                {
                    try
                    {
                        // 終了処理開始
                        isClosing = true;
                        e.Cancel = true;
                        // 終了イベントはキャンセルして、アプリ終了時処理を開始
                        await vm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    finally
                    {
                        // 終了処理完了でアプリ終了ブロックを解除、改めてアプリ終了を開始
                        isClosing = false;
                        Application.Current.Shutdown();
                    }
                }
            };

            FocusManager.SetIsFocusScope(this, true);
        }
        
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox txt = (TextBox)((Grid)((TextBlock)sender).Parent).Children[1];
            txt.Visibility = Visibility.Visible;
            txt.Focus();
            e.Handled = true;
            ((TextBlock)sender).Visibility = Visibility.Collapsed;
        }

        private void TextBlock_SelectAll_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox txt = (TextBox)((Grid)((TextBlock)sender).Parent).Children[1];
            // テキストボックス選択時に全範囲選択したいとき
            // MouseLeftButtonDownイベントがキャレットを動かして解除してしまうので、
            // マウスイベントをハンドリング済みにすることで防ぐ
            txt.Visibility = Visibility.Visible;
            txt.Focus();
            txt.SelectAll();
            e.Handled = true;
            ((TextBlock)sender).Visibility = Visibility.Collapsed;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBlock tb = (TextBlock)((Grid)((TextBox)sender).Parent).Children[0];
            tb.Visibility = Visibility.Visible;
            ((TextBox)sender).Visibility = Visibility.Collapsed;
        }

        private void Button_AddTask_Click(object sender, RoutedEventArgs e)
        {
            this.vm.addTaskMain();
        }

        private void Button_AddSubTask_Click(object sender, RoutedEventArgs e)
        {
            this.vm.addTaskSub();
        }

        private void Button_Timer_Click(object sender, RoutedEventArgs e)
        {
            this.vm.OnButtonClick_TimerOn();
        }

        private void Button_AddItem_Click(object sender, RoutedEventArgs e)
        {
            this.vm.addTaskItem();
        }

        private void Button_MakeSummary1_Click(object sender, RoutedEventArgs e)
        {
            this.vm.MakeSummary(SummaryDispFormat.AliasItemCode);
        }

        private void Button_MakeSummary2_Click(object sender, RoutedEventArgs e)
        {
            this.vm.MakeSummary(SummaryDispFormat.CodeNameCode);
        }

        private void DataGrid_Summary_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: DataGrid_Summary_MouseLeftButtonUp START");
            this.vm.OnMouseLeftButtonUp_Summary();
            //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: DataGrid_Summary_MouseLeftButtonUp FINISH");
        }

        private async void Button_SaveSummary1All_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClick_SaveSummary1All();
        }

        private async void Button_SaveSummary1NonZero_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClick_SaveSummary1NonZero();
        }

        private async void Button_SaveSummary2All_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClick_SaveSummary2All();
        }

        private async void Button_SaveSummary2NonZero_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnClick_SaveSummary2NonZero();
        }

        private void Button_OpenSummaryDir_Click(object sender, RoutedEventArgs e)
        {
            vm.OnClick_OpenSummaryDir();
        }

        private void Button_OpenLogDir_Click(object sender, RoutedEventArgs e)
        {
            vm.OnClick_OpenLogDir();
        }

        private void Button_ManualSave_Click(object sender, RoutedEventArgs e)
        {
            vm.OnClick_ManualSave();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tb = sender as TextBox;
                //tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                //FocusManager.SetIsFocusScope(root, true);
                FocusManager.SetFocusedElement(this, root);
                //System.Windows.Input.Keyboard.ClearFocus();
            }
        }

        private void Button_TaskViewOpeUp_Click(object sender, RoutedEventArgs e)
        {
            vm.TaskViewOpe(TaskViewOpe.Up);
        }

        private void Button_TaskViewOpeDown_Click(object sender, RoutedEventArgs e)
        {
            vm.TaskViewOpe(TaskViewOpe.Down);
        }

        private void Button_TaskViewOpeDelete_Click(object sender, RoutedEventArgs e)
        {
            vm.TaskViewOpe(TaskViewOpe.Delete);
        }

        private void TaskMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //vm.SelectTaskView(TaskClass.MainKey);
        }

        private void TaskSub_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //vm.SelectTaskView(TaskClass.SubKey);
        }

        private void TaskItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //vm.SelectTaskView(TaskClass.Item);
        }

        private void ListView_TaskMain_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            vm.SelectTaskView(TaskClass.MainKey);
        }

        private void ListBox_TaskSub_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            vm.SelectTaskView(TaskClass.SubKey);
        }

        private void ListView_TaskItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            vm.SelectTaskView(TaskClass.Item);
        }

        private void ListView_TaskMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, root);
        }

        private void ListBox_TaskSub_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, root);
        }

        private void ListView_TaskItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, root);
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.TargetDateChanged();
        }

        private void Button_SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            vm.OnClick_ManualSaveConfig();
        }

        private async void Button_Web_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnButtonClick_EdgeCtrlLaunch();
        }

        private async void Button_WebNavigate_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnButtonClick_EdgeCtrlNavigate();
        }

        private async void Button_ExcelExport_Click(object sender, RoutedEventArgs e)
        {
            await vm.OnButtonClick_ExcelExport();
        }
    }
}
