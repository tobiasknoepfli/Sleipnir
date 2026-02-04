using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using Sleipnir.App.Models;
using System.Linq;

namespace Sleipnir.App.Services
{
    public class SupabaseDataService : IDataService
    {
        private readonly Supabase.Client _client;

        public SupabaseDataService(string url, string key)
        {
            var options = new SupabaseOptions { AutoConnectRealtime = true };
            _client = new Supabase.Client(url, key, options);
        }

        public async Task InitializeAsync()
        {
            await _client.InitializeAsync();
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            var response = await _client.From<Project>().Get();
            return response.Models ?? new List<Project>();
        }

        public async Task<Project> CreateProjectAsync(string name, string description, string? logoUrl = null, string? logoData = null)
        {
            var project = new Project { Name = name, Description = description, LogoUrl = logoUrl, LogoData = logoData };
            var response = await _client.From<Project>().Insert(project);
            return response.Model ?? project;
        }

        public async Task UpdateProjectAsync(Project project)
        {
            await _client.From<Project>().Update(project);
        }

        public async Task DeleteProjectAsync(Guid projectId)
        {
            await _client.From<Project>()
                .Where(x => x.Id == projectId)
                .Delete();
        }

        public async Task<List<Sprint>> GetSprintsAsync(Guid projectId)
        {
            var response = await _client.From<Sprint>()
                .Where(x => x.ProjectId == projectId)
                .Get();
            return response.Models ?? new List<Sprint>();
        }

        public async Task<Sprint> CreateSprintAsync(Sprint sprint)
        {
            var response = await _client.From<Sprint>().Insert(sprint);
            return response.Model ?? sprint;
        }

        public async Task UpdateSprintAsync(Sprint sprint)
        {
            await _client.From<Sprint>().Update(sprint);
        }

        public async Task DeleteSprintAsync(Guid sprintId)
        {
            await _client.From<Sprint>()
                .Where(x => x.Id == sprintId)
                .Delete();
        }

        public async Task<List<Issue>> GetIssuesAsync(Guid projectId)
        {
            var response = await _client.From<Issue>()
                .Where(x => x.ProjectId == projectId)
                .Get();
            return response.Models ?? new List<Issue>();
        }

        public async Task<Issue> CreateIssueAsync(Issue issue)
        {
            var response = await _client.From<Issue>().Insert(issue);
            return response.Model ?? issue;
        }

        public async Task UpdateIssueAsync(Issue issue)
        {
            await _client.From<Issue>().Update(issue);
        }

        public async Task DeleteIssueAsync(Guid issueId)
        {
            await _client.From<Issue>()
                .Where(x => x.Id == issueId)
                .Delete();
        }

        public async Task<List<IssueLog>> GetLogsAsync(Guid issueId)
        {
             var response = await _client.From<IssueLog>()
                .Where(x => x.IssueId == issueId)
                .Get();
            return response.Models ?? new List<IssueLog>();
        }

        public async Task AddLogAsync(IssueLog log)
        {
            await _client.From<IssueLog>().Insert(log);
        }

        public async Task<List<Collaborator>> GetCollaboratorsAsync()
        {
            var response = await _client.From<Collaborator>().Get();
            return response.Models ?? new List<Collaborator>();
        }

        public async Task<List<AppUser>> GetUsersAsync()
        {
            var response = await _client.From<AppUser>().Get();
            return response.Models ?? new List<AppUser>();
        }

        public async Task<AppUser?> GetUserByUsernameAsync(string username)
        {
            var response = await _client.From<AppUser>()
                .Where(x => x.Username == username)
                .Single();
            return response;
        }

        public async Task<AppUser> CreateUserAsync(AppUser user)
        {
            var response = await _client.From<AppUser>().Insert(user);
            return response.Model ?? user;
        }

        public async Task UpdateUserAsync(AppUser user)
        {
            await _client.From<AppUser>().Update(user);
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            await _client.From<AppUser>()
                .Where(x => x.Id == userId)
                .Delete();
        }
    }
}
