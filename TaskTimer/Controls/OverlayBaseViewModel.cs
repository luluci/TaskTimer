using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTimer.Controls
{
    class OverlayBaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            var e = new PropertyChangedEventArgs(PropertyName);
            PropertyChanged?.Invoke(this, e);
        }

        public OverlayBaseViewModel()
        {

        }

        private string fixedMessage;
        public string FixedMessage
        {
            get { return fixedMessage; }
            set { fixedMessage = value; }
        }

        private double width = 10.0;
        public double Width
        {
            get { return width; }
            set {
                width = value;
                NotifyPropertyChanged(nameof(Width));
            }
        }
        private double height = 10.0;
        public double Height
        {
            get { return height; }
            set {
                height = value;
                NotifyPropertyChanged(nameof(Height));
            }
        }
    }
}
