using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Base
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool? _longRunningProcessSuccessful = null;

        public virtual void AddModelEvents() { }

        public virtual void RemoveModelEvents() { }

        /// <summary>
        /// Execute long running asynchronous process without locking the UI thread.
        /// </summary>
        /// <param name="longRunningFunction">Function to execute. If its process fails, it needs to return false or null.</param>
        internal async Task<T> InitiateLongRunningProcessAsync<T>(Func<Task<T>> longRunningFunction)
        {
            try
            {
                IsBusy = true;

                // Run the asynchronous function
                T result = await longRunningFunction();

                // Update the success flag
                LongRunningProcessSuccessful = (result is bool boolResult) ? boolResult : result != null;

                return result;
            }
            finally
            {
                IsBusy = false;
                LongRunningProcessSuccessful = null;
            }
        }

        /// <summary>
        /// Execute long running synchronous process without locking the UI thread.
        /// </summary>
        /// <param name="longRunningFunction">Function to execute. If its process fails, it needs to return false or null.</param>
        internal async Task<T> InitiateLongRunningProcessAsync<T>(Func<T> longRunningFunction)
        {
            return await InitiateLongRunningProcessAsync(() => Task.Run(longRunningFunction));
        }
    }
}
