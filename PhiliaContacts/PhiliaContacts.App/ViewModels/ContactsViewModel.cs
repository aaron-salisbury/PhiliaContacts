﻿using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using PhiliaContacts.Core;
using PhiliaContacts.Domains;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace PhiliaContacts.App.ViewModels
{
    public class ContactsViewModel : BaseViewModel
    {
        public Action WorkflowSuccessAction { get; set; }
        public Action WorkflowFailureAction { get; set; }

        public RelayCommand NewContactCommand { get; }
        public RelayCommand DeleteContactCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand SaveCommand { get; }

        private Contact _selected;
        public Contact Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ContactsViewModel()
        {
            Selected = Manager.Contacts?.First();

            NewContactCommand = new RelayCommand(() => AddNewContact());
            DeleteContactCommand = new RelayCommand(() => DeleteContact());
            ImportCommand = new RelayCommand(async () => await LaunchFilePickerAndImportAsync(), () => !IsBusy);
            SaveCommand = new RelayCommand(async () => await InitiateProcessAsync(Manager.Save, SaveCommand, WorkflowSuccessAction, WorkflowFailureAction), () => !IsBusy);
        }

        private void AddNewContact()
        {
            Contact newContact = new Contact
            {
                Photo = Manager.DefaultContactImage
            };

            Manager.Contacts.Add(newContact);

            Selected = newContact;
        }

        private void DeleteContact()
        {
            if (Selected != null)
            {
                int selectedIndex = Manager.Contacts.IndexOf(Selected);
                int newIndex = selectedIndex == (Manager.Contacts.Count - 1) ? selectedIndex - 1 : selectedIndex;

                Manager.Contacts.Remove(Selected);

                Selected = newIndex >= 0 ? Manager.Contacts[newIndex] : null;
            }
        }

        private async Task<bool> LaunchFilePickerAndImportAsync()
        {
            bool processIsSuccessful = false;

            FileOpenPicker picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            picker.FileTypeFilter.Add(".vcf");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    IsBusy = true;

                    IBuffer buffer = await FileIO.ReadBufferAsync(file);

                    using (DataReader dataReader = DataReader.FromBuffer(buffer))
                    {
                        if (await ImportContacts(dataReader.ReadString(buffer.Length)).ConfigureAwait(false))
                        {
                            processIsSuccessful = true;
                        }
                    }
                }
                finally
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        IsBusy = false;
                        ImportCommand.RaiseCanExecuteChanged();
                        Selected = Manager.Contacts.FirstOrDefault();
                    });
                }
            }

            return processIsSuccessful;
        }

        private Task<bool> ImportContacts(string vCardContents)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Task.Run(() =>
            {
                bool processIsSuccessful = false;

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    processIsSuccessful = Importer.Import(Manager, vCardContents);

                    if (processIsSuccessful)
                    {
                        if (WorkflowSuccessAction != null)
                        {
                            WorkflowSuccessAction.Invoke();
                        }
                    }
                    else
                    {
                        if (WorkflowFailureAction != null)
                        {
                            WorkflowFailureAction.Invoke();
                        }
                    }
                });

                tcs.SetResult(processIsSuccessful);
            }).ConfigureAwait(false);

            return tcs.Task;
        }
    }
}