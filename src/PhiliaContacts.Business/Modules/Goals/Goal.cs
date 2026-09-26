using PhiliaContacts.Business.Modules.LifeDomains;

namespace PhiliaContacts.Business.Modules.Goals;

public sealed record Goal(GoalId Id, LifeDomainId LifeDomainId, string Name, GoalState State, bool IsArchived);
