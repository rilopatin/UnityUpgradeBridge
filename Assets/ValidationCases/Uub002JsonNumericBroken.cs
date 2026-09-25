using System;
using UnityEngine;

internal static class Uub002JsonNumericBroken
{
    [Serializable]
    private struct Payload
    {
        public long entityId;
    }

    public static string Serialize(UnityEngine.Object target)
    {
        EntityId entityId = target.GetEntityId();
        long rawEntityId = entityId;

        return JsonUtility.ToJson(new Payload { entityId = rawEntityId });
    }
}
