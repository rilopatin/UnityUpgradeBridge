# Unity Upgrade Bridge

Small Unity Editor tool for detecting selected API migration risks in Unity 6.x projects.

## Current v0.1 rules

- UUB001 — EntityId stored in `int`
- UUB002 — 64-bit EntityId transported as numeric `instanceId` with possible JSON/JavaScript precision loss
- UUB003 — `GetEntityId().GetHashCode()` used as object identity

## Usage

In Unity:

`Tools → Unity Upgrade Bridge → Scan Project`

Findings are reported in the Unity Console.

The tool is report-only and does not modify project code automatically.