using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Cli.Commands
{
    internal class OpenProjectCommand
    {
        public OpenProjectCommand(Composition.Services services)
        {
            Services = services;
        }

        private Composition.Services Services { get; }

        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            // Check if the project file actually exists
            // Load it
            // Start editing it
        }
    }
}
