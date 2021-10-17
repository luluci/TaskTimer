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
using System.Windows.Threading;

namespace TaskTimer.Controls
{
    /// <summary>
    /// WaitingOverlay.xaml の相互作用ロジック
    /// </summary>
    public partial class WaitingOverlay
    {
        public WaitingOverlay()
        {
            InitializeComponent();
            this.IsVisibleChanged += (s, e) =>
            {
                // 表示・非表示切替時にViewの位置が悪い。Viewのサイズ計算処理の動作するタイミングが悪いっぽい
                // 表示状態が切り替わったときに、微妙に遅らせて、再描画処理を呼び出す。
                // 参考サイト：http://geekswithblogs.net/ilich/archive/2012/10/16/running-code-when-windows-rendering-is-completed.aspx
                this.Dispatcher.BeginInvoke(
                    new Action(() => this.SubView.Redraw()),
                    DispatcherPriority.ContextIdle
                );
            };
        }

        public static readonly DependencyProperty OverlayTargetNameProperty = DependencyProperty.Register(
            "OverlayTargetName",
            typeof(string),
            typeof(WaitingOverlay),
            new FrameworkPropertyMetadata(null, OnOverlayTargetNameChanged)
            );

        public static readonly DependencyProperty FixedMessageProperty = DependencyProperty.Register(
            "FixedMessage",
            typeof(string),
            typeof(WaitingOverlay),
            new FrameworkPropertyMetadata("Waiting", OnFixedMessageChanged)
            );


        public string OverlayTargetName
        {
            get { return (string)GetValue(OverlayTargetNameProperty); }
            set { SetValue(OverlayTargetNameProperty, value); }
        }

        public string FixedMessage
        {
            get { return (string)GetValue(FixedMessageProperty); }
            set { SetValue(FixedMessageProperty, value); }
        }

        private static void OnOverlayTargetNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is WaitingOverlay myView)
            {
                myView.SubView.OverlayTargetName = myView.OverlayTargetName;
            }
        }

        private static void OnFixedMessageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is WaitingOverlay myView)
            {
                ((OverlayBaseViewModel)myView.SubView.DataContext).FixedMessage = myView.FixedMessage;
            }
        }
    }
}
