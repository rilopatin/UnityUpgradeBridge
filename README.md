# Unity Upgrade Bridge

> **Beta:** This is an experimental report-only tool. It may produce false positives or miss issues. Always review findings manually before changing project code.

Unity Editor tool for detecting selected API migration risks in Unity 6.x projects.

Tested with Unity 6000.3.10f1.

## Current v0.1 rules

- **UUB001** — detects `EntityId` values stored in `int`
- **UUB002** — detects 64-bit `EntityId` transported as numeric `instanceId`, where JSON/JavaScript precision may be lost
- **UUB003** — detects `GetEntityId().GetHashCode()` when it appears to be used as object identity

## Installation

In Unity:

1. Open `Window → Package Manager`
2. Click `+`
3. Choose `Add package from git URL...`
4. Enter:

`https://github.com/rilopatin/UnityUpgradeBridge.git?path=/Package/com.rilopatin.unity-upgrade-bridge`

5. Click `Add`

## Usage

In Unity, run:

`Tools → Unity Upgrade Bridge → Scan Project`

Findings are written to the Unity Console and include:

- file
- line number
- rule ID
- explanation

Example:

`Assets/MyScript.cs, line 12, UUB001, ...`

When nothing suspicious is found:

`Unity Upgrade Bridge: scan complete — 0 finding(s).`

## Important

Unity Upgrade Bridge is report-only.

It does **not** modify your project or automatically fix code.

A finding means that the code should be reviewed. It does not necessarily mean the code is incorrect.

## Feedback

This is an early beta.

If the tool finds a real migration problem in your project, misses one, or produces a false positive, please open a GitHub Issue.

## License

Mozilla Public License 2.0 (MPL-2.0)