using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Application.Services;
using Yggdrasil.Application.UseCases;

namespace Yggdrasil.Application
{
    public sealed class AppServices
    {
        public required IQcTemplateStore Templates { get; init; }
        public required IQcAssembler Assembler { get; init; }
        public required IProportionTrickService ProportionTrickService { get; init; }
        public required IProjectStore ProjectStore { get; init; }
        public required IModelImporter Importer { get; init; }
        public required IMeshExporter GeneralExporter { get; init; }
        public IMeshExporter? DmxExporter { get; init; }
        public IMeshExporter? SmdExporter { get; init; }

        public required CreateProjectUseCase CreateProject { get; init; }
        public required OpenProjectUseCase OpenProject { get; init; }
        public required SaveProjectUseCase SaveProject { get; init; }
        public required ImportModelUseCase ImportModel { get; init; }
        public required ExportBuildUseCase ExportBuild { get; init; }
        public required ProjectEditorService ProjectEditor { get; init; }

    }
}
