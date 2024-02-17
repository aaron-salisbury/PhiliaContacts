using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using PhiliaContacts.Business.Models;
using PhiliaContacts.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Base.Controls
{
    public partial class RibbonControl : UserControl
    {
        private ContactsViewModel? _viewModel;
        private Animation _workflowAnimation;

        public RibbonControl()
        {
            InitializeComponent();

            _viewModel = (ContactsViewModel?)App.Current?.Services?.GetService(typeof(ContactsViewModel));
            this.DataContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
            }

            _workflowAnimation = CreateWorkflowAnimation();
        }

        private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ContactsViewModel)
            {
                switch (e.PropertyName)
                {
                    case nameof(ContactsViewModel.LongRunningProcessSuccessful):
                        HandleWorkflowComplete();
                        break;
                }
            }
        }

        private void ContactSearchBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is AutoCompleteBox contactSearchBox && contactSearchBox.SelectedItem is string selectedFormattedName && _viewModel != null)
            {
                Contact? contact = _viewModel.Contacts.Where(c => string.Equals(selectedFormattedName, c.FormattedName)).FirstOrDefault();

                if (contact != null)
                {
                    _viewModel.SelectedContact = contact;
                }
            }
        }

        private async void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ContactsViewModel viewModel)
            {
                IStorageFile? file = await GetVirtualContactFileStorageAsync();

                if (file != null)
                {
                    await using var stream = await file.OpenWriteAsync();
                    await viewModel.ExportAsync(stream);
                }
            }
        }

        private async void ImportBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ContactsViewModel viewModel)
            {
                IStorageFile? file = await GetVirtualContactFileAsync();

                if (file != null)
                {
                    await using var stream = await file.OpenReadAsync();
                    await viewModel.ImportAsync(stream);
                }
            }
        }

        private async Task<IStorageFile?> GetVirtualContactFileStorageAsync()
        {
            IStorageFile? file = null;
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel != null)
            {
                file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Virtual Contact File",
                    SuggestedFileName = "PhiliaContacts",
                    DefaultExtension = ".vcf",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = new List<FilePickerFileType>() { new FilePickerFileType("Virtual Contact File") { Patterns = new[] { "*.vcf" } } },
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads)
                });
            }

            return file;
        }

        private async Task<IStorageFile?> GetVirtualContactFileAsync()
        {
            IStorageFile? file = null;
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel != null)
            {
                IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Virtual Contact File",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>() { new FilePickerFileType("Virtual Contact File") { Patterns = new[] { "*.vcf" } } },
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads)
                });

                if (files.Count >= 1)
                {
                    file = files[0];
                }
            }

            return file;
        }

        private void HandleWorkflowComplete()
        {
            if (_viewModel != null && _viewModel.LongRunningProcessSuccessful != null)
            {
                if (_viewModel.LongRunningProcessSuccessful.Value)
                {
                    _workflowAnimation.RunAsync(WorkflowSucessIcon);
                }
                else
                {
                    _workflowAnimation.RunAsync(WorkflowFailureIcon);
                }
            }
        }

        private static Animation CreateWorkflowAnimation()
        {
            return new Animation()
            {
                Duration = TimeSpan.FromMilliseconds(2000),
                IterationCount = new IterationCount(1),
                Children =
                {
                    new KeyFrame()
                    {
                        Setters = { new Setter { Property = OpacityProperty, Value = 0.0D } },
                        Cue = new Cue(0.0D)
                    },
                    new KeyFrame()
                    {
                        Setters = { new Setter { Property = OpacityProperty, Value = 1.0D } },
                        Cue = new Cue(0.66D)
                    },
                    new KeyFrame()
                    {
                        Setters = { new Setter { Property = OpacityProperty, Value = 0.0D } },
                        Cue = new Cue(1.0D)
                    }
                }
            };
        }
    }
}
