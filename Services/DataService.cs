using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sleipnir.App.Models;

namespace Sleipnir.App.Services
{
    public interface IDataService
    {
        Task<List<Project>> GetProjectsAsync();
        Task<Project> CreateProjectAsync(string name, string description, string? logoUrl = null, string? logoData = null);
        Task UpdateProjectAsync(Project project);
        Task DeleteProjectAsync(Guid projectId);
        
        Task<List<Sprint>> GetSprintsAsync(Guid projectId);
        Task<Sprint> CreateSprintAsync(Sprint sprint);
        Task UpdateSprintAsync(Sprint sprint);
        Task DeleteSprintAsync(Guid sprintId);

        Task<List<Issue>> GetIssuesAsync(Guid projectId);
        Task<Issue> CreateIssueAsync(Issue issue);
        Task UpdateIssueAsync(Issue issue);
        Task DeleteIssueAsync(Guid issueId);

        Task<List<IssueLog>> GetLogsAsync(Guid issueId);
        Task AddLogAsync(IssueLog log);

        Task<List<Collaborator>> GetCollaboratorsAsync();

        // User Management
        Task<List<AppUser>> GetUsersAsync();
        Task<AppUser?> GetUserByUsernameAsync(string username);
        Task<AppUser> CreateUserAsync(AppUser user);
        Task UpdateUserAsync(AppUser user);
        Task DeleteUserAsync(Guid userId);
    }

    public class MockDataService : IDataService
    {
        private List<Project> _projects = new List<Project>();
        private List<Sprint> _sprints = new List<Sprint>();
        private List<Issue> _issues = new List<Issue>();
        private List<IssueLog> _logs = new List<IssueLog>();
        private List<Collaborator> _collaborators = new List<Collaborator>();
        private List<AppUser> _users = new List<AppUser>();

