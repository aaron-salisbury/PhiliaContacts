using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using PhiliaContacts.Core;
using PhiliaContacts.Core.Base;
using Windows.UI.Xaml;

namespace PhiliaContacts.App.ViewModels
{
    public class BaseViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
    {
        public AppLogger AppLogger => App.Current.Services.GetService<ShellViewModel>().AppLogger;

        public Manager Manager => App.Current.Services.GetService<ShellViewModel>().Manager;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                ProgressBarVisibility = _isBusy ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Visibility _progressBarVisibility = Visibility.Collapsed;
        public Visibility ProgressBarVisibility
        {
            get => _progressBarVisibility;
            set => SetProperty(ref _progressBarVisibility, value);
        }

        /// <summary>
        /// Tie a RelayCommand to a long running synchronous process that returns a bool indicating whether it completed successfully.
        /// </summary>
        /// <param name="longRunningFunction">Action that performs long running synchronous process.</param>
        /// <param name="taskCommand">The command that when raised triggers this work.</param>
        /// <returns></returns>
        public async Task InitiateProcessAsync(Func<bool> longRunningFunction, RelayCommand taskCommand, Action successAction = null, Action failureAction = null)
        {
            try
            {
                IsBusy = true;
                await InitiateProcess(longRunningFunction, successAction, failureAction).ConfigureAwait(false);
            }
            finally
            {
                Base.Helpers.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    IsBusy = false;
                    taskCommand.NotifyCanExecuteChanged();
                });
            }
        }

        private Task<bool> InitiateProcess(Func<bool> longRunningFunction, Action successAction, Action failureAction)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Task.Run(() =>
            {
                bool processIsSuccessful = false;

                Base.Helpers.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    processIsSuccessful = longRunningFunction.Invoke();

                    if (processIsSuccessful)
                    {
                        if (successAction != null)
                        {
                            successAction.Invoke();
                        }
                    }
                    else
                    {
                        if (failureAction != null)
                        {
                            failureAction.Invoke();
                        }
                    }
                });

                tcs.SetResult(processIsSuccessful);
            }).ConfigureAwait(false);

            return tcs.Task;
        }
    }
}
