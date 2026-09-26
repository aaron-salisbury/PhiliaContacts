namespace PhiliaContacts.Business.Modules.Projects;

public sealed record ProjectContext(ProjectContextId Id, string Name, ProjectContextState State, bool IsArchived);
