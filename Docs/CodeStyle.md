# Code Style

## General Principles

- Prefer small components with a single responsibility.
- Use serialized private fields for Inspector settings.
- Validate critical dependencies in `Awake()` and disable incorrectly configured components.
- Express required component dependencies with `RequireComponent`.
- Use one-line guard clauses for simple `return`, `continue`, or `break` checks.
- Declare all ordinary local variables at the beginning of the method, separated from executable logic by a blank line.
- Initialize locals there when safe. If a value depends on validation, an action, a load, or a coroutine yield, declare it first and assign it at the original execution point. Do not move side effects earlier just to group declarations.
- Keep `foreach` iteration variables and `catch` exception variables in their language-defined scopes. Declare `out` variables at the beginning of the method.
- Separate validation, preparation, actions, notifications, and results into meaningful paragraphs with blank lines. Keep related statements together; do not insert a blank line after every statement.
- Expand statement-bodied methods and constructors onto multiple lines. Simple expression-bodied properties and methods are fine.
- In tests, visually separate setup, actions, and assertions; separate each additional scenario or phase as well.

## Inspector Layout

- Place attribute lists on the line immediately above the field, property, method, or class, never on the declaration line. Group related attributes together (for example, `[SerializeField, Min(1f)]`).
- Separate attributed fields with a blank line.

- Put input and external references before gameplay tuning and limits.
- Use `Header` for meaningful groups.
- Add `Tooltip` to serialized references and debug fields whose purpose is not obvious.
- Use `Min` or `Range` where appropriate for the field's valid values.
- Preserve serialized data with `FormerlySerializedAs` when renaming fields.

## Organization

- Keep actor input, interaction, and UI responsibilities separate.
- Keep editor-only scripts in an `Editor` folder.
- Follow [NamingConvention.md](NamingConvention.md).

Runtime code uses the `Halloween2026` namespace; editor tooling and tests are isolated in their own assemblies.
