# Validation Cases

## Case 001 — Unity object IDs stored as `int`

- Unity Upgrade Bridge detects UUB001 in both validation fixtures.
- Project Auditor 3.1.1, using Upgrade Recommendations with target 6000.5, reports "No items".
- Preliminary result: UUB001 adds information beyond Project Auditor for this case.
- Real repository validation:
  - Repository: `NeoXider/NeoxiderTools`
  - Regression pattern: `int id = upgrade.GetEntityId();`
  - The value was subsequently used with `Dictionary<int, int>`.
  - The current UUB001 scanner detects the real-world pattern.
  - No production-code change was required.
- All 12 EditMode tests pass.

## Case 002 — EntityId serialized as a numeric JSON value

- Fixture: `Assets/ValidationCases/Uub002JsonNumericBroken.cs` converts an `EntityId` to a raw 64-bit integer and prepares it for JSON as a numeric value.
- Expected risk: A 64-bit EntityId may lose precision when consumed as a JavaScript number.
- Unity Upgrade Bridge detects UUB002 for the numeric JSON EntityId risk.
- The UUB002 positive and negative tests pass.
- Project Auditor 3.1.1, using Upgrade Recommendations with target 6000.5, reports "No items".
- Preliminary result: UUB002 provides added value beyond Project Auditor for this case.
- Real repository validation:
  - Repository: `AnkleBreaker-Studio/unity-mcp-plugin`
  - Historical regression: A 64-bit `EntityId` was transported as numeric `instanceId` through a JSON/JavaScript bridge and lost precision.
  - The current UUB002 scanner detects the reduced historical pattern.
  - When a JSON sink is proven, it reports normal UUB002.
  - When transport cannot be proven locally, it reports Needs Manual Review.
  - String transport remains a negative.
- All 13 EditMode tests pass.

## Case 003 — EntityId identity replaced by a hash code

- Fixture: `Assets/ValidationCases/Uub003HashCodeBroken.cs` stores `target.GetEntityId().GetHashCode()` as an `int` ID.
- Expected risk: `GetHashCode()` produces a 32-bit hash, not the original `EntityId` identity.
- Unity Upgrade Bridge detects UUB003 for `GetEntityId().GetHashCode()`.
- The UUB003 positive and negative tests pass.
- Scan Project reports the UUB003 finding end-to-end.
- Project Auditor 3.1.1, using Upgrade Recommendations with target 6000.5, reports "No items".
- Preliminary result: UUB003 provides added value beyond Project Auditor for this case.
- Real repository validation:
  - Repository: `KhronosGroup/UnityGLTF`
  - File: `Runtime/Scripts/SceneExporter/ExporterAnimation.cs`
  - The current UUB003 scanner reports one finding.
  - Matched line 1598: `return obj.GetEntityId().GetHashCode();`
  - The helper is used as a key in `TryGetValue` lookups for exported object maps.
  - This confirms UUB003 on a real open-source repository file.
- The ML-Agents seed case remains a negative and is not reported.
- All 11 EditMode tests pass.
