using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoTechHub.Data;
using AutoTechHub.Models;

namespace AutoTechHub.Services
{
    public class SnippetService : ISnippetService
    {
        private readonly AppDbContext _context;

        public SnippetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CodeSnippet>> GetAllSnippetsAsync(string language = null, string category = null, string search = null)
        {
            var query = _context.CodeSnippets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(language))
            {
                query = query.Where(s => s.Language.ToLower() == language.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.CategoryTag.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(lowerSearch) ||
                                         s.Description.ToLower().Contains(lowerSearch) ||
                                         s.CodeContent.ToLower().Contains(lowerSearch));
            }

            return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task<CodeSnippet> GetSnippetByIdAsync(int id)
        {
            return await _context.CodeSnippets.FindAsync(id);
        }

        public async Task<CodeSnippet> CreateSnippetAsync(CodeSnippet snippet)
        {
            snippet.CreatedAt = DateTime.UtcNow;
            _context.CodeSnippets.Add(snippet);
            await _context.SaveChangesAsync();
            return snippet;
        }

        public async Task<bool> DeleteSnippetAsync(int id)
        {
            var snippet = await _context.CodeSnippets.FindAsync(id);
            if (snippet == null) return false;

            _context.CodeSnippets.Remove(snippet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetAvailableLanguagesAsync()
        {
            return await _context.CodeSnippets
                .Select(s => s.Language)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
        }
    }
}
