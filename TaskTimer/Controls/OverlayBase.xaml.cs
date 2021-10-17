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

namespace TaskTimer.Controls
{
    /// <summary>
    /// OverlayBase.xaml の相互作用ロジック
    /// </summary>
    public partial class OverlayBase : UserControl
    {
        public OverlayBase()
        {
            // https://monakaice88.hatenablog.com/entry/2019/06/15/090115

            InitializeComponent();

            _vm = new OverlayBaseViewModel();
            this.DataContext = _vm;
        }

        private OverlayBaseViewModel _vm;

        public string OverlayTargetName { get; set; }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void Redraw()
        {
            Resize();
        }

        private void Resize()
        {
            // 表示しているコントロールの真ん中に表示したいので、親を辿り、ターゲットのサイズを取得する
            FrameworkElement parentElement = this.Parent as FrameworkElement;
            while (parentElement?.Parent != null && parentElement.Parent is FrameworkElement parent)
            {
                if (parent.Name == this.OverlayTargetName)
                {
                    _vm.Width = parent.ActualWidth;
                    _vm.Height = parent.ActualHeight;
                    break;
                }

                parentElement = parent;
            }
        }
    }
}
