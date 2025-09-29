using System;
using System.Collections.Generic;
using System.Windows.Input;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Models;
using Sharp.Studio.Views;

namespace Sharp.Studio.ViewModels
{
    public class ComplexDockableViewModel : ViewModelBase
    {
        public ComplexDockableViewModel() {
            var firstViewFactory = () => new DockableFirstView();
            var secondViewFactory = () => new DockableSecondView();

            CreateTabMenuItems = new List<TabFactoryViewModel>()
            {
                new TabFactoryViewModel()
                {
                    Header = ((CloseableHeader)firstViewFactory().Header).Text,
                    CommandParameter = firstViewFactory
                },
                new TabFactoryViewModel()
                {
                    Header = "-"
                },
                new TabFactoryViewModel()
                {
                    Header = ((CloseableHeader)secondViewFactory().Header).Text,
                    CommandParameter = secondViewFactory
                }
            };
        }

        public IReadOnlyList<TabFactoryViewModel> CreateTabMenuItems { get; }
    }
}
