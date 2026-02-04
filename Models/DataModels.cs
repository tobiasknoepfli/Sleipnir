using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;

namespace Sleipnir.App.Models
{
    [Table("projects")]
    public class Project : BaseModel, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private string _name = string.Empty;
        private string _description = string.Empty;
        private string? _logoUrl;

        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("name")]
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        [Column("description")]
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("logo_url")]
        public string? LogoUrl { get => _logoUrl; set { _logoUrl = value; OnPropertyChanged(); } }

        [Column("logo_data")]
        public string? LogoData { get => _logoData; set { _logoData = value; OnPropertyChanged(); } }
        private string? _logoData;

        public override string ToString() => Name;
    }

    [Table("sprints")]
    public class Sprint : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public bool IsCurrent => DateTime.Today >= StartDate.Date && DateTime.Today <= EndDate.Date;
        [JsonIgnore]
        public bool CanBeCompleted => IsActive && DateTime.Today > EndDate.Date;
        [JsonIgnore]
        public bool IsPast => !IsActive;
    }

    [Table("collaborators")]
    public class Collaborator : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        public override string ToString() => Name;
    }

    [Table("issues")]
    public class Issue : BaseModel, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private string _status = "Open";
        private string _priority = "Neutral";
        private string _responsibleUsers = string.Empty;
        private string _description = string.Empty;
        private string _longDescription = string.Empty;
        private Guid? _sprintId;

        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [Column("sprint_id")]
        public Guid? SprintId { get => _sprintId; set { _sprintId = value; OnPropertyChanged(); } }

        [Column("program_component")]
        public string ProgramComponent { get; set; } = string.Empty;

        [Column("sub_components")]
        public string SubComponents { get; set; } = string.Empty; // Semicolon separated

        [Column("description")]
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTitle)); } }

        [Column("long_description")]
        public string LongDescription { get => _longDescription; set { _longDescription = value; OnPropertyChanged(); } }

        [Column("type")]
        public string Type { get; set; } = "Bug"; // Bug, Feature, Epic, Story

        [Column("category")]
        public string Category { get; set; } = "Backlog"; // Backlog, Pipeline, Hub

        [Column("status")]
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        [Column("priority")]
        public string Priority { get => _priority; set { _priority = value; OnPropertyChanged(); } }

        [Column("responsible_users")]
        public string ResponsibleUsers { get => _responsibleUsers; set { _responsibleUsers = value; OnPropertyChanged(); } }

        [Column("parent_issue_id")]
        public Guid? ParentIssueId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("persona")]
        public string Persona { get => _persona; set { _persona = value; OnPropertyChanged(); } }
        private string _persona = string.Empty;

        [Column("purpose")]
        public string Purpose { get => _purpose; set { _purpose = value; OnPropertyChanged(); } }
        private string _purpose = string.Empty;

        [Column("goal")]
        public string Goal { get => _goal; set { _goal = value; OnPropertyChanged(); } }
        private string _goal = string.Empty;

        [Column("definition_of_done")]
        public string DefinitionOfDone { get => _definitionOfDone; set { _definitionOfDone = value; OnPropertyChanged(); } }
        private string _definitionOfDone = string.Empty;

        // UI support for hierarchy
        private System.Collections.ObjectModel.ObservableCollection<Issue> _children = new();
        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<Issue> Children 
        { 
            get => _children; 
            set { _children = value; OnPropertyChanged(); } 
        }

        private string? _parentTitle;
        [JsonIgnore]
        public string? ParentTitle { get => _parentTitle; set { _parentTitle = value; OnPropertyChanged(); } }

        private string? _sprintTag;
        [JsonIgnore]
        public string? SprintTag { get => _sprintTag; set { _sprintTag = value; OnPropertyChanged(); } }
        
        private int? _parentFriendlyId;
        [JsonIgnore]
        public int? ParentFriendlyId { get => _parentFriendlyId; set { _parentFriendlyId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ParentEpicNumber)); OnPropertyChanged(nameof(ParentStoryNumber)); } }
        
        [JsonIgnore]
        public string? ParentEpicNumber => ParentFriendlyId.HasValue ? $"Epic #{ParentFriendlyId.Value:D5}" : null;
        
        private int? _parentStoryFriendlyId;
        [JsonIgnore]
        public int? ParentStoryFriendlyId { get => _parentStoryFriendlyId; set { _parentStoryFriendlyId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ParentStoryNumber)); } }
        [JsonIgnore]
        public string? ParentStoryNumber => ParentStoryFriendlyId.HasValue ? $"Story #{ParentStoryFriendlyId.Value:D5}" : null;

        // Formatted title: Program / Sub1 / Sub2 : Description
        [JsonIgnore]
        public string FormattedTitle
        {
            get
            {
                var subs = SubComponents.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var subPart = subs.Length > 0 ? " / " + string.Join(" / ", subs) : "";
                var componentPart = string.IsNullOrWhiteSpace(ProgramComponent) ? "" : ProgramComponent;

                if (string.IsNullOrWhiteSpace(componentPart) && string.IsNullOrWhiteSpace(subPart))
                    return Description;

                return $"{componentPart}{subPart} : {Description}";
            }
        }

        [JsonIgnore]
        public string LocationTag => Status == "Archived" ? $"{Category} Archive" : Category;
        [JsonIgnore]
        public string FullLocationTag => ParentFriendlyId.HasValue ? $"{LocationTag} | {SprintTag}" : LocationTag;

        [JsonIgnore]
        public string EpicNumber => $"Epic #{FriendlyId:D5}";
        [JsonIgnore]
        public string StoryNumber => $"Story #{FriendlyId:D5}";
        [JsonIgnore]
        public string IssueNumber => $"Issue #{FriendlyId:D5}";

        [JsonIgnore]
        public string TypeTag => Type switch
        {
            "Bug" => "🐞 Bug",
            "Feature" => "✨ Feature",
            "Patch" => "🛠️ Patch",
            "Overhaul" => "🏗️ Overhaul",
            "Alteration" => "⚙️ Alteration",
            "Story" => "📘 Story",
            "Epic" => "💡 Epic",
            _ => $"📋 {Type}"
        };

        [JsonIgnore]
        public string PriorityTag => Priority switch
        {
            "Critical" => "‼ CRITICAL",
            "Very High" => "❗ VERY HIGH",
            "High" => "↑ HIGH",
            "Neutral" => "→ NEUTRAL",
            "Low" => "↓ LOW",
            "Very Low" => "- VERY LOW",
            "Nice to Have" => "-- NICE TO HAVE",
            _ => $"📋 {Priority.ToUpper()}"
        };

        [JsonIgnore]
        public int StoryCount => Children.Count;
        [JsonIgnore]
        public int IssueCount => Children.Count;

        [Column("friendly_id")]
        public int FriendlyId { get; set; }

        [JsonIgnore]
        public string AgeString
        {
            get
            {
                var age = DateTime.UtcNow - CreatedAt;
                if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d up";
                if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h up";
                return $"{(int)age.TotalMinutes}m up";
            }
        }

        [JsonIgnore]
        public bool CanBeArchived
        {
            get
            {
                if (Type != "Epic" && Type != "Story") return false;
                if (Children.Count == 0) return true;
                return Children.All(c => c.Status == "Finished");
            }
        }

        public void RefreshArchivability()
        {
            OnPropertyChanged(nameof(CanBeArchived));
            OnPropertyChanged(nameof(StoryCount));
        }
    }

    [Table("issue_logs")]
    public class IssueLog : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("issue_id")]
        public Guid IssueId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; } = "System";

        [Column("action")]
        public string Action { get; set; } = string.Empty; // Created, Edited, Status Changed

        [Column("details")]
        public string Details { get; set; } = string.Empty;

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    [Table("users")]
    public class AppUser : BaseModel, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _pendingEmail = string.Empty;
        private string _verificationCode = string.Empty;
        private bool _isEmailVerified;
        private bool _isRoot;
        private bool _isPasswordRevealed;
        private string _emoji = "Account";
        private bool _isSuperuser;
        private bool _canAutoLogin;

        [JsonIgnore]
        public bool IsPasswordRevealed { get => _isPasswordRevealed; set { _isPasswordRevealed = value; OnPropertyChanged(); } }
        private string _allowedProjectIds = string.Empty; // Semicolon separated IDs

        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("username")]
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsOriginalAdmin)); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("password")]
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("first_name")]
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("last_name")]
        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("email")]
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        [Column("pending_email")]
        public string PendingEmail { get => _pendingEmail; set { _pendingEmail = value; OnPropertyChanged(); } }

        [Column("verification_code")]
        public string VerificationCode { get => _verificationCode; set { _verificationCode = value; OnPropertyChanged(); } }

        [Column("is_email_verified")]
        public bool IsEmailVerified { get => _isEmailVerified; set { _isEmailVerified = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("is_root")]
        public bool IsRoot { get => _isRoot; set { _isRoot = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsOriginalAdmin)); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("emoji")]
        public string Emoji { get => _emoji; set { _emoji = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEditProtectedFields)); } }

        [Column("is_superuser")]
        public bool IsSuperuser { get => _isSuperuser; set { _isSuperuser = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasHubAccess)); OnPropertyChanged(nameof(HasPipelineAccess)); OnPropertyChanged(nameof(HasBacklogAccess)); OnPropertyChanged(nameof(HasSettingsAccess)); } }

        [Column("can_access_hub")]
        public bool CanAccessHub { get => _canAccessHub; set { _canAccessHub = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasHubAccess)); } }
        private bool _canAccessHub = true;

        [Column("can_access_pipeline")]
        public bool CanAccessPipeline { get => _canAccessPipeline; set { _canAccessPipeline = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPipelineAccess)); } }
        private bool _canAccessPipeline = true;

        [Column("can_access_backlog")]
        public bool CanAccessBacklog { get => _canAccessBacklog; set { _canAccessBacklog = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBacklogAccess)); } }
        private bool _canAccessBacklog = true;

        [Column("can_access_settings")]
        public bool CanAccessSettings { get => _canAccessSettings; set { _canAccessSettings = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSettingsAccess)); } }
        private bool _canAccessSettings = true;

        [JsonIgnore]
        public bool HasHubAccess => IsSuperuser || CanAccessHub;
        [JsonIgnore]
        public bool HasPipelineAccess => IsSuperuser || CanAccessPipeline;
        [JsonIgnore]
        public bool HasBacklogAccess => IsSuperuser || CanAccessBacklog;
        [JsonIgnore]
        public bool HasSettingsAccess => IsSuperuser || CanAccessSettings;

        [Column("can_auto_login")]
        public bool CanAutoLogin { get => _canAutoLogin; set { _canAutoLogin = value; OnPropertyChanged(); } }

        [Column("allowed_project_ids")]
        public string AllowedProjectIds { get => _allowedProjectIds; set { _allowedProjectIds = value; OnPropertyChanged(); } }

        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";

        [JsonIgnore]
        public bool IsOriginalAdmin => IsRoot;

        [JsonIgnore]
        public bool CanEditProtectedFields => !IsRoot || IsEmailVerified;

        public bool HasAccessToProject(Guid projectId)
        {
            if (IsSuperuser) return true;
            if (string.IsNullOrEmpty(AllowedProjectIds)) return false;
            var ids = AllowedProjectIds.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return ids.Contains(projectId.ToString());
        }
    }
}
