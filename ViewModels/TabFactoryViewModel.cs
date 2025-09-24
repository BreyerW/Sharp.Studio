using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sharp.Studio.ViewModels
{
    public class TabFactoryViewModel : ViewModelBase
    {
        public string? Header { get; set; }
        public ICommand? Command { get; set; }
        public object? CommandParameter { get; set; }

        //when nested menu is needed
        public IReadOnlyList<TabFactoryViewModel>? Items { get; set; }
    }
}
