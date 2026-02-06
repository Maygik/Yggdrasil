

namespace Yggdrassil.Application.Abstractions
{
    /// <summary>
    /// Interface for QC template store operations.
    /// </summary>
    public interface IQcTemplateStore
    {
        /// <summary>
        /// Initializes the template store by loading available templates.
        /// </summary>
        void Init(string appName);

    }
}