using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sleipnir.App.Models;
using Sleipnir.App.Services;
using Sleipnir.App.Utils;
using Sleipnir.App.Views;

namespace Sleipnir.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private AppUser? _currentUser;
        
        [ObservableProperty]
        private bool _isUserManagementVisible;

        [ObservableProperty]
        private bool _isAdminEmojiPickerVisible;

        [ObservableProperty]
        private string _enteredOtpCode = string.Empty;

        [ObservableProperty]
        private string _emojiSearchQuery = string.Empty;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedTypeFilter = "All";
        [ObservableProperty]
        private string _selectedPriorityFilter = "All";
        [ObservableProperty]
        private string _selectedAssigneeFilter = "All";

        partial void OnSearchQueryChanged(string value) => RefreshCategorizedIssues();
        partial void OnSelectedTypeFilterChanged(string value) => RefreshCategorizedIssues();
        partial void OnSelectedPriorityFilterChanged(string value) => RefreshCategorizedIssues();
        partial void OnSelectedAssigneeFilterChanged(string value) => RefreshCategorizedIssues();

        [RelayCommand]
        public void ClearFilters()
        {
            SearchQuery = string.Empty;
            SelectedTypeFilter = "All";
            SelectedPriorityFilter = "All";
            SelectedAssigneeFilter = "All";
        }

        public ObservableCollection<string> AvailableEmojis { get; } = new();
        private List<IconItem> _allEmojis = new();

        private AppUser? _editingUser;
        private readonly EmailService _emailService = new();

        [ObservableProperty]
        private string _editingUserEmoji = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<AppUser> _allUsers = new();

        private readonly IDataService _dataService;
        public IDataService DataService => _dataService;
        private List<Issue> _allProjectIssues = new();

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private string _selectedCategory = "Backlog"; 

        public string CurrentViewTitle => SelectedSprint == null ? "Project Pool" : SelectedSprint.Name;

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
        private bool _areCardsCollapsed;

        [ObservableProperty]
        private ObservableCollection<Collaborator> _collaborators = new();
        public List<string> Kinds { get; } = new() { "Bug", "Feature", "Patch", "Overhaul", "Alteration" };
        public List<string> AllKinds { get; } = new() { "Bug", "Feature", "Patch", "Overhaul", "Alteration", "Story", "Epic" };
        public List<string> FilterKinds { get; }

        public List<string> Priorities { get; } = new() { "Critical", "Very High", "High", "Neutral", "Low", "Very Low", "Nice to Have" };
        public List<string> FilterPriorities { get; }

        public ObservableCollection<string> FilterAssignees { get; } = new() { "All" };

        partial void OnCollaboratorsChanged(ObservableCollection<Collaborator> value)
        {
            UpdateFilterAssignees();
        }

        private void UpdateFilterAssignees()
        {
            FilterAssignees.Clear();
            FilterAssignees.Add("All");
            if (Collaborators != null)
            {
                foreach (var c in Collaborators.OrderBy(c => c.Name))
                {
                    FilterAssignees.Add(c.Name);
                }
            }
        }

        [RelayCommand]
        private void ConfirmLogout()
        {
            if (CustomDialogWindow.Show("LOGOUT", "Are you sure you want to log out? You will need to enter your credentials next time.", CustomDialogWindow.DialogType.Info, "Logout", "Cancel") == CustomDialogWindow.CustomDialogResult.Ok)
            {
                LogoutCommand.Execute(null);
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            if (CurrentUser != null)
            {
                CurrentUser.CanAutoLogin = false;
                await _dataService.UpdateUserAsync(CurrentUser);
            }
            System.Windows.Application.Current.Shutdown();
        }

        [RelayCommand]
        private void ShowEmojiPicker(AppUser user)
        {
            _editingUser = user;
            EditingUserEmoji = user.Emoji;
            if (_allEmojis.Count == 0) _allEmojis = EmojiHelper.GetAllEmojis();
            RefreshEmojiList();
            IsAdminEmojiPickerVisible = true;
        }

        private void RefreshEmojiList()
        {
            AvailableEmojis.Clear();
            var filtered = string.IsNullOrWhiteSpace(EmojiSearchQuery) 
                ? _allEmojis 
                : _allEmojis.Where(e => e.Id.Contains(EmojiSearchQuery, StringComparison.OrdinalIgnoreCase) || e.Name.Contains(EmojiSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var e in filtered.Take(5000)) AvailableEmojis.Add(e.Id);
        }

        partial void OnEmojiSearchQueryChanged(string value) => RefreshEmojiList();

        partial void OnEditingUserEmojiChanged(string value)
        {
            if (_editingUser != null && !string.IsNullOrEmpty(value))
            {
                _editingUser.Emoji = value;
                IsAdminEmojiPickerVisible = false;
            }
        }

        [RelayCommand]
        private void CloseAdminEmojiPicker() => IsAdminEmojiPickerVisible = false;

        [RelayCommand]
        private async Task ToggleUserManagement()
        {
            if (CurrentUser == null || !CurrentUser.IsSuperuser) return;
            IsUserManagementVisible = !IsUserManagementVisible;
            if (IsUserManagementVisible)
            {
                var users = await _dataService.GetUsersAsync();
                AllUsers.Clear();
                foreach (var u in users) AllUsers.Add(u);
            }
        }

        [RelayCommand]
        private async Task CreateUser()
        {
            try 
            {
                var newUser = new AppUser
                {
                    FirstName = "New",
                    LastName = "User",
                    Username = "user_" + Guid.NewGuid().ToString().Substring(0, 4),
                    Password = "password",
                    Emoji = "👤"
                };
                await _dataService.CreateUserAsync(newUser);
                AllUsers.Add(newUser);
            }
            catch (Exception ex)
            {
                string msg = "Failed to create new user.\n\nError: " + ex.Message;
                if (ex.Message.Contains("42703") || ex.Message.Contains("column"))
                {
                    msg += "\n\nTip: Your database is missing the NEW permission columns. Please run the SQL migration script in Supabase.";
                }
                CustomDialogWindow.Show("Database Error", msg, CustomDialogWindow.DialogType.Error);
            }
        }

        [RelayCommand]
        private async Task ToggleProjectAccess(object? parameter)
        {
            if (parameter is object[] values && values.Length == 2 && values[0] is AppUser user && values[1] is Project project)
            {
                var projectId = project.Id.ToString();
                var currentIds = (user.AllowedProjectIds ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                             .Select(s => s.Trim())
                                                             .Where(s => !string.IsNullOrEmpty(s))
                                                             .ToList();
                
                if (currentIds.Contains(projectId))
                {
                    currentIds.Remove(projectId);
                }
                else
                {
                    currentIds.Add(projectId);
                }
                
                user.AllowedProjectIds = string.Join(";", currentIds);
                await _dataService.UpdateUserAsync(user);
            }
        }

        [RelayCommand]
        private async Task SendVerification(AppUser user)
        {
            if (string.IsNullOrWhiteSpace(user.PendingEmail))
            {
                CustomDialogWindow.Show("Input Required", "Please enter an email address first.", CustomDialogWindow.DialogType.Warning);
                return;
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            user.VerificationCode = otp;
            user.IsEmailVerified = false;

            bool success = await _emailService.SendOtpEmailAsync(user.PendingEmail, otp);
            
            if (success)
            {
                await _dataService.UpdateUserAsync(user);
                CustomDialogWindow.Show("Verification Sent", $"Verification code sent to {user.PendingEmail}!\n\nPlease check your inbox and enter the code.", CustomDialogWindow.DialogType.Success);
            }
            else
            {
                CustomDialogWindow.Show("Email Error", "Failed to send email. Ensure you have set your Resend API Key in EmailService.cs", CustomDialogWindow.DialogType.Error);
            }
        }

        [RelayCommand]
        private async Task VerifyOtp(AppUser user)
        {
            if (string.IsNullOrEmpty(EnteredOtpCode)) return;

            if (user.VerificationCode == EnteredOtpCode)
            {
                user.Email = user.PendingEmail;
                user.IsEmailVerified = true;
                user.VerificationCode = string.Empty; // Clear code after use
                await _dataService.UpdateUserAsync(user);
                EnteredOtpCode = string.Empty;
                CustomDialogWindow.Show("Verification Complete", "Email verified successfully! Profile fields are now unlocked.", CustomDialogWindow.DialogType.Success);
            }
            else
            {
                CustomDialogWindow.Show("Error", "Invalid verification code. Please try again.", CustomDialogWindow.DialogType.Error);
            }
        }

        [RelayCommand]
        private async Task SaveUser(AppUser user)
        {
            try 
            {
                if (user.IsOriginalAdmin)
                {
                    // Fetch fresh state for comparison
                    var users = await _dataService.GetUsersAsync();
                    var dbUser = users.FirstOrDefault(u => u.Id == user.Id);
                    
                    if (dbUser != null)
                    {
                        bool hasProtectedChanges = dbUser.Password != user.Password || 
                                                  dbUser.Username != user.Username || 
                                                  dbUser.Emoji != user.Emoji ||
                                                  dbUser.FirstName != user.FirstName ||
                                                  dbUser.LastName != user.LastName;

                        if (hasProtectedChanges && !user.IsEmailVerified)
                        {
                            CustomDialogWindow.Show("Verification Required", "Profile is locked. You must verify your email address to change Username, Password, Name, or Icon.", CustomDialogWindow.DialogType.Error);
                            
                            // Revert protected changes from DB
                            user.Password = dbUser.Password;
                            user.Username = dbUser.Username;
                            user.Emoji = dbUser.Emoji;
                            user.FirstName = dbUser.FirstName;
                            user.LastName = dbUser.LastName;
                            return;
                        }

                        // If email changed, mark as unverified (Sanitized check)
                        if (!string.Equals(user.PendingEmail?.Trim(), dbUser.PendingEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            user.IsEmailVerified = false;
                        }
                    }
                    user.IsSuperuser = true; // Force admin status
                }

                await _dataService.UpdateUserAsync(user);

                // Sync CurrentUser (sidebar profile) if we just edited ourselves
                if (CurrentUser != null && CurrentUser.Id == user.Id)
                {
                    CurrentUser.Username = user.Username;
                    CurrentUser.Password = user.Password;
                    CurrentUser.FirstName = user.FirstName;
                    CurrentUser.LastName = user.LastName;
                    CurrentUser.Emoji = user.Emoji;
                }

                CustomDialogWindow.Show("Success", "User saved successfully!", CustomDialogWindow.DialogType.Success);
            }
            catch (Exception ex)
            {
                string msg = "Failed to save user changes.\n\nError: " + ex.Message;
                if (ex.Message.Contains("42703") || ex.Message.Contains("column"))
                {
                    msg += "\n\nTip: You likely need to run the SQL script for 'is_root' and 'verification_code' columns and RELOAD the schema in Supabase.";
                }
                CustomDialogWindow.Show("Database Error", msg, CustomDialogWindow.DialogType.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteUser(AppUser user)
        {
            if (user.IsOriginalAdmin)
            {
                CustomDialogWindow.Show("Restricted Action", "The original superuser cannot be deleted.", CustomDialogWindow.DialogType.Error);
                return;
            }
            if (user.Id == CurrentUser?.Id) return; // Can't delete self via normal list
            await _dataService.DeleteUserAsync(user.Id);
            AllUsers.Remove(user);
        }

        [RelayCommand]
        private void CopyProjectId(Project project)
        {
            System.Windows.Clipboard.SetText(project.Id.ToString());
            CustomDialogWindow.Show("Copied", "Project ID copied to clipboard: " + project.Id, CustomDialogWindow.DialogType.Success);
        }
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
        private bool _showPipelineArchive;
        [ObservableProperty]
        private bool _showBacklogArchive;

        [ObservableProperty]
        private Issue? _plannedIssue;

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            FilterKinds = new List<string> { "All" }.Concat(AllKinds).ToList();
            FilterPriorities = new List<string> { "All" }.Concat(Priorities).ToList();
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
            TogglePipelineArchiveCommand = new RelayCommand(() => { ShowPipelineArchive = !ShowPipelineArchive; RefreshCategorizedIssues(); });
            ToggleBacklogArchiveCommand = new RelayCommand(() => { ShowBacklogArchive = !ShowBacklogArchive; RefreshCategorizedIssues(); });
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
            ClearLogoCommand = new RelayCommand(() => { NewProjectLogoUrl = ""; if (SelectedProject != null) SelectedProject.LogoData = null; });
            ShowHierarchyCommand = new RelayCommand<Issue>(ShowHierarchy);

            AddStoryCommand = new AsyncRelayCommand<Issue>(AddStoryAsync);
            AddChildIssueCommand = new AsyncRelayCommand<Issue>(AddChildIssueAsync);
            UnlinkIssueCommand = new AsyncRelayCommand<Issue>(UnlinkIssueAsync);
            UpdateIssueParentCommand = new AsyncRelayCommand<Issue>(UpdateIssueParentAsync);
            EditChildStoryCommand = new AsyncRelayCommand<Issue>(EditChildStoryAsync);
            DeleteChildStoryCommand = new AsyncRelayCommand<Issue>(DeleteChildStoryAsync);
            JumpToIssueCommand = new RelayCommand<object>(JumpToIssue);
            ArchiveIssueCommand = new AsyncRelayCommand<Issue>(ArchiveIssueAsync);
            OpenUserPermissionsCommand = new RelayCommand<AppUser>(OpenUserPermissions);
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
        public IRelayCommand<Issue> ShowHierarchyCommand { get; }

        public IAsyncRelayCommand<Issue> AddStoryCommand { get; }
        public IAsyncRelayCommand<Issue> AddChildIssueCommand { get; }
        public IAsyncRelayCommand<Issue> UnlinkIssueCommand { get; }
        public IAsyncRelayCommand<Issue> UpdateIssueParentCommand { get; }
        public IAsyncRelayCommand<Issue> EditChildStoryCommand { get; }
        public IAsyncRelayCommand<Issue> DeleteChildStoryCommand { get; }

        public IRelayCommand<object> JumpToIssueCommand { get; }
        public IAsyncRelayCommand<Issue> ArchiveIssueCommand { get; }
        public IRelayCommand<AppUser> OpenUserPermissionsCommand { get; }
        public IRelayCommand ToggleHubArchiveCommand { get; }
        public IRelayCommand TogglePipelineArchiveCommand { get; }
        public IRelayCommand ToggleBacklogArchiveCommand { get; }
        public IAsyncRelayCommand<Issue> RestoreIssueCommand { get; }

        public System.Collections.ObjectModel.ObservableCollection<Issue> PotentialParents { get; } = new();

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var allProjects = await _dataService.GetProjectsAsync();
                Projects.Clear();
                foreach (var p in allProjects)
                {
                    if (CurrentUser != null && CurrentUser.HasAccessToProject(p.Id))
                    {
                        Projects.Add(p);
                    }
                }
                
                // Load all users and convert to collaborators
                var allUsers = await _dataService.GetUsersAsync();
                AllUsers.Clear();
                Collaborators.Clear();
                foreach (var user in allUsers)
                {
                    AllUsers.Add(user);
                    // Convert AppUser to Collaborator for assignee selection
                    Collaborators.Add(new Collaborator
                    {
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                        Email = user.Username, // Using username as email placeholder
                        Emoji = user.Emoji
                    });
                }
                UpdateFilterAssignees();

                // Restore project selection
                if (Projects.Any())
                {
                    if (CurrentUser != null && CurrentUser.LastProjectId.HasValue)
                    {
                        var lastProject = Projects.FirstOrDefault(p => p.Id == CurrentUser.LastProjectId.Value);
                        SelectedProject = lastProject ?? Projects.First();
                    }
                    else if (SelectedProject == null)
                    {
                        SelectedProject = Projects.First();
                    }
                }
                else
                {
                    OpenProjectModal();
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
            if (value != null && CurrentUser != null && CurrentUser.LastProjectId != value.Id)
            {
                SaveLastProjectSelection(value.Id);
            }
        }

        private async void SaveLastProjectSelection(Guid projectId)
        {
            if (CurrentUser == null) return;
            try
            {
                CurrentUser.LastProjectId = projectId;
                await _dataService.UpdateUserAsync(CurrentUser);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project preference: {ex.Message}");
                // If the update fails, it's likely due to a missing column in Supabase
                if (ex.Message.Contains("last_project_id") || ex.Message.Contains("column"))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        CustomDialogWindow.Show("Database Update Needed", "Failed to save project preference. You likely need to add the 'last_project_id' column to your 'users' table in Supabase and reload the schema.", CustomDialogWindow.DialogType.Warning);
                    });
                }
            }
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
            _isDataSyncing = true;
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
                _isDataSyncing = false;
                IsLoading = false;
            }
        }


        private void RefreshCategorizedIssues()
        {
            try 
            {
                OpenItems.Clear();
                InProgressItems.Clear();
                TestingItems.Clear();
                FinishedItems.Clear();
                AllCategoryItems.Clear();

                if (_allProjectIssues == null) return;

            // Clear children on all issues first
            foreach (var issue in _allProjectIssues) issue.Children.Clear();

            // Assign Friendly IDs to Epics
            var epics = _allProjectIssues.Where(i => i.Type == "Epic").OrderBy(i => i.CreatedAt).ToList();
            for (int i = 0; i < epics.Count; i++) epics[i].FriendlyId = i + 1;

            // Assign Friendly IDs to Stories
            var stories = _allProjectIssues.Where(i => i.Type == "Story").OrderBy(i => i.CreatedAt).ToList();
            for (int i = 0; i < stories.Count; i++) stories[i].FriendlyId = i + 1;

            // Assign Friendly IDs to Issues (Bugs/Features)
            var otherIssues = _allProjectIssues.Where(i => i.Type != "Epic" && i.Type != "Story").OrderBy(i => i.CreatedAt).ToList();
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
                
                // Pre-populate Parent titles if available
                if (issue.ParentIssueId.HasValue)
                {
                    var parent = _allProjectIssues.FirstOrDefault(p => p.Id == issue.ParentIssueId);
                    if (parent != null)
                    {
                        issue.ParentTitle = parent.Description;
                    }
                }
            }

            // Build Hierarchy (Stories -> Epics)
            foreach (var story in _allProjectIssues.Where(i => i.Type == "Story"))
            {
                if (story.ParentIssueId.HasValue)
                {
                    var epic = _allProjectIssues.FirstOrDefault(p => p.Id == story.ParentIssueId);
                    if (epic != null)
                    {
                        epic.Children.Add(story);
                        story.ParentTitle = epic.Description;
                        story.ParentFriendlyId = epic.FriendlyId;
                    }
                }
            }

            // Build Hierarchy (Backlog Items -> Stories/Epics)
            foreach (var issue in _allProjectIssues.Where(i => i.Type != "Story" && i.Type != "Epic"))
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
                            // Trace back to Epic if the Story has one
                            if (parent.ParentFriendlyId.HasValue)
                            {
                                issue.ParentFriendlyId = parent.ParentFriendlyId;
                            }
                        }
                        else if (parent.Type == "Epic")
                        {
                            issue.ParentFriendlyId = parent.FriendlyId;
                        }
                    }
                }
            }

            // Refresh archivability
            foreach (var issue in _allProjectIssues)
            {
                if (issue.Type == "Epic" || issue.Type == "Story") issue.RefreshArchivability();
            }

            var filtered = _allProjectIssues
                .Where(i => i.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (SelectedCategory == "Hub" && ShowHubArchive)
            {
                filtered = filtered.Where(i => i.Status == "Archived").ToList();
            }
            else if (SelectedCategory == "Pipeline" && ShowPipelineArchive)
            {
                filtered = filtered.Where(i => i.Status == "Archived").ToList();
            }
            else if (SelectedCategory == "Backlog" && ShowBacklogArchive)
            {
                filtered = filtered.Where(i => i.Status == "Archived").ToList();
            }
            else
            {
                filtered = filtered.Where(i => i.Status != "Archived").ToList();

                // Only apply category-specific sub-filters when NOT in archive mode
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
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(i => 
                    (i.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.LongDescription?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.IssueNumber?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.EpicNumber?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.StoryNumber?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.ResponsibleUsers?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            // Apply Specific Filters
            if (!string.IsNullOrEmpty(SelectedTypeFilter) && SelectedTypeFilter != "All")
            {
                filtered = filtered.Where(i => i.Type == SelectedTypeFilter).ToList();
            }
            if (!string.IsNullOrEmpty(SelectedPriorityFilter) && SelectedPriorityFilter != "All")
            {
                filtered = filtered.Where(i => i.Priority == SelectedPriorityFilter).ToList();
            }
            if (!string.IsNullOrEmpty(SelectedAssigneeFilter) && SelectedAssigneeFilter != "All")
            {
                filtered = filtered.Where(i => i.ResponsibleUsers?.Contains(SelectedAssigneeFilter, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RefreshCategorizedIssues Error: " + ex.Message);
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

        private void OpenUserPermissions(AppUser? user)
        {
            if (user == null) return;
            var window = new Sleipnir.App.Views.UserPermissionsWindow(user, this);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
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

        [RelayCommand]
        private async Task DeleteProject()
        {
            if (SelectedProject == null) return;

            if (CustomDialogWindow.Show("DELETE PROJECT", $"Are you sure you want to permanently delete the project '{SelectedProject.Name}'? This will also delete all its sprints and issues. This action cannot be undone.", CustomDialogWindow.DialogType.Warning, "Delete", "Cancel") == CustomDialogWindow.CustomDialogResult.Ok)
            {
                await _dataService.DeleteProjectAsync(SelectedProject.Id);
                var projectToRemove = SelectedProject;
                Projects.Remove(projectToRemove);
                
                if (Projects.Any())
                {
                    SelectedProject = Projects.First();
                }
                else
                {
                    _allProjectIssues.Clear();
                    RefreshCategorizedIssues();
                    SelectedProject = null;
                    OpenProjectModal();
                }
            }
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
                    
                    // Save image data if it's a local file
                    if (!string.IsNullOrEmpty(NewProjectLogoUrl) && File.Exists(NewProjectLogoUrl))
                    {
                        var bytes = File.ReadAllBytes(NewProjectLogoUrl);
                        SelectedProject.LogoData = Convert.ToBase64String(bytes);
                    }

                    await _dataService.UpdateProjectAsync(SelectedProject);
                    OnPropertyChanged(nameof(SelectedProject));
                }
                else
                {
                    var p = new Project { Name = NewProjectName, Description = NewProjectDescription, LogoUrl = NewProjectLogoUrl };
                    if (!string.IsNullOrEmpty(NewProjectLogoUrl) && File.Exists(NewProjectLogoUrl))
                    {
                        var bytes = File.ReadAllBytes(NewProjectLogoUrl);
                        p.LogoData = Convert.ToBase64String(bytes);
                    }
                    
                    var created = await _dataService.CreateProjectAsync(p.Name, p.Description, p.LogoUrl, p.LogoData);

                    Projects.Add(created);
                    SelectedProject = created;
                }
                IsProjectModalVisible = false;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.Show("Error", "Failed to save project: " + ex.Message, CustomDialogWindow.DialogType.Error);
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

            var result = CustomDialogWindow.Show(
                "Confirm Delete",
                $"Are you sure you want to delete {SelectedSprint.Name}? All associated issues will be unassigned.", 
                CustomDialogWindow.DialogType.Warning,
                "Delete", "Cancel");

            if (result != CustomDialogWindow.CustomDialogResult.Ok) return;

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
                        UserName = CurrentUser?.FullName ?? "System",
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

        private bool _isDataSyncing;

        public async Task ChangeIssueStatusAsync(Issue? issue, string newStatus)
        {
            if (issue == null || issue.Status == newStatus || _isDataSyncing) return;

            var oldStatus = issue.Status;
            issue.Status = newStatus;

            // Log the change
            await LogIssueChange(issue.Id, "Status", oldStatus, newStatus);

            // Update in DB
            await UpdateIssueAsync(issue);
        }

        public async Task UpdateIssueAsync(Issue issue)
        {
            if (issue == null || _isDataSyncing) return;
            IsLoading = true;
            try
            {
                await _dataService.UpdateIssueAsync(issue);

                // Auto-close Epic logic
                if (issue.Type == "Story" && issue.ParentIssueId.HasValue)
                {
                    await CheckAndCloseParentEpic(issue.ParentIssueId.Value);
                }

                RefreshCategorizedIssues();
            }
            catch (Exception ex)
            {
                string msg = $"Failed to update issue: {ex.Message}";
                if (ex.Message.Contains("42703") || ex.Message.Contains("column"))
                    msg += "\n\nTip: Database schema mismatch. Please run the SQL migration script.";
                
                CustomDialogWindow.Show("Error", msg, CustomDialogWindow.DialogType.Error);
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
                if (SelectedProject == null) CustomDialogWindow.Show("Project Required", "Please select a project first.", CustomDialogWindow.DialogType.Warning);
                return;
            }

            IsLoading = true;
            try 
            {
                // If creating an epic, make sure we show the active Hub (not archive)
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
                    Type = SelectedCategory == "Hub" ? "Epic" : (SelectedCategory == "Pipeline" ? "Story" : "Bug"),
                    SprintId = (SelectedCategory == "Backlog") ? SelectedSprint?.Id : null
                };

                var created = await _dataService.CreateIssueAsync(issue);
                if (created != null)
                {
                    _allProjectIssues.Add(created);
                    await LogIssueChange(created.Id, "Status", null, status);
                    RefreshCategorizedIssues();
                    OpenIssueDetail(created);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Failed to create issue: {ex.Message}";
                if (ex.Message.Contains("42703") || ex.Message.Contains("column"))
                    msg += "\n\nTip: Database schema mismatch. Please run the SQL migration script.";
                
                CustomDialogWindow.Show("Database Error", msg, CustomDialogWindow.DialogType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddStoryAsync(Issue? epic)
        {
            if (epic == null || SelectedProject == null) return;

            IsLoading = true;
            try 
            {
                var story = new Issue
                {
                    ProjectId = SelectedProject.Id,
                    ParentIssueId = epic.Id,
                    ProgramComponent = epic.ProgramComponent,
                    Description = "New Story",
                    Category = "Pipeline",
                    Type = "Story",
                    Status = "Open",
                    SprintId = null
                };

                var created = await _dataService.CreateIssueAsync(story);
                if (created != null)
                {
                    _allProjectIssues.Add(created);
                    await LogIssueChange(created.Id, "Status", null, "Open");
                    RefreshCategorizedIssues();
                    OpenIssueDetail(created);
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.Show("Error", $"Failed to created story: {ex.Message}", CustomDialogWindow.DialogType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddChildIssueAsync(Issue? story)
        {
            if (story == null || SelectedProject == null) return;

            IsLoading = true;
            try 
            {
                var issue = new Issue
                {
                    ProjectId = SelectedProject.Id,
                    ParentIssueId = story.Id,
                    ProgramComponent = story.ProgramComponent,
                    Description = "New Issue",
                    Category = "Backlog",
                    Type = "Bug",
                    Status = "Open"
                };

                var created = await _dataService.CreateIssueAsync(issue);
                if (created != null)
                {
                    _allProjectIssues.Add(created);
                    await LogIssueChange(created.Id, "Status", null, "Open");
                    RefreshCategorizedIssues();
                    OpenIssueDetail(created);
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.Show("Error", $"Failed to created issue: {ex.Message}", CustomDialogWindow.DialogType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AssignToSpecificSprintAsync(Sprint? sprint)
        {
            if (sprint == null || PlannedIssue == null) return;
            
            var issue = PlannedIssue;
            issue.SprintId = sprint.Id;

            await _dataService.UpdateIssueAsync(issue);
            await _dataService.AddLogAsync(new IssueLog { 
                IssueId = issue.Id, 
                UserName = CurrentUser?.FullName ?? "System",
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
                        UserName = CurrentUser?.FullName ?? "System",
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
                
                CustomDialogWindow.Show("Sprint Completed", $"{unfinished.Count} unfinished issues moved to {nextSprint.Name}.", CustomDialogWindow.DialogType.Success);
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
            
            if (oldParentId.HasValue) await CheckAndCloseParentEpic(oldParentId.Value);

            RefreshCategorizedIssues();
        }

        private async Task CheckAndCloseParentEpic(Guid parentId)
        {
            var parentEpic = _allProjectIssues.FirstOrDefault(i => i.Id == parentId && i.Type == "Epic");
            if (parentEpic != null && parentEpic.Status != "Finished")
            {
                var allChildren = _allProjectIssues.Where(i => i.ParentIssueId == parentId && i.Type == "Story").ToList();
                if (allChildren.Count > 0 && allChildren.All(c => c.Status == "Finished"))
                {
                    var oldStatus = parentEpic.Status;
                    parentEpic.Status = "Finished";
                    await LogIssueChange(parentEpic.Id, "Status", oldStatus, "Finished");
                    await _dataService.UpdateIssueAsync(parentEpic);
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
                // Stories link to Epics
                var epics = _allProjectIssues.Where(i => i.Type == "Epic" && i.Status != "Archived" && i.Id != issue.Id).ToList();
                foreach (var epic in epics) PotentialParents.Add(epic);
            }
            else if (issue.Type != "Epic")
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

            var result = CustomDialogWindow.Show(
                "Delete Story", 
                $"Are you sure you want to permanently delete the story '{story.Description}' AND all its linked backlog issues?",
                CustomDialogWindow.DialogType.Warning,
                "Delete Everything", "Keep Story");

            if (result != CustomDialogWindow.CustomDialogResult.Ok) return;

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

            if (oldParentId.HasValue) await CheckAndCloseParentEpic(oldParentId.Value);

            RefreshCategorizedIssues();
        }

        [RelayCommand]
        private void ToggleCardsCollapse()
        {
            AreCardsCollapsed = !AreCardsCollapsed;
        }


        private async Task ShowLogsAsync(Issue? issue)
        {
            if (issue == null) return;
            var logs = await _dataService.GetLogsAsync(issue.Id);
            
            ActivityLogWindow.Show(Application.Current.MainWindow, logs);
        }

        [RelayCommand]
        public void CopyIssueToClipboard(Issue? issue)
        {
            if (issue == null) return;
            try
            {
                var text = $"{issue.Description} // {issue.LongDescription}";
                System.Windows.Clipboard.SetText(text);
                // Optionally show a small info that it was copied
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }


        public async Task LogIssueChange(Guid issueId, string fieldChanged, string? oldValue, string? newValue)
        {
            try
            {
                var log = new IssueLog
                {
                    IssueId = issueId,
                    UserName = CurrentUser?.FullName ?? "System",
                    FieldChanged = fieldChanged ?? "Status",
                    OldValue = oldValue ?? "Unknown",
                    NewValue = newValue ?? "Unknown",
                    Action = $"{fieldChanged} changed",
                    Details = $"{oldValue} → {newValue}"
                };
                
                await _dataService.AddLogAsync(log);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                CustomDialogWindow.Show("Activity Log Error", 
                    $"The action was performed, but the history entry couldn't be saved: {ex.Message}", 
                    CustomDialogWindow.DialogType.Error);
            }
        }

        private async Task ArchiveIssueAsync(Issue? issue)
        {
            if (issue == null) return;

            // When archiving epics/stories, children remain untouched (no unlinking, no archiving)
            
            var oldStatus = issue.Status;
            issue.Status = "Archived";
            await LogIssueChange(issue.Id, "Status", oldStatus, "Archived");
            await _dataService.UpdateIssueAsync(issue);
            RefreshCategorizedIssues();
        }

        [RelayCommand]
        private async Task DeleteMainIssueAsync(Issue? issue)
        {
            if (issue == null) return;

            // Epic/Story logic - automatically unlink all children
            if ((issue.Type == "Epic" || issue.Type == "Story") && issue.Children.Any())
            {
                foreach (var child in issue.Children.ToList())
                {
                    child.ParentIssueId = null;
                    await _dataService.UpdateIssueAsync(child);
                }
            }
            else
            {
                var result = CustomDialogWindow.Show(
                    "Confirm Delete", 
                    $"Are you sure you want to delete '{issue.Description}'?", 
                    CustomDialogWindow.DialogType.Warning,
                    "Delete", "Cancel");
                if (result != CustomDialogWindow.CustomDialogResult.Ok) return;
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
            var oldStatus = issue.Status;
            issue.Status = "Open"; // Restore to open state
            await LogIssueChange(issue.Id, "Status", oldStatus, "Open");
            await _dataService.UpdateIssueAsync(issue);
            RefreshCategorizedIssues();
        }

        private void ShowHierarchy(Issue? issue)
        {
            if (issue == null) return;
            var window = new HierarchyWindow(issue, _allProjectIssues);
            window.Owner = Application.Current.MainWindow;
            window.Show();
        }
    }
}
