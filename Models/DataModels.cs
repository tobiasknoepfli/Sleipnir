using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

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

        public bool IsCurrent => DateTime.Today >= StartDate.Date && DateTime.Today <= EndDate.Date;
        public bool CanBeCompleted => IsActive && DateTime.Today > EndDate.Date;
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
        private string _priority = "Medium";
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
        public string Type { get; set; } = "Bug"; // Bug, Feature, Idea, Story

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

        // UI support for hierarchy
        private System.Collections.ObjectModel.ObservableCollection<Issue> _children = new();
        public System.Collections.ObjectModel.ObservableCollection<Issue> Children 
        { 
            get => _children; 
            set { _children = value; OnPropertyChanged(); } 
        }

        private string? _parentTitle;
        public string? ParentTitle { get => _parentTitle; set { _parentTitle = value; OnPropertyChanged(); } }

        private string? _sprintTag;
        public string? SprintTag { get => _sprintTag; set { _sprintTag = value; OnPropertyChanged(); } }
        
        private int? _parentFriendlyId;
        public int? ParentFriendlyId { get => _parentFriendlyId; set { _parentFriendlyId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ParentIdeaNumber)); OnPropertyChanged(nameof(ParentStoryNumber)); } }
        
        public string? ParentIdeaNumber => ParentFriendlyId.HasValue ? $"Idea #{ParentFriendlyId.Value:D5}" : null;
        
        private int? _parentStoryFriendlyId;
        public int? ParentStoryFriendlyId { get => _parentStoryFriendlyId; set { _parentStoryFriendlyId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ParentStoryNumber)); } }
        public string? ParentStoryNumber => ParentStoryFriendlyId.HasValue ? $"Story #{ParentStoryFriendlyId.Value:D5}" : null;

        // Formatted title: Program / Sub1 / Sub2 : Description
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

        public string LocationTag => Status == "Archived" ? $"{Category} Archive" : Category;
        public string FullLocationTag => ParentFriendlyId.HasValue ? $"{LocationTag} | {SprintTag}" : LocationTag;

        public string IdeaNumber => $"Idea #{FriendlyId:D5}";
        public string StoryNumber => $"Story #{FriendlyId:D5}";
        public string IssueNumber => $"Issue #{FriendlyId:D5}";

        public string TypeTag => Type switch
        {
            "Bug" => "🐞 Bug",
            "Feature" => "✨ Feature",
            "Patch" => "🛠️ Patch",
            "Overhaul" => "🏗️ Overhaul",
            "Alteration" => "⚙️ Alteration",
            "Story" => "📘 Story",
            "Idea" => "💡 Idea",
            _ => $"📋 {Type}"
        };

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

        public int StoryCount => Children.Count;
        public int IssueCount => Children.Count;

        [Column("friendly_id")]
        public int FriendlyId { get; set; }

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

        public bool CanBeArchived
        {
            get
            {
                if (Type != "Idea" && Type != "Story") return false;
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
}
