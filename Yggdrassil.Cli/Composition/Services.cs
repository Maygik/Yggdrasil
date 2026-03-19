using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Infrastructure.Export;
using Yggdrassil.Infrastructure.Import;
using Yggdrassil.Infrastructure.IO;
using Yggdrassil.Infrastructure.QC;

namespace Yggdrassil.Cli.Composition
{
    public class Services
    {
        private Services()
        {
        }

        public required IQcTemplateStore Templates { get; init; }
        public required IQcAssembler Assembler { get; init; }
        public required IModelImporter Importer { get; init; }
        public required IProjectStore ProjectStore { get; init; }

        public required IMeshExporter GeneralExporter { get; init; }
        public IMeshExporter? DmxExporter { get; init; }
        public IMeshExporter? SmdExporter { get; init; }

        public static Services Create()
        {
            var store = new QcTemplateStore();
            store.Init();

            var assembler = new QcAssembler(store);

            var projectStore = new YggProjectStore();

            var importer = new AssimpModelImporter();

            var exporter = new SmdExporter();

            return new Services
            {
                Templates = store,
                Assembler = assembler,
                Importer = importer,
                ProjectStore = projectStore,
                GeneralExporter = exporter
            };
        }
    }
}
