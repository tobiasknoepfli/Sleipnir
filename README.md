# Sleipnir 🚀

A modern, multi-user Jira-style desktop application built with C# and WPF.

## Features
- **Project Management**: Create and organize multiple projects.
- **3-Lane Dashboard**: 
    - **Backlog**: Dedicated to bug tracking.
    - **Pipeline**: Visualized feature development.
    - **The Hub**: Brainstorming space for epics and stories.
- **Cloud Powered**: Built to work with Supabase (PostgreSQL) for free, multi-user web-hosted data.
- **Premium UI**: Sleek dark theme with MahApps.Metro and custom glassmorphism styles.

## How to use Supabase (Free Web DB)
1. Go to [supabase.com](https://supabase.com/) and create a free account.
2. Create a new project.
3. In the SQL Editor, run the following to create your tables:

```sql
create table projects (
  id uuid primary key default uuid_generate_v4(),
  name text not null,
  description text,
  created_at timestamp with time zone default now()
);

create table issues (
  id uuid primary key default uuid_generate_v4(),
  project_id uuid references projects(id) on delete cascade,
  title text not null,
  description text,
  type text, -- 'Bug', 'Feature', 'Epic'
  category text, -- 'Backlog', 'Pipeline', 'Hub'
  status text,
  priority int default 1,
  created_at timestamp with time zone default now()
);
```

4. Enable **Realtime** on these tables if you want instant updates across users.
5. In `MainWindow.xaml.cs`, replace `MockDataService` with `SupabaseDataService`.

## Running the App
- Open the project in Visual Studio or VS Code.
- Run `dotnet run`.
