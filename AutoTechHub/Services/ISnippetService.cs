using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;

namespace AutoTechHub.Services
{
    public interface ISnippetService
    {
        Task<List<CodeSnippet>> GetAllSnippetsAsync(string language = null, string category = null, string search = null);
        Task<CodeSnippet> GetSnippetByIdAsync(int id);
        Task<CodeSnippet> CreateSnippetAsync(CodeSnippet snippet);
        Task<bool> DeleteSnippetAsync(int id);
        Task<List<string>> GetAvailableLanguagesAsync();
    }
}
