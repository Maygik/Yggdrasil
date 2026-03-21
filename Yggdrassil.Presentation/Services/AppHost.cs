using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Infrastructure;
using Yggdrassil.Presentation.ViewModels;

namespace Yggdrassil.Presentation.Services
{
    public class AppHost
    {
        public AppServices Backend { get; }
        public FileDialogService FileDialogs { get; }
        public ShellViewModel Shell { get; }


        public AppHost()
        {
            Backend = AppServicesFactory.Create();
            FileDialogs = new FileDialogService();
            Shell = new ShellViewModel(Backend, FileDialogs);
        }
    }
}
