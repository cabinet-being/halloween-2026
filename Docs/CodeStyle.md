# Code Style

## General Principles

- Prefer small components with a single responsibility.
- Use serialized private fields for Inspector settings.
- Validate critical dependencies in `Awake()` and disable incorrectly configured components.
- Express required component dependencies with `RequireComponent`.
- Use one-line guard clauses for simple `return`, `continue`, or `break` checks.
- Initialize local variables near the start of a method, after any guards they depend on.

## Inspector Layout

- Put input and external references before gameplay tuning and limits.
- Use `Header` for meaningful groups.
- Add `Tooltip` to serialized references and debug fields whose purpose is not obvious.
- Use `Min` or `Range` where appropriate for the field's valid values.
- Preserve serialized data with `FormerlySerializedAs` when renaming fields.

## Organization

- Keep actor input, interaction, and UI responsibilities separate.
- Keep editor-only scripts in an `Editor` folder.
- Follow [NamingConvention.md](NamingConvention.md).

These conventions are adapted from mousestaurant; no custom runtime components or helper APIs exist yet.
