# Philia Contacts Development Conventions

## Purpose

This document defines the current development conventions for Philia Contacts. 
It is intended for both human contributors and AI coding assistants.

These conventions are defaults, not dogma. They should promote clarity,
consistency, maintainability, separation of concerns, and simplicity.
Reconsider a convention when a concrete design problem demonstrates that
another approach better serves the project.

------------------------------------------------------------------------

## 1. General Design Philosophy

### Prefer Simple Designs

Follow KISS. Do not introduce abstractions, layers, patterns, or object
types solely to satisfy an architectural diagram. Every abstraction
should solve a concrete problem or protect an intentional boundary.

Avoid speculative architecture for capabilities Philia Contacts does not yet
require.

### Enforce Separation of Concerns

Code should live with the responsibility it implements.

-   Presentation does not implement business logic.
-   Business does not implement persistence concerns.
-   Data access does not implement business validation.
-   External integration details do not leak into core business
    concepts.
-   Persistence structure does not define business structure.
-   A project reference grants technical access, not architectural
    permission.

Only protocols, services, DTOs, and other types intentionally exposed
across a boundary should be consumed by another tier or capability.

### Preserve Locality of Behaviour

Within Business, organize code so behavior related to a capability is
easy to discover and reason about locally. Prefer cohesive
feature/domain organization over scattering related behavior throughout
large technical folders.

