using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SCQueryConnect.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            int p = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        //private Dispatcher currentDispatcher;

        public void AlertPropertChanged(string property)
        {
            OnPropertyChanged(property);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                //check if we are on the UI thread if not switch
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string>(this.OnPropertyChanged), propertyName);
            }
        }

        public string AppNameOnly => $"SharpCloud QueryConnect v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public string AppName
        {
            get
            {
                if (IntPtr.Size == 4)
                    return $"{AppNameOnly} - 32Bit(x86)";
                return $"{AppNameOnly} - 64Bit(AnyCPU)";
            }
        }
    }
}
