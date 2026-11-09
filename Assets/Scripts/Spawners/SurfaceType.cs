using System.Collections.Generic;
using UnityEngine;

public enum SurfaceType { OpenSpace, Reception, MeetingRoom, Cafe }

[System.Serializable]
public class PropGroup
{
    public string groupName;
    public List<GameObject> props;
}

[System.Serializable]
public class SurfacePropSet
{
    public SurfaceType surfaceType;
    public List<PropGroup> propGroups;
}

[System.Serializable]
public class WallSurfacePropSet
{
    public SurfaceType surfaceType;

    [Header("Propy stojące przy ścianie")]
    public List<GameObject> standingProps;

    [Header("Propy wiszące")]
    public List<GameObject> hangingProps;

    [Header("Propy stojące z możliwością zawieszenia nad nimi")]
    public List<GameObject> standingWithHangingAllowed;
}