Reference: [Locality of
Behaviour](https://htmx.org/essays/locality-of-behaviour/)

### Prefer Modularity

The solution should be modular both horizontally through architectural
boundaries and vertically through cohesive business capabilities.

Modules should expose deliberate contracts rather than internal
implementation details.

------------------------------------------------------------------------

## 2. .NET Platform

Projects should target the current Long-Term Support version of .NET,
following the even-numbered .NET release cadence.

Prefer the highest supported even-numbered LTS release unless a concrete
compatibility constraint requires otherwise. Do not upgrade merely to
consume a non-LTS release without a compelling reason.

------------------------------------------------------------------------

## 3. Solution Architecture

Follows a modified three-tier architecture centered on **Business**, with 
**Data** and **Integrations** treated as implementation boundaries that 
satisfy application-facing contracts defined by Business.

The familiar Data, Business, and Presentation responsibilities remain, but 
dependencies across persistence and external-provider boundaries should 
follow the **Dependency Inversion Principle**. Business defines the 
capabilities the application requires; Data and Integrations reference 
Business and implement those contracts.

A dedicated executable application project acts as the **composition root**. 
For the desktop application this is `*.DesktopApp`. It owns startup, 
dependency-injection composition, application lifetime, and initialization 
that necessarily coordinates concrete implementations. Presentation projects 
remain presentation libraries rather than acquiring composition-root 
responsibilities merely because they provide a user interface.

```text
                         Business
                      ▲            ▲
                      │            │
                    Data      Integrations
                      ▲            ▲
                       \          /
                        \        /
                     DesktopApp
                         │
                         ▼
              Presentation.Desktop
```

Conceptually:

```text
*.Business
    Defines business concepts, application behavior, validation,
    application-facing DTOs, and contracts for capabilities required
    from persistence and external providers.

*.Data
    Implements persistence capabilities required by Business and
    persists application-owned state.

*.Integrations
    Implements external capabilities required by Business by adapting
    third-party applications, standards, protocols, and services.

*.Presentation.<Desktop|Web|Mobile>
    Provides presentation-specific views, state, binding models, and
    interaction behavior over intentionally exposed Business
    capabilities.

*.<Desktop|Web|Mobile>App
    Is the executable and composition root for the application.
    It references the projects required to assemble the application,
    registers concrete implementations, performs application startup and
    initialization, and owns the application lifetime.
```

Business remains the architectural center. The composition root has broad 
technical visibility because assembling the application is its 
responsibility; that visibility does not change the architectural 
ownership of Business, Data, Integrations, or Presentation behavior.

### Business

`*.Business` owns application and domain behavior, including business
concepts, processes/services, validation, application-facing DTOs,
expected application failure states, and contracts describing the
persistence and external capabilities the application requires.

Business should not depend on persistence implementations,
provider-specific integrations, or presentation technology.

For example, if Business requires Goals to be persisted, Business may
define an `IGoalRepository` contract. If it requires access to external
knowledge, it may define an `IKnowledgeProvider` contract. Data and
Integrations implement those contracts rather than Business referencing
their concrete implementations.

### Data

`*.Data` owns persistence implementation concerns for application-owned
information, including database access, persistent/data entities, schema
and migrations, persistence-specific mapping, repository/store
implementations, local caches and indexes, and synchronization
persistence state where appropriate.

Data references Business as necessary to implement Business-defined
persistence contracts.

Data access must not implement business validation or application
decisions. Persistence implementation details should not leak into
Business or Presentation.

### Integrations

External systems are capabilities, not merely data stores. Provider
implementations should therefore generally live in an Integrations
boundary rather than being forced into `*.Data`.

The solution starts with a single integration project organized by
provider-specific namespaces or folders:

```text
PhiliaContacts.Integrations/
├── EWSoftware/
├── vCardLib/
└── ...
```

Dedicated integration projects may be introduced when an integration's
size, dependencies, deployment needs, or isolation requirements justify
them.

Integration implementations:

* Understand the external provider's API, protocol, authentication, and
  data formats.
* Implement application-facing capability contracts defined by Business.
* Translate provider-specific representations into application-defined
  contracts and DTOs.
* Keep provider-specific types from leaking into Business.
* Do not become canonical owners of external source data unless the
  application explicitly owns that information.

Integrations reference Business as necessary to implement its capability
contracts.

### Presentation

There may be multiple presentations over the same business
functionality.

```text
*.Presentation.Desktop
*.Presentation.Web
*.Presentation.Mobile
*.Presentation.Cli
```

Presentation projects do not implement business or persistence logic.
Presentation-specific state and binding models belong in the appropriate
Presentation project.

Presentation projects are not composition roots by default. Executable
application projects such as `*.DesktopApp` compose Presentation with
Business, Data, and Integrations.

Presentation implementations share application capabilities, not
presentation architecture by default. Patterns adopted for one
presentation target should not be imposed on another unless they solve a
shared presentation concern.

------------------------------------------------------------------------

## 4. Project Naming

Use `PhiliaContacts` as the root project name and a suffix describing the
architectural tier, capability, or presentation target.

Examples:

``` text
PhiliaContacts.Data
PhiliaContacts.Business
PhiliaContacts.Integrations
PhiliaContacts.Presentation.Desktop
```

For presentation projects, include the target platform/presentation in
the suffix.

------------------------------------------------------------------------

## 5. Presentation Patterns

### Desktop and Mobile GUI

Desktop and mobile GUI applications should generally follow MVVM where
appropriate.

``` text
View
    Visual structure and presentation behavior.

ViewModel
    Presentation state, commands, and coordination.

Model
    Presentation-facing data suitable for binding.
```

Business rules remain in Business.

### ViewModel and View Activation

ViewModels should not depend on Views or UI-framework types. Views should
not locate or construct their own ViewModels. Presentation composition
establishes the root ViewModel, while child ViewModels should be supplied
through presentation-owned content or activation mechanisms.

Content identity, content activation, and content placement are separate
presentation concerns. Dependency injection may construct presentation
objects, but DI resolution should not define navigation semantics or
determine whether content appears in the current shell region, a tab, a
docked panel, a separate window, or another presentation form.

For Avalonia desktop presentation, prefer explicit ViewModel-to-View
mappings using Avalonia template mechanisms over reflection or naming
conventions that derive View types from ViewModel names at runtime.

Do not introduce generalized navigation or workspace abstractions until
concrete interaction requirements justify them.

### Web

Use a pattern appropriate to the chosen web technology while maintaining
the same separation-of-concerns rules. Do not force desktop MVVM
concepts into a web framework where they do not naturally fit.

### CLI

MVVM is not required for CLI applications. Prefer the simplest pattern
suited to command-line interaction, for example:

``` text
Command
    ↓
Business service/process
    ↓
Result
    ↓
Console rendering
```

The absence of MVVM does not relax architectural separation.

------------------------------------------------------------------------

## 6. Pattern Naming Conventions

Use architectural suffixes only when a class actually performs that
role.

-   View classes end in `View`.
-   Presenter classes end in `Presenter`.
-   ViewModel classes end in `ViewModel`.
-   Controller classes end in `Controller`.
-   Classes that do not perform these roles must not use these suffixes.

------------------------------------------------------------------------

## 7. Business Object Vocabulary

### Persistent Entities / Data Entities

A **persistent entity** or **data entity** is a uniquely identifiable
persisted data representation. Use these qualified terms in
architectural discussion when ambiguity is possible.

Do not call every business object an entity merely because it has
identity.

Persistence structure does not define business structure. Multiple data
entities may support one business concept, and a data entity may exist
only for caching, synchronization, indexing, auditing, integration
state, or other infrastructure bookkeeping.

Persistent entities should generally remain simple data-oriented
objects.

### DTOs

DTOs are lightweight objects intended to transfer data across an
intentional boundary.

DTOs should:

-   Be serializable by design.
-   Be POCOs.
-   Generally be C# records.
-   Contain data rather than complex behavior.
-   Represent the contract that the application intends to expose rather 
than blindly mirror persistence or provider representations.

Example:

``` csharp
public sealed record CreateGoalDto(
    string Name,
    string? Description,
    Guid LifeDomainId);
```

External integrations may have provider-specific DTOs in addition to
PhiliaContacts-facing DTOs. Do not expose an external provider DTO 
directly merely to avoid writing a mapping.

### Domains / Business Concepts

A **domain** or **business concept** represents a real-world or
app-specific concept required by business logic. It is not defined by
whether or how it persists.

A business representation may have one persistent representation,
multiple persistent representations, or none.

Business objects should generally remain straightforward data-oriented
representations. Prefer focused business services/processes for
non-trivial behavior rather than automatically placing complex behavior
on the objects themselves.

Useful DDD ideas may be adopted when they solve a concrete problem, but 
this solution does not require DDD terminology or rich domain objects by 
default.

### Models

A **Model** is a representation intended for presentation consumption.
Models may represent a view of business concepts, DTOs, or composed
information from multiple sources.

In MVVM and MVC, Models are suitable for presentation consumption and
data binding. Presentation Models belong to Presentation when their
shape exists specifically for UI needs.

### Do Not Manufacture One-to-One Representations

Do not automatically create all of the following for every concept:

``` text
GoalEntity
Goal
GoalDto
GoalModel
```

Create a distinct representation only when a real boundary or
transformation requires it. KISS takes precedence over architectural
ceremony.

------------------------------------------------------------------------

## 8. Business Behavior

### Prefer Focused Processes and Services

The solution generally favors restrained behavior on data-oriented objects. 
Non-trivial operations should normally live in focused services/processes 
near the capability they implement.

For example:

``` text
Goals/
├── Goal.cs
├── Objective.cs
├── CreateGoalProcess.cs
├── AddObjectiveProcess.cs
└── CompleteGoalProcess.cs
```

Exact organization should follow the capability and Locality of
Behaviour rather than a mandatory folder template.

### Validation

Application DTOs should be validated at the appropriate
business/application boundary.

Do not put business validation in Data, depend on UI validation as sole
enforcement, or treat database constraints as a replacement for business
validation. Database constraints may still enforce data integrity
defensively.

------------------------------------------------------------------------

## 9. Integration Contracts

Use the following terminology consistently:

-   **Provider** — an implementation the applicaiton knows how to connect to.
-   **Integration** — a user-configured instance of a Provider.
-   **Capability** — an application operation or family of operations
    that a Provider can satisfy.

> **Providers satisfy capabilities. Integrations configure Providers.
> Presentations, automations, and agents consume capabilities.**

Business defines application-facing capability contracts; Integrations
implements them. Introduce typed capability contracts from concrete
requirements rather than creating speculative interfaces for every
possible external-system category.

Provider-specific types remain inside the corresponding integration
boundary. App-facing contracts use app-defined DTOs and business
concepts.

A Provider is registered under a stable `ProviderKey`. Persisted
Integrations reference that key rather than CLR type names. Runtime
provider selection uses the provider registry, which contains
already-constructed Provider implementations and must not become a
wrapper around `IServiceProvider`.

Provider-specific safe configuration crosses Business as opaque
`ProviderConfiguration`. Business does not inspect its fields. JSON is
the initial durable interchange representation; concrete Providers
should deserialize to typed provider-owned configuration for use and
validation.

Secrets must not be stored in provider configuration. Persist only an
opaque `CredentialReference` and resolve secret material through the
appropriate infrastructure boundary when a real Provider requires it.

Read and write authority should be distinguishable when concrete
capabilities require it, particularly where agents or automation may
exercise those capabilities.

------------------------------------------------------------------------

## 10. Expected Failures and Exceptions

Expected operational states should generally be represented explicitly
rather than communicated through exceptions, especially for
integrations.

Examples:

``` text
Success
NotFound
Unavailable
Unauthorized
Forbidden
InvalidResponse
```

A result type may be appropriate. Use exceptions for exceptional or
unexpected failures rather than normal control flow.

Reference: [Exception-Handling Antipatterns and API
Design](https://www.infoq.com/articles/Exceptions-API-Design/)

------------------------------------------------------------------------

## 11. C# Coding Conventions

### Namespaces

Prefer file-scoped namespaces.

``` csharp
namespace PhiliaContacts.Business.Goals;
```

### Local Variable Declarations

Prefer an explicit type with target-typed construction:

``` csharp
Goal goal = new();
```

over:

``` csharp
var goal = new Goal();
```

Use judgment where explicit typing would make code substantially less
readable.

### Constants

Name constants using uppercase snake case.

``` csharp
private const string API_ENDPOINT = "https://rest.example.com/v1";
```

### Private Fields

Use camel case with a leading underscore.

``` csharp
private int _counter;
```

### Public and Internal Members

Use PascalCase.

``` csharp
public required DateTime TimeStamp { get; set; }
```

### General C# Style

Unless soluciotn documentation defines a more specific convention, generally follow
[Microsoft C# Coding
Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).

### HTTP Clients

Prefer `IHttpClientFactory` for application and integration code that performs HTTP 
requests. Register HTTP client configuration in the executable composition root and 
inject `IHttpClientFactory` into long-lived consumers rather than registering or 
capturing an unconfigured singleton `HttpClient`.

Create clients when an operation needs them:

``` csharp
HttpClient httpClient = _httpClientFactory.CreateClient();
```

Creating an `HttpClient` through the factory is inexpensive because the factory 
manages and pools the underlying handlers. Named or typed clients may be used 
when a concrete integration needs stable provider-specific HTTP configuration.

A long-lived `HttpClient` is not inherently incorrect. It can be appropriate 
when deliberately configured with connection-lifetime management such as 
`SocketsHttpHandler.PooledConnectionLifetime`. Prefer that approach only when 
its lifetime and handler policy are intentional and provide a concrete advantage 
over the application's existing `IHttpClientFactory` infrastructure.

Do not create a separate HTTP lifetime policy inside an integration merely 
because it needs HTTP access. The composition root owns application-level HTTP 
infrastructure; integration code owns provider/protocol behavior.

### Library Design

Libraries should generally follow the [.NET Framework Design
Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
where applicable.

------------------------------------------------------------------------

## 12. Dependency and Boundary Rules

The conceptual dependency relationship is:

``` text
                         DesktopApp
                    /        |         \
                   ▼         ▼          ▼
        Presentation.Desktop Data   Integrations
                   │         │          │
                   ▼         ▼          ▼
                         Business
```

The executable/composition-root project sits at the outer edge of the
architecture. It may reference concrete implementation projects because
its responsibility is to assemble and run the application. Those
references are composition authority, not permission to relocate
business, persistence, integration, or presentation behavior into the
application project.

Exact project-reference mechanics may vary as contracts are factored,
but preserve these rules:

-   Business does not depend on Presentation or an executable application
    project.
-   Business does not depend on provider-specific integrations.
-   Business does not depend on persistence implementation details.
-   Data implements persistence responsibilities required by Business.
-   Integrations implement external capabilities required by Business.
-   Presentation consumes intentionally exposed Business capabilities.
-   Presentation does not reference Data or Integrations merely to obtain
    concrete implementations.
-   The composition root may reference Business, Data, Integrations, and
    its Presentation implementation to register and coordinate them at
    startup.
-   The composition root should contain only application-host concerns,
    not business rules or implementation behavior owned by another
    boundary.
-   Provider-specific DTOs remain inside their integration boundary.
-   Persistent/data entities remain inside the persistence boundary
    unless deliberately exposed for a specific reason.

Technical visibility does not grant architectural permission to consume
an implementation type outside the responsibility that justified the
reference.

------------------------------------------------------------------------

## 13. AI Coding Assistant Guidance

When generating or modifying code:

1.  Preserve Data, Business, Integration, and Presentation boundaries.
2.  Do not introduce provider-specific types into Business.
3.  Do not introduce persistent/data entities into Presentation.
4.  Do not implement business validation in Data.
5.  Do not implement business logic in Presentation.
6.  Prefer focused capability-local services/processes over large
    generic managers.
7.  Prefer simple data-oriented business objects unless object behavior
    provides a clear advantage.
8.  Use records for DTOs unless there is a concrete reason not to.
9.  Do not create DTOs, Models, data entities, interfaces, repositories,
    or mappings merely for architectural symmetry.
10. Prefer explicit contracts at project boundaries.
11. Preserve external systems as canonical owners of information the app
    does not intrinsically own.
12. Prefer open protocols and provider abstractions over vendor
    coupling.
13. Follow this document's C# naming and formatting conventions.
14. Target the current even-numbered .NET LTS release unless the
    solution explicitly specifies otherwise.
15. Ask whether an abstraction solves a current problem before adding
    it.
16. If a convention appears inappropriate for a concrete problem,
    explain the tradeoff rather than silently working around it.
17. Give appropriate credit when implementation or design is substantially
    derived from an identifiable external source rather than merely informed
    by general knowledge. When practical, cite the source near the adopted
    implementation, typically in XML documentation such as a `<remarks>`
    element. This applies even to permissively licensed, public-domain, CC0,
    educational, textbook, or article sources. Use judgment: ordinary
    techniques and broadly learned ideas do not require mechanical citations.

------------------------------------------------------------------------

## 14. Decision Heuristics

When deciding where new code belongs:

**Is this about storing app-owned information?**\
It probably belongs in `PhiliaContacts.Data`.

**Is this a Philia Contacts rule, concept, process, validation, or capability
contract?**\
It probably belongs in `PhiliaContacts.Business`.

**Does it understand an external application's API, protocol, or
representation?**\
It probably belongs in `PhiliaContacts.Integrations.<Provider>`.

**Does its shape or behavior exist specifically because of a user
interface?**\
It probably belongs in `PhiliaContacts.Presentation.<Platform>`.

**Does a new class merely duplicate another representation without
protecting a boundary?**\
Do not create it yet.

**Does a database table exist only for infrastructure reasons?**\
Do not invent a matching business concept.

**Does a business concept persist?**\
That does not make the persistence representation the business model.

------------------------------------------------------------------------

## 15. Guiding Architectural Rule

> **Architecture should make intended behavior easy to find, intended
> dependencies obvious, and unintended coupling difficult.**

This solution favors explicit boundaries and consistent conventions, but
simplicity and clear responsibility take precedence over pattern
compliance for its own sake.
