using UnityEngine;

internal static class Uub003HashCodeBroken
{
    public static int GetId(Object target)
    {
        int id = target.GetEntityId().GetHashCode();
        return id;
    }
}
