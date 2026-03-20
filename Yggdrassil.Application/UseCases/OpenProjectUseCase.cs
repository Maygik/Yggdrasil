using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.UseCases
{
    public sealed class OpenProjectUseCase
    {
        private readonly IProjectStore _projectStore;

        public OpenProjectUseCase(IProjectStore projectStore)
        {
            _projectStore = projectStore;
        }

        public OpenProjectResult Execute(OpenProjectRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectFilePath))
            {
                return new OpenProjectResult(false, null, "Project file path cannot be empty.");
            }

            if (!File.Exists(request.ProjectFilePath))
            {
                return new OpenProjectResult(false, null, $"Project file '{request.ProjectFilePath}' does not exist.");
            }


            var project = _projectStore.LoadProject(request.ProjectFilePath);
            if (project == null)
            {
                return new OpenProjectResult(false, null, $"Failed to open project \"{request.ProjectFilePath}.\"");
            }

            // Update the project directory just in case the project file was moved since it was last saved.
            project.Directory = Path.GetDirectoryName(request.ProjectFilePath);

            var result = new OpenProjectResult(true, project);
            result.Messages.Add($"Opened project '{project.Name}'.");
            result.Messages.Add($"Loaded from '{request.ProjectFilePath}'.");
            return result;
        }
    }

    public class OpenProjectResult : ServiceResult
    {
        public Project? OpenedProject { get; }

        public OpenProjectResult(bool success, Project? openedProject, string? errorMessage = null) : base(success, errorMessage)
        {
            OpenedProject = openedProject;
        }
    }

    public struct OpenProjectRequest
    {
        public string ProjectFilePath { get; set; }
    }
}
