namespace PhiliaContacts.Business.Modules.Goals;

public sealed record Objective(ObjectiveId Id, GoalId GoalId, string Name, ObjectiveState State, bool IsArchived);