        public MockDataService()
        {
            var p1 = new Project { 
                Name = "Elysium Engine", 
                Description = "Next-gen game engine",
                LogoUrl = "C:/Users/tobias.knoepfli/.gemini/antigravity/brain/49eb8de9-c6da-4c3a-853c-3455bb6ee5dc/elysium_engine_logo_1769524255497.png"
            };
            var p2 = new Project { 
                Name = "CyberNet UI", 
                Description = "Modern UI Framework",
                LogoUrl = "C:/Users/tobias.knoepfli/.gemini/antigravity/brain/49eb8de9-c6da-4c3a-853c-3455bb6ee5dc/cybernet_ui_logo_1769524616278.png"
            };
            _projects.Add(p1);
            _projects.Add(p2);

            var s1 = new Sprint { ProjectId = p1.Id, Name = "Sprint 1", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(14) };
            _sprints.Add(s1);

            var c1 = new Collaborator { Name = "Tobias K.", Email = "tobias@example.com" };
            var c2 = new Collaborator { Name = "Sarah M.", Email = "sarah@example.com" };
            var c3 = new Collaborator { Name = "Admin", Email = "admin@example.com" };
            _collaborators.AddRange(new[] { c1, c2, c3 });

            _issues.Add(new Issue { 
                ProjectId = p1.Id, 
                ProgramComponent = "Game", 
                SubComponents = "Add New Game;Add new Stadium", 
                Description = "Add Button needs rounded corners",
                LongDescription = "The current buttons are too sharp. We need a 8px corner radius across the entire UI for consistency.",
                Type = "Bug", 
                Category = "Backlog", 
                Status = "Open",
                Priority = "Medium",
                ResponsibleUsers = "Sarah M.",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

            _issues.Add(new Issue {
                ProjectId = p1.Id,
                ProgramComponent = "Web",
                Description = "Implement WebAssembly Support",
                LongDescription = "Explore how we can run the engine in the browser via WASM.",
                Type = "Feature",
                Category = "Backlog",
                Status = "Open",
                Priority = "Critical",
                ResponsibleUsers = "Tobias K.;Sarah M.",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            });

            _issues.Add(new Issue {
                ProjectId = p1.Id,
                ProgramComponent = "UI",
                Description = "Glassmorphism Sidebar Epic",
                LongDescription = "Think about adding a blur effect to the sidebar for a more modern look.",
                Type = "Epic",
                Category = "Backlog",
                Status = "Open",
                Priority = "Nice to Have",
                ResponsibleUsers = "Tobias K.",
                CreatedAt = DateTime.UtcNow
            });

            _issues.Add(new Issue { 
                ProjectId = p1.Id, 
                ProgramComponent = "Core", 
                SubComponents = "Renderer", 
                Description = "Raytracing Refactor", 
                Type = "Story", 
                Category = "Pipeline", 
                Status = "In Progress",
                SprintId = s1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });

            _issues.Add(new Issue { 
                ProjectId = p1.Id, 
                ProgramComponent = "Engine", 
                SubComponents = "Physics", 
                Description = "Collision Overhaul", 
                Type = "Story", 
                Category = "Pipeline", 
                Status = "In Testing",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

            foreach(var issue in _issues) {
                _logs.Add(new IssueLog { IssueId = issue.Id, Action = "Created", Details = "Initial creation" });
            }

            // Create default admin user
            _users.Add(new AppUser 
            { 
                Username = "admin", 
                Password = "admin", 
                FirstName = "Super", 
                LastName = "User", 
                IsSuperuser = true, 
                Emoji = "⚡" 
            });
        }

        public Task<List<Project>> GetProjectsAsync() => Task.FromResult(_projects);
        
        public Task<Project> CreateProjectAsync(string name, string description, string? logoUrl = null, string? logoData = null)
        {
            var p = new Project { Name = name, Description = description, LogoUrl = logoUrl, LogoData = logoData };
            _projects.Add(p);
            return Task.FromResult(p);
        }

        public Task UpdateProjectAsync(Project project)
        {
            var idx = _projects.FindIndex(p => p.Id == project.Id);
            if (idx >= 0) _projects[idx] = project;
            return Task.CompletedTask;
        }

        public Task DeleteProjectAsync(Guid projectId)
        {
            _projects.RemoveAll(p => p.Id == projectId);
            _sprints.RemoveAll(s => s.ProjectId == projectId);
            _issues.RemoveAll(i => i.ProjectId == projectId);
            return Task.CompletedTask;
        }

        public Task<List<Sprint>> GetSprintsAsync(Guid projectId) => 
            Task.FromResult(_sprints.FindAll(s => s.ProjectId == projectId));

        public Task<Sprint> CreateSprintAsync(Sprint sprint)
        {
            _sprints.Add(sprint);
            return Task.FromResult(sprint);
        }

        public Task UpdateSprintAsync(Sprint sprint)
        {
            var idx = _sprints.FindIndex(s => s.Id == sprint.Id);
            if (idx >= 0)
            {
                _sprints[idx] = sprint;
            }
            return Task.CompletedTask;
        }

        public Task DeleteSprintAsync(Guid sprintId)
        {
            _sprints.RemoveAll(s => s.Id == sprintId);
            return Task.CompletedTask;
        }

        public Task<List<Issue>> GetIssuesAsync(Guid projectId) => 
            Task.FromResult(_issues.FindAll(i => i.ProjectId == projectId));

        public Task<Issue> CreateIssueAsync(Issue issue)
        {
            _issues.Add(issue);
            _logs.Add(new IssueLog { IssueId = issue.Id, Action = "Created", Details = "Manual entry" });
            return Task.FromResult(issue);
        }

        public Task UpdateIssueAsync(Issue issue)
        {
            var idx = _issues.FindIndex(i => i.Id == issue.Id);
            if (idx >= 0) {
                 var old = _issues[idx];
                 if (old.Status != issue.Status)
                    _logs.Add(new IssueLog { IssueId = issue.Id, Action = "Status Changed", Details = $"From {old.Status} to {issue.Status}" });
                 _issues[idx] = issue;
            }
            return Task.CompletedTask;
        }

        public Task DeleteIssueAsync(Guid issueId)
        {
            _issues.RemoveAll(i => i.Id == issueId);
            return Task.CompletedTask;
        }

        public Task<List<IssueLog>> GetLogsAsync(Guid issueId) => 
            Task.FromResult(_logs.FindAll(l => l.IssueId == issueId));

        public Task AddLogAsync(IssueLog log)
        {
            _logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<List<Collaborator>> GetCollaboratorsAsync() => Task.FromResult(_collaborators);

        public Task<List<AppUser>> GetUsersAsync() => Task.FromResult(_users);
        
        public Task<AppUser?> GetUserByUsernameAsync(string username) => 
            Task.FromResult(_users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));

        public Task<AppUser> CreateUserAsync(AppUser user)
        {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateUserAsync(AppUser user)
        {
            var idx = _users.FindIndex(u => u.Id == user.Id);
            if (idx >= 0) _users[idx] = user;
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(Guid userId)
        {
            _users.RemoveAll(u => u.Id == userId);
            return Task.CompletedTask;
        }
    }
}
