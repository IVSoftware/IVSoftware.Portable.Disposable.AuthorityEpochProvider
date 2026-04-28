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
- **Nested and overlapping requests do not override authority**
- **FinalDispose fires once** when the last token is released
- **IsDisposing is true during FinalDispose**
- **CancelAuthorityEpoch**:
  - Ends the current epoch immediately
  - Suppresses FinalDispose
  - Allows reuse on the next line


> _An optional shared ephemeral context (see *Context* below) is available while tokens are active._
___

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

___

## Notes

- `HasRequestedAuthority` reflects **active participation**, not history
- Cancellation detaches the provider from the current epoch; existing scopes complete silently
- Thread-safe via underlying host

___

## Context (Optional)


`AuthorityEpochProvider` inherits [dictionary](https://github.com/IVSoftware/IVSoftware.Portable.Disposable/blob/master/IVSoftware.Portable.Disposable/README/dictionary-as-context.md) semantics from [DisposableHost](https://github.com/IVSoftware/IVSoftware.Portable.Disposable/blob/master/README.md):

- Participants may contribute key/value pairs during the epoch
- Values are aggregated across participants
- A final, immutable snapshot is emitted on `FinalDispose`

While not required for authority coordination, this enables a powerful pattern:
a shared, mutable context that evolves across the epoch and resolves deterministically at teardown.

___
_While AuthorityEpochProvider is intended for production code, there is also utility when it comes to unit testing. Version 1.0.3 adds this handy test utility._
___

# TestableEpoch

## Deterministic Test Data

`TestableEpoch` is useful when a unit test needs fast, repeatable
fixture data.

The pattern is simple:

- Write production-style code such as
  `Guid.NewGuid().WithTestability().ToString()`
- Wrap the fixture builder in `using var te = this.TestableEpoch();`
- Capture the generated text model once
- Paste that text back into `expected`

Why this helps:

- The call site stays natural
- The generated ids and timestamps become deterministic inside the test
- Large copied expectations remain stable across runs
- Test writing stays fast because "generate, inspect, paste" is safe

Why this matters:

If a production model like `PlaceableModel` is allowed to create real
Guids during the test, then the text model changes every run. The copied
`expected` value becomes noise instead of a useful assertion.

With `TestableEpoch`, the same fixture builder produces the same ids and
timestamps every time, so verbose modeled output can be captured and
asserted directly.

Typical use:

```csharp
[TestMethod, DoNotParallelize]
public void Test_Something()
{
    using var te = this.TestableEpoch();
    var id = Guid.NewGuid().WithTestability().ToString();
}
```

`DoNotParallelize` is important because `TestableEpoch` uses static state
and must remain isolated per test.

This is especially handy for tests that build on-the-fly XML or text
models, inspect the result once, and then lock it in as the expected
value.
