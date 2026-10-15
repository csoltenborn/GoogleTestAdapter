using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    // DTE.Properties("Google Test Adapter", <page>) returns the pages via COM, which requires them (and their base
    // classes) to be COM visible; the assembly is not
    [ComVisible(true)]
    public abstract class NotifyingDialogPage : DialogPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}