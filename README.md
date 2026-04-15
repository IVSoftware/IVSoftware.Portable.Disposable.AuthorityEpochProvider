# AuthorityEpochProvider

Coordinates a shared, reference-counted authority across overlapping scopes.

## Problem

Multiple participants need to operate within a shared context, but:

- The *first* participant defines the controlling authority
- Additional participants may join without overriding that authority
- Cleanup must occur **once**, when the last participant exits
- Reentry and cancellation must not corrupt state or leak events

## Solution

`AuthorityEpochProvider` wraps a reference-counted lifetime (`DisposableHost`) and projects:

- **Stable authority** (first requester wins)
- **Scoped participation** via `IDisposable`
- **Deterministic teardown** on the 1 -> 0 transition
- **Safe cancellation** that detaches the current epoch without poisoning the instance

## Usage

```csharp
var aep = new AuthorityEpochProvider();

using (aep.RequestAuthority(MyAuthority.A1))
{
    // A1 is the authority for this epoch

    using (aep.RequestAuthority(MyAuthority.A2))
    {
        // A2 participates, but authority remains A1
    }
}
// FinalDispose raised once here
```

## Key Behaviors

- **Authority is established once** (0 -> 1 transition)
- **Authority remains stable** until all participants exit
- **Nested requests do not override authority**
- **FinalDispose fires once** when the last token is released
- **IsDisposing is true during FinalDispose**
- **CancelAuthorityEpoch**:
  - Ends the current epoch immediately
  - Suppresses FinalDispose
  - Allows reuse on the next line

## Token Ring Mental Model

Authorities can be treated as positions in a logical sequence (e.g., `A1 -> A2 -> A3`).

- Any participant may enter the epoch at any position
- The **first entrant defines the starting point**
- Subsequent participants may join at any position without changing the start
- Downstream logic can interpret authority relative to that starting point

Example:

```csharp
using (aep.RequestAuthority(A2))
{
    // Epoch starts at A2

    using (aep.RequestAuthority(A3))
    using (aep.RequestAuthority(A1))
    {
        // Participants span the ring, but authority remains A2
    }
}
```

This enables scenarios where work proceeds in a predictable cycle,
anchored to the first participant’s entry point.

## Notes

- `HasRequestedAuthority` reflects **active participation**, not history
- Cancellation detaches the provider from the current epoch; existing scopes complete silently
- Thread-safe via underlying host
```