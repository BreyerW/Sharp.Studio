using System;
using System.Collections.Generic;
using System.Windows.Input;
using Sharp.DockManager.ViewModels;
using Sharp.Studio.Models;

namespace Sharp.Studio.ViewModels
{
    public class ComplexDockableViewModel : ViewModelBase
    {
        public ComplexDockableViewModel() {
            CreateTabMenuItems = new List<TabFactoryViewModel>()
            {
                new TabFactoryViewModel()
                {
                    Header = "Assets View",
                    CommandParameter = ()=> new DockableAssets()
                },
                new TabFactoryViewModel()
                {
                    Header = "-"
                },
                new TabFactoryViewModel()
                {
                    Header = "Scene View",
                    CommandParameter = ()=> new DockableScene()
                }
            };
        }

        public IReadOnlyList<TabFactoryViewModel> CreateTabMenuItems { get; }
    }
}
