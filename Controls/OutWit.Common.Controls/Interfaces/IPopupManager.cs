using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OutWit.Common.Controls.Interfaces
{
    public interface IPopupManager
    {
        void ShowSnackBarInfo(string message);

        public void CloseDialog(object parameter);

        Task<TResult> ShowPrompt<TResult>(Control view);
        Task<TResult> ShowPrompt<TResult>(Control view, DialogOpenedEventHandler onOpenDialog);

        public Task<TEnum> ShowError<TEnum, TConverter>(string header, params string[] lines)
            where TEnum : Enum
            where TConverter : StringToResourceConverterBase, new();

        public Task<TEnum> ShowInfo<TEnum, TConverter>(string header, params string[] lines)
            where TEnum : Enum
            where TConverter : StringToResourceConverterBase, new();

        public Task<TResult> RunLongProcess<TResult, TConverter>(string header, ProcessDelegate<TResult> process)
            where TConverter : StringToResourceConverterBase, new();

        public Task<TResult> RunLongProcess<TResult, TConverter>(string header, string button, ProgressDelegate<TResult> process)
            where TConverter : StringToResourceConverterBase, new();

        public Task<TResult> RunLongProcess<TResult, TConverter>(string header, ProgressDelegate<TResult> process)
            where TConverter : StringToResourceConverterBase, new();
    }

    public delegate TResult ProcessDelegate<out TResult>();
    public delegate TResult ProgressDelegate<out TResult>(Action<double> progress, Func<bool> isCancelled);
}
