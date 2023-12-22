using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainEditor
{
    public static class Const
    {
        public static string TerrainBlenderFilePath = "Assets/Editor/ErosionSimulation/ErosionSimulation.blend";
        public static string TerrainDataPath = "Assets/TerrainData.asset";
        public static string TerrainMeshPath = "Assets/ErosionSimulation.fbx";
        public static string TerrainBlenderName = "Erosion Simulation";
        public static string TreesMeshPath = "Assets/Trees.fbx";
        public static string TreesPrefabFolder = "Assets/";
        public static string TreesRootName = "Erosion Simulation";
        public static string[] TreesBlenderNames = new string[]
        {
            "Deciduous Tree 1",
            "Deciduous Tree 2",
            "Coniferous Tree 1"
        };
    }
}
