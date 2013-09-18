﻿using System.Windows.Input;
using Common.ViewModels;

namespace Selenite.Client.TestResults.ViewModels
{
    public class TestCollectionSummaryViewModel : ViewModelBase
    {
        public string Name { get; set; }

        public bool IsEnabled
        {
            get { return Get(() => IsEnabled); }
            set { Set(value, () => IsEnabled); }
        }

        public ICommand IsEnabledChangedCommand { get; set; }
    }
}
