
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace exDBF
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] String property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }


        public bool Set<T>(ref T field, T value)
        {
            if (Equals(field, value))
            {
                return false;
            }
            field = value;
            return true;
        }
    }
}
