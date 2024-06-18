using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[CreateAssetMenu(fileName = "AtomicSettings", menuName = "Atomic/Settings")]
public class AtomicSettings : ScriptableObject
{
    public List<string> installationPaths = new List<string>();

    public string buildPath = "Assets/AddressableAssetsData/atomic_build/";
    
    public AddressableAssetGroup addressableAssets;
    public AddressableAssetGroup addressablescene;
}
