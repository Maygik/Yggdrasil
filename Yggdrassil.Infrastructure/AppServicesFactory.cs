using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Application.Services;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Infrastructure.Export;
using Yggdrassil.Infrastructure.Import;
using Yggdrassil.Infrastructure.IO;
using Yggdrassil.Infrastructure.QC;

namespace Yggdrassil.Infrastructure
{
    public static class AppServicesFactory
    {
        public static AppServices Create()
        {
            var templateStore = new QcTemplateStore();
            templateStore.Init();

            var projectStore = new YggProjectStore();
            var importer = new AssimpModelImporter();
            var smdExporter = new SmdExporter();
            var dmxExporter = new DmxExporter();
            var exporter = smdExporter;
            var assembler = new QcAssembler(templateStore);
            var proportionTrick = new ProportionTrickService();
            var projectEditor = new ProjectEditorService();

            return new AppServices
            {
                Templates = templateStore,
                Assembler = assembler,
                ProportionTrickService = proportionTrick,
                ProjectStore = projectStore,
                Importer = importer,
                GeneralExporter = exporter,
                SmdExporter = smdExporter,
                DmxExporter = dmxExporter,
                CreateProject = new CreateProjectUseCase(projectStore),
                OpenProject = new OpenProjectUseCase(projectStore),
                SaveProject = new SaveProjectUseCase(projectStore),
                ImportModel = new ImportModelUseCase(importer),
                ExportBuild = new ExportBuildUseCase(exporter, assembler, proportionTrick),
                ProjectEditor = projectEditor
            };
        }
    }
}
