using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sleipnir.App.Models;
using Sleipnir.App.Services;

namespace Sleipnir.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private List<Issue> _allProjectIssues = new();

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private string _selectedCategory = "Backlog"; 

        public string CurrentViewTitle => SelectedSprint == null ? "Unplanned Issues" : $"Sprint: {SelectedSprint.Name}";

        public bool IsBoardViewVisible => SelectedSprint != null && SelectedCategory == "Backlog";

        public bool CanCompleteSelectedSprint => SelectedSprint != null && SelectedSprint.CanBeCompleted;

        [ObservableProperty]
        private ObservableCollection<Sprint> _sprints = new();

        [ObservableProperty]
        private Sprint? _selectedSprint;

        partial void OnSelectedSprintChanged(Sprint? value)
        {
            if (value != null)
            {
                SelectedCategory = "Backlog";
            }
            OnPropertyChanged(nameof(CurrentViewTitle));
            OnPropertyChanged(nameof(IsBoardViewVisible));
            OnPropertyChanged(nameof(CanCompleteSelectedSprint));
            RefreshCategorizedIssues();
        }

        [ObservableProperty]
        private ObservableCollection<Sprint> _archivedSprints = new();

        [ObservableProperty]
        private bool _isArchiveVisible;


        [ObservableProperty]
        private ObservableCollection<Collaborator> _collaborators = new();
        public List<string> Kinds { get; } = new() { "Bug", "Feature", "Patch", "Overhaul", "Alteration" };
        public List<string> Priorities { get; } = new() { "Critical", "Very High", "High", "Neutral", "Low", "Very Low", "Nice to Have" };
        public List<string> Statuses { get; } = new() { "Open", "Blocked", "In Progress", "Testing", "Finished" };

        [ObservableProperty]
        private ObservableCollection<Issue> _openItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _allCategoryItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _inProgressItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _testingItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _finishedItems = new();

        [ObservableProperty]
        private bool _isLoading;

        // Sprint Planning Overlay Properties
        [ObservableProperty]
        private bool _isSprintModalVisible;

        [ObservableProperty]
        private string _newSprintName = "";

        [ObservableProperty]
        private DateTime _newSprintStartDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _newSprintEndDate = DateTime.Today.AddDays(14);

        [ObservableProperty]
        private bool _isEditingSprint;

        [ObservableProperty]
        private string _sprintModalTitle = "Plan New Sprint";

        [ObservableProperty]
        private bool _isProjectSelectorVisible;

        // Project Modal Properties
        [ObservableProperty]
        private bool _isProjectModalVisible;
        [ObservableProperty]
        private string _newProjectName = "";
        [ObservableProperty]
        private string _newProjectDescription = "";
        [ObservableProperty]
        private string _newProjectLogoUrl = "";
        [ObservableProperty]
        private bool _isEditingProject;
        [ObservableProperty]
        private string _projectModalTitle = "Create New Project";
        [ObservableProperty]
        private bool _showHubArchive;

        [ObservableProperty]
        private Issue? _plannedIssue;

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            CreateProjectCommand = new RelayCommand(OpenProjectModal);
            CreateIssueCommand = new AsyncRelayCommand<string>(CreateIssueAsync);
            
            OpenSprintModalCommand = new RelayCommand(OpenSprintModal);
            EditSprintModalCommand = new RelayCommand(OpenEditSprintModal);
            SaveSprintCommand = new AsyncRelayCommand(SaveSprintAsync);
            CancelSprintModalCommand = new RelayCommand(() => IsSprintModalVisible = false);
            
            ShowLogsCommand = new AsyncRelayCommand<Issue>(ShowLogsAsync);
            CompleteSprintCommand = new AsyncRelayCommand(CompleteSprintAsync);
            AssignToSprintCommand = new AsyncRelayCommand<Sprint>(AssignToSpecificSprintAsync);
            ToggleArchiveCommand = new RelayCommand(() => IsArchiveVisible = !IsArchiveVisible);
            ToggleHubArchiveCommand = new RelayCommand(() => { ShowHubArchive = !ShowHubArchive; RefreshCategorizedIssues(); });
            RestoreIssueCommand = new AsyncRelayCommand<Issue>(RestoreIssueAsync);
            DeleteSprintCommand = new AsyncRelayCommand(DeleteSprintAsync);
            ToggleProjectSelectorCommand = new RelayCommand(() => IsProjectSelectorVisible = !IsProjectSelectorVisible);
            SelectProjectCommand = new RelayCommand<Project>(p => {
                if (p != null) {
                    SelectedProject = p;
                    IsProjectSelectorVisible = false;
                }
            });

            OpenIssueDetailCommand = new RelayCommand<Issue>(OpenIssueDetail);

            SetPlannedIssueCommand = new RelayCommand<Issue>(issue => PlannedIssue = issue);

            OpenProjectModalCommand = new RelayCommand(OpenProjectModal);
            EditProjectModalCommand = new RelayCommand(OpenEditProjectModal);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
            CancelProjectModalCommand = new RelayCommand(() => IsProjectModalVisible = false);
            BrowseLogoCommand = new RelayCommand(BrowseLogo);
            ClearLogoCommand = new RelayCommand(() => NewProjectLogoUrl = "");

            AddStoryCommand = new AsyncRelayCommand<Issue>(AddStoryAsync);
            AddChildIssueCommand = new AsyncRelayCommand<Issue>(AddChildIssueAsync);
            UnlinkIssueCommand = new AsyncRelayCommand<Issue>(UnlinkIssueAsync);
            UpdateIssueParentCommand = new AsyncRelayCommand<Issue>(UpdateIssueParentAsync);
            EditChildStoryCommand = new AsyncRelayCommand<Issue>(EditChildStoryAsync);
            DeleteChildStoryCommand = new AsyncRelayCommand<Issue>(DeleteChildStoryAsync);
            JumpToIssueCommand = new RelayCommand<object>(JumpToIssue);
            ArchiveIssueCommand = new AsyncRelayCommand<Issue>(ArchiveIssueAsync);
        }

        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand CreateProjectCommand { get; }
        public IAsyncRelayCommand<string> CreateIssueCommand { get; }
        
        public IRelayCommand OpenSprintModalCommand { get; }
        public IRelayCommand EditSprintModalCommand { get; }
        public IAsyncRelayCommand SaveSprintCommand { get; }
        public IRelayCommand CancelSprintModalCommand { get; }
        
        public IAsyncRelayCommand<Issue> ShowLogsCommand { get; }
        public IAsyncRelayCommand CompleteSprintCommand { get; }
        public IAsyncRelayCommand<Sprint> AssignToSprintCommand { get; }
        public IRelayCommand ToggleArchiveCommand { get; }
        public IAsyncRelayCommand DeleteSprintCommand { get; }
        public IRelayCommand ToggleProjectSelectorCommand { get; }
        public IRelayCommand<Project> SelectProjectCommand { get; }

        public IRelayCommand OpenProjectModalCommand { get; }
        public IRelayCommand EditProjectModalCommand { get; }
        public IAsyncRelayCommand SaveProjectCommand { get; }
        public IRelayCommand CancelProjectModalCommand { get; }
        public IRelayCommand BrowseLogoCommand { get; }
        public IRelayCommand ClearLogoCommand { get; }
        public IRelayCommand<Issue> OpenIssueDetailCommand { get; }

        public IRelayCommand<Issue> SetPlannedIssueCommand { get; }

        public IAsyncRelayCommand<Issue> AddStoryCommand { get; }
        public IAsyncRelayCommand<Issue> AddChildIssueCommand { get; }
        public IAsyncRelayCommand<Issue> UnlinkIssueCommand { get; }
        public IAsyncRelayCommand<Issue> UpdateIssueParentCommand { get; }
        public IAsyncRelayCommand<Issue> EditChildStoryCommand { get; }
        public IAsyncRelayCommand<Issue> DeleteChildStoryCommand { get; }

        public IRelayCommand<object> JumpToIssueCommand { get; }
        public IAsyncRelayCommand<Issue> ArchiveIssueCommand { get; }
        public IRelayCommand ToggleHubArchiveCommand { get; }
        public IAsyncRelayCommand<Issue> RestoreIssueCommand { get; }

        public System.Collections.ObjectModel.ObservableCollection<Issue> PotentialParents { get; } = new();

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var projects = await _dataService.GetProjectsAsync();
                Projects.Clear();
                foreach (var p in projects) Projects.Add(p);

                var collaborators = await _dataService.GetCollaboratorsAsync();
                Collaborators.Clear();
                foreach (var c in collaborators) Collaborators.Add(c);

                if (SelectedProject == null && Projects.Any())
                {
                    SelectedProject = Projects.First();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            _ = LoadProjectDataAsync();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            OnPropertyChanged(nameof(IsBoardViewVisible));
            RefreshCategorizedIssues();
        }


        private async Task LoadProjectDataAsync()
        {
            if (SelectedProject == null) return;

            IsLoading = true;
            try
            {
                var sprintsTask = _dataService.GetSprintsAsync(SelectedProject.Id);
                var issuesTask = _dataService.GetIssuesAsync(SelectedProject.Id);

                await Task.WhenAll(sprintsTask, issuesTask);

                Sprints.Clear();
                ArchivedSprints.Clear();
                foreach (var s in sprintsTask.Result.OrderBy(x => x.StartDate)) 
                {
                    if (s.IsActive) Sprints.Add(s);
                    else ArchivedSprints.Add(s);
                }
                
                if (SelectedSprint == null)
                {
                    SelectedSprint = Sprints.FirstOrDefault(s => s.IsCurrent) ?? Sprints.FirstOrDefault(s => s.IsActive);
                }

                _allProjectIssues = issuesTask.Result;
                RefreshCategorizedIssues();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshCategorizedIssues()
        {
            OpenItems.Clear();
            InProgressItems.Clear();
            TestingItems.Clear();
            FinishedItems.Clear();
            AllCategoryItems.Clear();

            // Clear children on all issues first
            foreach (var issue in _allProjectIssues) issue.Children.Clear();

            // Assign Friendly IDs to Ideas
            var ideas = _allProjectIssues.Where(i => i.Type == "Idea").OrderBy(i => i.CreatedAt).ToList();
            for (int i = 0; i < ideas.Count; i++) ideas[i].FriendlyId = i + 1;

            // Assign Friendly IDs to Stories
            var stories = _allProjectIssues.Where(i => i.Type == "Story").OrderBy(i => i.CreatedAt).ToList();
            for (int i = 0; i < stories.Count; i++) stories[i].FriendlyId = i + 1;

            // Assign Friendly IDs to Issues (Bugs/Features)
            var otherIssues = _allProjectIssues.Where(i => i.Type != "Idea" && i.Type != "Story").OrderBy(i => i.CreatedAt).ToList();
            for (int i = 0; i < otherIssues.Count; i++) otherIssues[i].FriendlyId = i + 1;

            // Build hierarchy and tags
            foreach (var issue in _allProjectIssues)
            {
                issue.ParentTitle = null; 
                issue.ParentFriendlyId = null;
                issue.ParentStoryFriendlyId = null;
                
                // Set Sprint Tag
                if (issue.SprintId == null)
                {
                    issue.SprintTag = "Unplanned";
                }
                else
                {
                    var sprint = Sprints.FirstOrDefault(s => s.Id == issue.SprintId) 
                                 ?? ArchivedSprints.FirstOrDefault(s => s.Id == issue.SprintId);
                    issue.SprintTag = sprint != null ? $"{sprint.Name} | {issue.Status}" : $"{issue.Status}";
                }
            }

            // Build Hierarchy (Stories -> Ideas)
            foreach (var story in _allProjectIssues.Where(i => i.Type == "Story"))
            {
                if (story.ParentIssueId.HasValue)
                {
                    var idea = _allProjectIssues.FirstOrDefault(p => p.Id == story.ParentIssueId);
                    if (idea != null)
                    {
                        idea.Children.Add(story);
                        story.ParentTitle = idea.Description;
                        story.ParentFriendlyId = idea.FriendlyId;
                    }
                }
            }

            // Build Hierarchy (Backlog Items -> Stories/Ideas)
            foreach (var issue in _allProjectIssues.Where(i => i.Type != "Story" && i.Type != "Idea"))
            {
                if (issue.ParentIssueId.HasValue)
                {
                    var parent = _allProjectIssues.FirstOrDefault(p => p.Id == issue.ParentIssueId);
                    if (parent != null)
                    {
                        parent.Children.Add(issue);
                        issue.ParentTitle = parent.Description;

                        if (parent.Type == "Story")
                        {
                            issue.ParentStoryFriendlyId = parent.FriendlyId;
                            // Trace back to Idea if the Story has one
                            if (parent.ParentFriendlyId.HasValue)
                            {
                                issue.ParentFriendlyId = parent.ParentFriendlyId;
                            }
                        }
                        else if (parent.Type == "Idea")
                        {
                            issue.ParentFriendlyId = parent.FriendlyId;
                        }
                    }
                }
            }

            // Refresh archivability
            foreach (var issue in _allProjectIssues)
            {
                if (issue.Type == "Idea" || issue.Type == "Story") issue.RefreshArchivability();
            }

            var filtered = _allProjectIssues
                .Where(i => i.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (SelectedCategory == "Hub" && ShowHubArchive)
            {
                filtered = filtered.Where(i => i.Status == "Archived").ToList();
            }
            else
            {
                filtered = filtered.Where(i => i.Status != "Archived").ToList();
            }

            if (SelectedCategory == "Backlog")
            {
                if (SelectedSprint != null)
                {
                    filtered = filtered.Where(i => i.SprintId == SelectedSprint.Id).ToList();
                }
                else
                {
                    filtered = filtered.Where(i => i.SprintId == null).ToList();
                }
            }

            foreach (var issue in filtered)
            {
                AllCategoryItems.Add(issue);
                switch (issue.Status?.ToLower())
                {
                    case "open": OpenItems.Add(issue); break;
                    case "in progress": InProgressItems.Add(issue); break;
                    case "in testing": TestingItems.Add(issue); break;
                    case "finished": FinishedItems.Add(issue); break;
                    default: OpenItems.Add(issue); break;
                }
            }
        }

        private void OpenProjectModal()
        {
            IsEditingProject = false;
            ProjectModalTitle = "Create New Project";
            NewProjectName = "";
            NewProjectDescription = "";
            NewProjectLogoUrl = "";
            IsProjectSelectorVisible = false;
            IsProjectModalVisible = true;
        }

        private void OpenIssueDetail(Issue? issue)
        {
            if (issue == null) return;
            var window = new Sleipnir.App.Views.IssueDetailWindow(issue, this);
            window.Show();
        }

        private void JumpToIssue(object? param)
        {
            if (param is Guid id)
            {
                var issue = _allProjectIssues.FirstOrDefault(i => i.Id == id);
                if (issue != null) OpenIssueDetail(issue);
            }
            else if (param is Issue issue)
            {
                OpenIssueDetail(issue);
            }
        }
        private void OpenEditProjectModal()
        {
            if (SelectedProject == null) return;
            IsEditingProject = true;
            ProjectModalTitle = "Edit Project";
            NewProjectName = SelectedProject.Name;
            NewProjectDescription = SelectedProject.Description;
            NewProjectLogoUrl = SelectedProject.LogoUrl ?? "";
            IsProjectSelectorVisible = false;
            IsProjectModalVisible = true;
        }

        private async Task SaveProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            IsLoading = true;
            try
            {
                if (IsEditingProject && SelectedProject != null)
                {
                    SelectedProject.Name = NewProjectName;
                    SelectedProject.Description = NewProjectDescription;
                    SelectedProject.LogoUrl = string.IsNullOrWhiteSpace(NewProjectLogoUrl) ? null : NewProjectLogoUrl;
                    await _dataService.UpdateProjectAsync(SelectedProject);
                    OnPropertyChanged(nameof(SelectedProject));
                }
                else
                {
                    var p = await _dataService.CreateProjectAsync(NewProjectName, NewProjectDescription, NewProjectLogoUrl);
                    Projects.Add(p);
                    SelectedProject = p;
                }
                IsProjectModalVisible = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BrowseLogo()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Select Project Logo"
            };
            if (dialog.ShowDialog() == true)
            {
                NewProjectLogoUrl = dialog.FileName;
            }
        }

        private void OpenSprintModal()
        {
            if (SelectedProject == null) return;
            IsEditingSprint = false;
            SprintModalTitle = "Plan New Sprint";
            NewSprintName = $"Sprint {Sprints.Count + 1}";
            NewSprintStartDate = DateTime.Today;
            NewSprintEndDate = DateTime.Today.AddDays(14);
            IsSprintModalVisible = true;
        }

        private void OpenEditSprintModal()
        {
            if (SelectedSprint == null) return;
            IsEditingSprint = true;
            SprintModalTitle = "Edit Sprint";
            NewSprintName = SelectedSprint.Name;
            NewSprintStartDate = SelectedSprint.StartDate;
            NewSprintEndDate = SelectedSprint.EndDate;
            IsSprintModalVisible = true;
        }

        private async Task SaveSprintAsync()
        {
            if (SelectedProject == null) return;

            if (IsEditingSprint)
            {
                if (SelectedSprint == null) return;
                SelectedSprint.Name = NewSprintName;
                SelectedSprint.StartDate = NewSprintStartDate;
                SelectedSprint.EndDate = NewSprintEndDate;
                await _dataService.UpdateSprintAsync(SelectedSprint);
            }
            else
            {
                // Deactivate existing active sprints
                var activeSprints = Sprints.Where(s => s.IsActive).ToList();
                foreach (var existingSprint in activeSprints)
                {
                    existingSprint.IsActive = false;
                    await _dataService.UpdateSprintAsync(existingSprint);
                }
                
                var s = new Sprint { 
                    ProjectId = SelectedProject.Id, 
                    Name = NewSprintName, 
                    StartDate = NewSprintStartDate, 
                    EndDate = NewSprintEndDate,
                    IsActive = true
                };
                
                await _dataService.CreateSprintAsync(s);
                SelectedSprint = s;
            }
            
            // Re-fetch sprints to ensure UI updates with the new state
            var updatedSprints = await _dataService.GetSprintsAsync(SelectedProject.Id);
            Sprints.Clear();
            foreach (var sprint in updatedSprints) Sprints.Add(sprint);
            
            // Re-select if we were editing or just created
            if (SelectedSprint != null)
            {
                var id = SelectedSprint.Id;
                SelectedSprint = Sprints.FirstOrDefault(sp => sp.Id == id);
            }

            IsSprintModalVisible = false;
        }

        private async Task DeleteSprintAsync()
        {
            if (SelectedSprint == null || SelectedProject == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {SelectedSprint.Name}? All associated issues will be unassigned.", 
                "Confirm Delete", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                // Unassign issues
                var affectedIssues = _allProjectIssues.Where(i => i.SprintId == SelectedSprint.Id).ToList();
                foreach (var issue in affectedIssues)
                {
                    issue.SprintId = null;
                    await _dataService.UpdateIssueAsync(issue);
                    await _dataService.AddLogAsync(new IssueLog { 
                        IssueId = issue.Id, 
                        Action = "Unassigned", 
                        Details = $"Removed from deleted sprint: {SelectedSprint.Name}" 
                    });
                }

                await _dataService.DeleteSprintAsync(SelectedSprint.Id);
                
                // Refresh data
                await LoadProjectDataAsync();
                SelectedSprint = null;
                IsSprintModalVisible = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task UpdateIssueAsync(Issue issue)
        {
            if (issue == null) return;
            IsLoading = true;
            try
            {
                await _dataService.UpdateIssueAsync(issue);

                // Auto-close Idea logic
                if (issue.Type == "Story" && issue.ParentIssueId.HasValue)
                {
                    await CheckAndCloseParentIdea(issue.ParentIssueId.Value);
                }

                RefreshCategorizedIssues();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateIssueAsync(string? status)
        {
            if (SelectedProject == null || string.IsNullOrEmpty(status))
            {
                if (SelectedProject == null) System.Windows.MessageBox.Show("Please select a project first.");
                return;
            }

            // If creating an idea, make sure we show the active Hub (not archive)
            if (SelectedCategory == "Hub")
            {
                ShowHubArchive = false;
            }

            var issue = new Issue
            {
                ProjectId = SelectedProject.Id,
                ProgramComponent = "",
                Description = "Descriptive Title",
                Category = SelectedCategory,
                Status = status,
                Type = SelectedCategory == "Hub" ? "Idea" : (SelectedCategory == "Pipeline" ? "Story" : "Bug"),
                SprintId = (SelectedCategory == "Backlog") ? SelectedSprint?.Id : null
            };

            await _dataService.CreateIssueAsync(issue);
            _allProjectIssues.Add(issue);
            RefreshCategorizedIssues();

            if (issue.Type == "Idea" || issue.Type == "Story")
            {
                OpenIssueDetail(issue);
            }
        }

        private async Task AddStoryAsync(Issue? idea)
        {
            if (idea == null || SelectedProject == null) return;

            var story = new Issue
            {
                ProjectId = SelectedProject.Id,
                ParentIssueId = idea.Id,
                ProgramComponent = idea.ProgramComponent,
                Description = "New Story",
                Category = "Pipeline",
                Type = "Story",
                Status = "Open",
                SprintId = null
            };

            await _dataService.CreateIssueAsync(story);
            _allProjectIssues.Add(story);
            RefreshCategorizedIssues();
            
            // Open immediately for editing
            OpenIssueDetail(story);
        }

        private async Task AddChildIssueAsync(Issue? story)
        {
            if (story == null || SelectedProject == null) return;

            var issue = new Issue
            {
                ProjectId = SelectedProject.Id,
                ParentIssueId = story.Id,
                ProgramComponent = story.ProgramComponent,
                Description = "New Backlog Issue",
                Category = "Backlog",
                Type = "Bug",
                Status = "Open"
            };

            await _dataService.CreateIssueAsync(issue);
            _allProjectIssues.Add(issue);
            RefreshCategorizedIssues();
        }

        private async Task AssignToSpecificSprintAsync(Sprint? sprint)
        {
            if (sprint == null || PlannedIssue == null) return;
            
            var issue = PlannedIssue;
            issue.SprintId = sprint.Id;

            await _dataService.UpdateIssueAsync(issue);
            await _dataService.AddLogAsync(new IssueLog { 
                IssueId = issue.Id, 
                Action = "Planned", 
                Details = $"Assigned to {sprint.Name}" 
            });

            PlannedIssue = null; // Clear context
            RefreshCategorizedIssues();
        }

        private async Task CompleteSprintAsync()
        {
            if (SelectedSprint == null || SelectedProject == null) return;

            IsLoading = true;
            try
            {
                SelectedSprint.IsActive = false;
                await _dataService.UpdateSprintAsync(SelectedSprint);

                var unfinished = _allProjectIssues
                    .Where(i => i.SprintId == SelectedSprint.Id && i.Status != "Finished")
                    .ToList();

                // Find the next chronologically active sprint, or create one if none exist
                var nextSprint = Sprints.Where(s => s.IsActive && s.Id != SelectedSprint.Id && s.StartDate >= SelectedSprint.EndDate)
                                       .OrderBy(s => s.StartDate)
                                       .FirstOrDefault();

                if (nextSprint == null)
                {
                    nextSprint = new Sprint
                    {
                        ProjectId = SelectedProject.Id,
                        Name = $"Sprint {Sprints.Count + ArchivedSprints.Count + 1}",
                        StartDate = SelectedSprint.EndDate.AddDays(1),
                        EndDate = SelectedSprint.EndDate.AddDays(15),
                        IsActive = true
                    };
                    await _dataService.CreateSprintAsync(nextSprint);
                }

                foreach (var issue in unfinished)
                {
                    issue.SprintId = nextSprint.Id;
                    await _dataService.UpdateIssueAsync(issue);
                    await _dataService.AddLogAsync(new IssueLog { 
                        IssueId = issue.Id, 
                        Action = "Rollover", 
                        Details = $"Moved from {SelectedSprint.Name} to {nextSprint.Name} (Unfinished)" 
                    });
                }

                // Important: refresh both lists
                var updatedSprints = await _dataService.GetSprintsAsync(SelectedProject.Id);
                Sprints.Clear();
                ArchivedSprints.Clear();
                foreach (var s in updatedSprints.OrderBy(x => x.StartDate)) 
                {
                    if (s.IsActive) Sprints.Add(s);
                    else ArchivedSprints.Add(s);
                }

                SelectedSprint = Sprints.FirstOrDefault(sp => sp.Id == nextSprint.Id);
                RefreshCategorizedIssues();
                
                System.Windows.MessageBox.Show($"{unfinished.Count} unfinished issues moved to {nextSprint.Name}.", "Sprint Completed");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UnlinkIssueAsync(Issue? issue)
        {
            if (issue == null) return;
            var oldParentId = issue.ParentIssueId;
            issue.ParentIssueId = null;
            await _dataService.UpdateIssueAsync(issue);
            
            if (oldParentId.HasValue) await CheckAndCloseParentIdea(oldParentId.Value);

            RefreshCategorizedIssues();
        }

        private async Task CheckAndCloseParentIdea(Guid parentId)
        {
            var parentIdea = _allProjectIssues.FirstOrDefault(i => i.Id == parentId && i.Type == "Idea");
            if (parentIdea != null && parentIdea.Status != "Finished")
            {
                var allChildren = _allProjectIssues.Where(i => i.ParentIssueId == parentId && i.Type == "Story").ToList();
                if (allChildren.Count > 0 && allChildren.All(c => c.Status == "Finished"))
                {
                    parentIdea.Status = "Finished";
                    await _dataService.UpdateIssueAsync(parentIdea);
                }
            }
        }

        private async Task UpdateIssueParentAsync(Issue? issue)
        {
            if (issue == null) return;
            await _dataService.UpdateIssueAsync(issue);
            RefreshCategorizedIssues();
        }

        public void LoadPotentialParents(Issue issue)
        {
            PotentialParents.Clear();
            if (issue.Type == "Story")
            {
                // Stories link to Ideas
                var ideas = _allProjectIssues.Where(i => i.Type == "Idea" && i.Status != "Archived" && i.Id != issue.Id).ToList();
                foreach (var idea in ideas) PotentialParents.Add(idea);
            }
            else if (issue.Type != "Idea")
            {
                // Issues can ONLY be linked to stories
                var parents = _allProjectIssues.Where(i => i.Type == "Story" 
                    && i.Status != "Archived" && i.Id != issue.Id)
                    .OrderBy(i => i.CreatedAt)
                    .ToList();
                foreach (var p in parents) PotentialParents.Add(p);
            }
        }

        private async Task EditChildStoryAsync(Issue? story)
        {
            if (story == null) return;
            OpenIssueDetail(story);
        }

        private async Task DeleteChildStoryAsync(Issue? story)
        {
            if (story == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to permanently delete the story '{story.Description}' AND all its linked backlog issues?", 
                "Delete Story & Linked Items", 
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            var oldParentId = story.ParentIssueId;

            // Delete linked backlog issues first
            var linkedIssues = _allProjectIssues.Where(i => i.ParentIssueId == story.Id).ToList();
            foreach (var child in linkedIssues)
            {
                await _dataService.DeleteIssueAsync(child.Id);
                _allProjectIssues.Remove(child);
            }

            // Delete the story itself
            await _dataService.DeleteIssueAsync(story.Id);
            _allProjectIssues.Remove(story);

            if (oldParentId.HasValue) await CheckAndCloseParentIdea(oldParentId.Value);

            RefreshCategorizedIssues();
        }

        private async Task ShowLogsAsync(Issue? issue)
        {
            if (issue == null) return;
            var logs = await _dataService.GetLogsAsync(issue.Id);
            string logText = string.Join("\n", logs.Select(l => $"[{l.Timestamp:HH:mm}] {l.UserName}: {l.Action} ({l.Details})"));
            System.Windows.MessageBox.Show(logText, $"Timeline for: {issue.Description}");
        }

        private async Task ArchiveIssueAsync(Issue? issue)
        {
            if (issue == null) return;

            // Idea/Story logic
            if ((issue.Type == "Idea" || issue.Type == "Story") && issue.Children.Any())
            {
                var itemPlural = issue.Type == "Idea" ? "stories" : "backlog issues";
                var result = System.Windows.MessageBox.Show(
                    $"This {issue.Type} has linked {itemPlural}. Do you want to Archive them as well or Unlink them?\n\nSelect 'Yes' to ARCHIVE linked {itemPlural}.\nSelect 'No' to UNLINK items and keep them in Archive.", 
                    $"Archive Linked {issue.Type}?", 
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Cancel) return;

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    foreach (var child in issue.Children.ToList())
                    {
                        child.Status = "Archived";
                        await _dataService.UpdateIssueAsync(child);
                    }
                }
                else if (result == System.Windows.MessageBoxResult.No)
                {
                    foreach (var child in issue.Children.ToList())
                    {
                        child.ParentIssueId = null;
                        await _dataService.UpdateIssueAsync(child);
                    }
                }
            }

            issue.Status = "Archived";
            await _dataService.UpdateIssueAsync(issue);
            RefreshCategorizedIssues();
        }

        [RelayCommand]
        private async Task DeleteMainIssueAsync(Issue? issue)
        {
            if (issue == null) return;

            // Idea/Story logic
            if ((issue.Type == "Idea" || issue.Type == "Story") && issue.Children.Any())
            {
                var itemPlural = issue.Type == "Idea" ? "stories" : "backlog issues";
                var result = System.Windows.MessageBox.Show(
                    $"This {issue.Type} has linked {itemPlural}. Do you want to DELETE them as well or just UNLINK them?\n\nSelect 'Yes' to DELETE linked {itemPlural}.\nSelect 'No' to UNLINK {itemPlural}.", 
                    "Delete Linked Items?",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Cancel) return;

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    foreach (var child in issue.Children.ToList())
                    {
                        await _dataService.DeleteIssueAsync(child.Id);
                        _allProjectIssues.Remove(child);
                    }
                }
                else if (result == System.Windows.MessageBoxResult.No)
                {
                    foreach (var child in issue.Children.ToList())
                    {
                        child.ParentIssueId = null;
                        await _dataService.UpdateIssueAsync(child);
                    }
                }
            }
            else
            {
                var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete '{issue.Description}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;
            }

            await DeleteIssueDirectAsync(issue);
        }

        public async Task DeleteIssueDirectAsync(Issue? issue)
        {
            if (issue == null) return;
            await _dataService.DeleteIssueAsync(issue.Id);
            _allProjectIssues.Remove(issue);
            RefreshCategorizedIssues();
        }

        private async Task RestoreIssueAsync(Issue? issue)
        {
            if (issue == null) return;
            issue.Status = "Finished"; // Restore to finished state
            await _dataService.UpdateIssueAsync(issue);
            RefreshCategorizedIssues();
        }
    }
}
