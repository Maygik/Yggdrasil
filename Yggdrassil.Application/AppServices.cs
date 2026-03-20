using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Application.Services;
using Yggdrassil.Application.UseCases;

namespace Yggdrassil.Application
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
