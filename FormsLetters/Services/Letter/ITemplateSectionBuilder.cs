using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FormsLetters.Services.Letter
{
    /// <summary>
    /// Builds a section (header or ending) of a letter given a placeholder dictionary.
    /// </summary>
    public interface ITemplateSectionBuilder
    {
        Task<string> BuildAsync(Dictionary<string,string> data, CancellationToken cancellationToken = default);
    }
}
