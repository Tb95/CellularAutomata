using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorInspector : Editor {

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        MapGenerator mapGen = target as MapGenerator;

        if (GUILayout.Button("Generate Map"))
            mapGen.GenerateMap();
    }
}
