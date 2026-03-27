using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Presentation.Rendering;
using Yggdrasil.Renderer.Runtime;
using Yggdrasil.Application;
using Yggdrasil.Infrastructure;
using Yggdrasil.Presentation.ViewModels;

namespace Yggdrasil.Presentation.Services
{
    public class AppHost
    {
        public AppServices Backend { get; }
        public FileDialogService FileDialogs { get; }
        public ShellViewModel Shell { get; }
        public RecentProjectsService RecentProjects { get; }
        public RendererHost Renderer { get; }
        public SceneToRenderSnapshotMapper SceneMapper { get; }
        public ViewportCoordinator Viewport { get; }

        public AppHost()
        {
            Backend = AppServicesFactory.Create();
            FileDialogs = new FileDialogService();
            RecentProjects = new RecentProjectsService();
            Shell = new ShellViewModel(Backend, FileDialogs, RecentProjects);
            Renderer = new RendererHost();
            SceneMapper = new SceneToRenderSnapshotMapper();
            Viewport = new ViewportCoordinator(Renderer, SceneMapper);
        }
    }
}
