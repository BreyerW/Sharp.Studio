using System;
using System.Collections.Generic;
using System.Windows.Input;
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
                    Command = AddTab,
                    CommandParameter = typeof(DockableAssets)
                },
                new TabFactoryViewModel()
                {
                    Header = "-"
                },
                new TabFactoryViewModel()
                {
                    Header = "Scene View",
                    Command = AddTab,
                    CommandParameter = typeof(DockableScene)
                }
            };
            /*MenuItems = new ()
            {
                new MenuItemViewModel
                {
                    Header = "_File",
                    Items =
                    new () {
                        new MenuItemViewModel { Header = "_Open...", Command = OpenCommand },
                        new MenuItemViewModel { Header = "Save", Command = SaveCommand },
                        new MenuItemViewModel { Header = "-" },
                        new MenuItemViewModel
                        {
                            Header = "Recent",
                            Items =
                            [
                                new MenuItemViewModel
                                {
                                    Header = "File1.txt",
                                    Command = OpenRecentCommand,
                                    CommandParameter = @"c:\foo\File1.txt"
                                },
                                new MenuItemViewModel
                                {
                                    Header = "File2.txt",
                                    Command = OpenRecentCommand,
                                    CommandParameter = @"c:\foo\File2.txt"
                                },
                            ]
                        },
                    }
                },
                new MenuItemViewModel
                {
                    Header = "_Edit",
                    Items =
                    new () 
                        new MenuItemViewModel { Header = "_Copy" },
                        new MenuItemViewModel { Header = "_Paste" },
                    ]
                }
            };*/
        }

        public IReadOnlyList<TabFactoryViewModel> CreateTabMenuItems { get; }
        public ICommand? AddTab { get; }
    }
}
