using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;

namespace Yggdrassil.Application.UseCases
{
    public sealed class CreateProjectUseCase
    {
        private readonly IProjectStore _projectStore;

        public  CreateProjectUseCase(IProjectStore projectStore)
        {
            _projectStore = projectStore;
        }

        public CreateProjectResult Execute(CreateProjectRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new CreateProjectResult(false, null, null, "Project name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.ProjectDirectory))
            {
                return new CreateProjectResult(false, null, null, "Project directory cannot be empty.");
            }

            if (!Directory.Exists(request.ProjectDirectory))
            {
                Directory.CreateDirectory(request.ProjectDirectory);
            }

            var project = new Project
            {
                Name = request.Name,
                Directory = request.ProjectDirectory
            };

            project.Build.OutputDirectory = Path.Combine(request.ProjectDirectory, "output");

            var projectFilePath = Path.Combine(request.ProjectDirectory, $"{request.Name}.yggproj");
            _projectStore.Save(projectFilePath, project);

            var result = new CreateProjectResult(true, project, projectFilePath);
            result.Messages.Add($"Created project '{request.Name}'.");
            result.Messages.Add($"Saved project to '{projectFilePath}'.");
            return result;
        }
    }


    public class CreateProjectResult : ServiceResult
    {
        public Project? CreatedProject { get; }
        public string? ProjectFilePath { get; }

        public CreateProjectResult(bool success, Project? createdProject, string? projectFilePath, string? errorMessage = null) : base(success, errorMessage)
        {
            CreatedProject = createdProject;
            ProjectFilePath = projectFilePath;
        }
    }

    public struct CreateProjectRequest
    {
        public string Name { get; set; }
        public string ProjectDirectory { get; set; }
    }


}
