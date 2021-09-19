﻿using System;
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

namespace TaskTimer
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        WindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            this.vm = new WindowViewModel();

            this.DataContext = this.vm;

            InitTimer();
        }


        // ストップウォッチ制御
        bool IsTimerCounting = false;
        DispatcherTimer timer;
        private void InitTimer()
        {
            // 1秒基準でカウント
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);

            // 終了時にタイマを止める
            this.Closing += (s, e) => timer.Stop();
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            // 1秒経過を通知
            this.vm.TimerEllapse(1);
        }
        private void StartTimer()
        {
            timer.Start();
        }
        private void StopTimer()
        {
            timer.Stop();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox txt = (TextBox)((Grid)((TextBlock)sender).Parent).Children[1];
            txt.Visibility = Visibility.Visible;
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
            if (IsTimerCounting)
            {
                IsTimerCounting = false;
                StopTimer();
            }
            else
            {
                IsTimerCounting = true;
                StartTimer();
            }
        }
    }
}
