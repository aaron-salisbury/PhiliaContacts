﻿using GalaSoft.MvvmLight;
using PhiliaContacts.App.Base.Extensions;

namespace PhiliaContacts.App.ViewModels
{
    public class IntroductionViewModel : ViewModelBase
    {
        private string _appDisplayName;

        public string AppDisplayName
        {
            get => _appDisplayName;
            set => Set(ref _appDisplayName, value);
        }

        public IntroductionViewModel()
        {
            AppDisplayName = "AppDisplayName".GetLocalized();
        }
    }
}
