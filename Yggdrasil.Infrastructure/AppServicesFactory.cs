using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application;
using Yggdrasil.Application.Services;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Infrastructure.Export;
using Yggdrasil.Infrastructure.Import;
using Yggdrasil.Infrastructure.IO;
using Yggdrasil.Infrastructure.Images;
using Yggdrasil.Infrastructure.QC;

namespace Yggdrasil.Infrastructure
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
            var vtfWriter = new VtfCmdWriter();
            var materialBaker = new MaterialBaker(vtfWriter);
            var materialExporter = new MaterialExporter();

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
                ExportMaterials = new ExportMaterialsUseCase(materialExporter, materialBaker),
                ProjectEditor = projectEditor
            };
        }
    }
}
