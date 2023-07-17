using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MakeReadyWpf.Helpers
{
    public class StaTask
    {
        public static Task<T> RunStaTask<T>(Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(function());
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static Task RunStaTask(Action function)
        {
            var tcs = new TaskCompletionSource<int>();
            var thread = new Thread(() =>
            {
                try
                {
                    function();
                    tcs.SetResult(0);
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
