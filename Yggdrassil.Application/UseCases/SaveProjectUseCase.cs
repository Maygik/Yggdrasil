using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.UseCases
{
    public  class SaveProjectUseCase
    {
        private readonly IProjectStore _projectStore;

        public SaveProjectUseCase(IProjectStore projectStore)
        {
            _projectStore = projectStore;
        }

        public SaveProjectResult Execute(SaveProjectRequest request)
        {
            if (request.Project.Directory == null)
                return new SaveProjectResult(false, "Project directory is not set. Cannot save.");
            if (string.IsNullOrEmpty(request.Project.Name))
                return new SaveProjectResult(false, "Project name cannot be empty. Cannot save.");

            try
            {
                var projectFilePath = Path.Combine(request.Project.Directory, $"{request.Project.Name}.yggproj");
                _projectStore.Save(projectFilePath, request.Project);

                var result = new SaveProjectResult(true);
                result.Messages.Add($"Saved project '{request.Project.Name}'.");
                result.Messages.Add($"Wrote project file to '{projectFilePath}'.");
                return result;
            }
            catch (Exception ex)
            {
                return new SaveProjectResult(false, ex.Message);
            }

        }

    }

    public class SaveProjectResult : ServiceResult
    {
        public SaveProjectResult(bool success, string? errorMessage = null) : base(success, errorMessage)
        {

        }
    }

    public class SaveProjectRequest
    {
        public required Project Project;
    }
}
