using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Pipeline.Steps;

namespace Yggdrassil.Application.Pipeline
{
    /// <summary>
    /// Orchestrates execution of ordered build steps.
    /// Contains no format or engine specific logic.
    /// </summary>
    public sealed class BuildPipeline
    {
        private readonly List<IBuildStep> _steps = new();

        public BuildPipeline AddStep(IBuildStep step)
        {
            _steps.Add(step);
            return this;
        }
        public async Task<BuildResult> RunAsync(BuildContext context, CancellationToken cancellationToken)
        {
            var result = new BuildResult();

            Directory.CreateDirectory(context.Paths.OutputDirectory);
            Directory.CreateDirectory(context.Paths.StudioMdlDir);
            Directory.CreateDirectory(context.Paths.LogsDir);

            BuildContext contextBeforeStep;


            for (int i = 0; i < _steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // Backup context before running the step in case we need to revert after an error, or redo the step with modified context.
                    // We need a deep copy
                    contextBeforeStep = context.Clone();
                    await _steps[i].RunAsync(context, result, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Catch any unhandled exceptions from steps and convert them into error messages in the build result
                    result.Messages.Add(new BuildMessage(BuildSeverity.Error, "UnhandledException", $"Step '{_steps[i].Name}' threw an exception: {ex.Message}"));

                    // Stop on first failure to prevent cascading errors, but this could be changed to continue running subsequent steps if desired
                    break;
                }
            }
            return result;
        }
    }
}
