using System.Collections.Generic;
using UnityEngine;

internal sealed class Uub001DictionaryKeyBroken
{
    private readonly Dictionary<int, Object> objectsById = new Dictionary<int, Object>();

    public void Add(Object target)
    {
        int id = target.GetEntityId();
        objectsById[id] = target;
    }
}
