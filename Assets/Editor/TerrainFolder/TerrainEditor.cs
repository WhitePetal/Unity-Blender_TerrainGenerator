using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Blender;

namespace TerrainEditor
{
    public class TerrainEditor : MonoBehaviour
    {
        private static BlenderAPIs blender;

        [MenuItem("工具/地形/打开Blender文件")]
        public static void OpenBlenderFile()
        {
            if (blender != null)
            {
                blender.Close();
            }
            blender = new BlenderAPIs();
            blender.Launch();

            while (!blender.launched) ;

            Debug.Log("blender Path: " + Const.TerrainBlenderFilePath);
            blender.OpenBlenderFile(Application.dataPath.Replace("Assets", "") + Const.TerrainBlenderFilePath);
        }

        [MenuItem("工具/地形/导出地形网格")]
        public static void ExportTerrainMesh()
        {
            blender.SelectObject(Const.TerrainBlenderName);
            blender.ExportFBX(Application.dataPath.Replace("Assets", "") + Const.TerrainMeshPath,
                check_existing: false, use_selection: true, axis_forward: Axis.Z_Neg, axis_up: Axis.Y, use_space_transform: true, bake_space_transform: true);
        }

        [MenuItem("工具/地形/导出树木网格")]
        public static void ExportTreesMesh()
        {
            blender.SelectObject(Const.TreesBlenderNames);
            blender.ExportFBX(Application.dataPath.Replace("Assets", "") + Const.TreesMeshPath,
                check_existing: false, use_selection: true, axis_forward: Axis.Z_Neg, axis_up: Axis.Y, use_space_transform: true, bake_space_transform: true);
        }

        [MenuItem("工具/地形/设置高度")]
        public static void SetTerrainHeights()
        {
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Const.TerrainDataPath);
            GameObject terrainMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Const.TerrainMeshPath);
            int terLayer = LayerMask.NameToLayer("Terminal");

            GameObject terrainMesh = GameObject.Instantiate(terrainMeshPrefab);
            terrainMesh.AddComponent<MeshCollider>();
            terrainMesh.layer = terLayer;

            Vector3 terrainSize = terrainData.size;
            int heightMapResolution = terrainData.heightmapResolution;
            float[,] heights = new float[heightMapResolution, heightMapResolution];
            float terrScale = heightMapResolution / terrainSize.x;
            for (float z = 0; z < terrainSize.z; ++z)
            {
                for (float x = 0; x < terrainSize.x; ++x)
                {
                    int xIndex = Mathf.RoundToInt(x * terrScale);
                    int zIndex = Mathf.RoundToInt(z * terrScale);
                    if (Physics.Raycast(new Vector3(z, 1280, x), new Vector3(0f, -1280f, 0f), out RaycastHit hitinfo, 1280, 1 << terLayer))
                    {
                        heights[xIndex, zIndex] = hitinfo.point.y / terrainData.heightmapScale.y;
                    }
                }
            }
            GameObject.DestroyImmediate(terrainMesh);
        }

        [MenuItem("工具/地形/设置树木")]
        public static void SetTerrainTress()
        {
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Const.TerrainDataPath);
            GameObject terrainMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Const.TerrainMeshPath);
            GameObject treesMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Const.TreesMeshPath);

            GameObject terrainMesh = GameObject.Instantiate(terrainMeshPrefab);
            GameObject treesMesh = GameObject.Instantiate(treesMeshPrefab);
            //terrainMesh.transform.localScale = terrainMesh.transform.localScale * 10f;

            int prototypeCount = treesMesh.transform.childCount;
            TreePrototype[] prototypes = new TreePrototype[prototypeCount];
            string[] prototypeNames = new string[prototypeCount];
            List<TreeInstance> treeInstanceLst = new List<TreeInstance>();

            for (int i = 0; i < prototypeCount; ++i)
            {
                Transform treeTrans = treesMesh.transform.GetChild(i);
                GameObject tree = GameObject.Instantiate(treeTrans.gameObject);
                tree.transform.localPosition = Vector3.zero;
                tree.transform.localRotation = Quaternion.identity;
                tree.transform.localScale = Vector3.one;
                LODGroup lODGroup = tree.AddComponent<LODGroup>();
                LOD[] lods = lODGroup.GetLODs();
                for(int j = 0; j < lods.Length; ++j)
                {
                    lods[j].renderers = tree.GetComponentsInChildren<MeshRenderer>();
                }
                lODGroup.SetLODs(lods);
                lODGroup.RecalculateBounds();

                GameObject treeP = PrefabUtility.SaveAsPrefabAsset(tree, Const.TreesPrefabFolder + treeTrans.gameObject.name + ".prefab");

                TreePrototype prototype = new TreePrototype();
                prototype.prefab = treeP;
                prototype.navMeshLod = 1;
                prototypes[i] = prototype;
                prototypeNames[i] = treeP.name;

                GameObject.DestroyImmediate(tree);
            }

            foreach (Transform trans in terrainMesh.transform.Find(Const.TreesRootName))
            {
                for(int i = 0; i < prototypeCount; ++i)
                {
                    if (trans.gameObject.name.Contains(prototypeNames[i]))
                    {
                        TreeInstance ti = new TreeInstance();
                        ti.heightScale = trans.localScale.y;
                        ti.widthScale = trans.localScale.x;
                        ti.position = trans.position;
                        ti.position.x = trans.position.x / terrainData.size.x;
                        ti.position.z = trans.position.z / terrainData.size.z;
                        ti.rotation = trans.rotation.eulerAngles.y * Mathf.Deg2Rad;
                        ti.prototypeIndex = i;
                        ti.color = Color.white;
                        ti.lightmapColor = Color.white;
                        treeInstanceLst.Add(ti);
                    }
                }
            }

            terrainData.treePrototypes = prototypes;
            terrainData.SetTreeInstances(treeInstanceLst.ToArray(), true);
            GameObject.DestroyImmediate(terrainMesh);
            GameObject.DestroyImmediate(treesMesh);
        }

        [MenuItem("工具/地形/初始化图层")]
        public static void InitTextureLayers()
        {

        }

        [MenuItem("工具/地形/应用地形图层")]
        public static void ApplyTextureLayers()
        {
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Const.TerrainDataPath);
            Texture2D splatMap0 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/ErosionSimulation/Splatmap0.png");
            Texture2D splatMap1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/ErosionSimulation/Splatmap1.png");
            Texture2D splatMap2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/ErosionSimulation/Splatmap2.png");
            float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 8];
            for(int y = 0; y < terrainData.alphamapHeight; ++y)
            {
                for(int x = 0; x < terrainData.alphamapWidth; ++x)
                {
                    Color col0 = splatMap0.GetPixel(1023 - y, 1023 - x);
                    Color col1 = splatMap1.GetPixel(1023 - y, 1023 - x);
                    Color col2 = splatMap2.GetPixel(1023 - y, 1023 - x);
                    map[x, y, 0] = Mathf.Clamp01(1f - (col0.r + col0.g + col0.b + col1.r + col1.g + col1.b + col2.r));
                    map[x, y, 1] = col0.r;
                    map[x, y, 2] = col0.g;
                    map[x, y, 3] = col0.b;
                    map[x, y, 4] = col1.r;
                    map[x, y, 5] = col1.g;
                    map[x, y, 6] = col1.b;
                    map[x, y, 7] = col2.r > 0 ? 1 : 0;
                }
            }
            terrainData.SetAlphamaps(0, 0, map);
            Debug.Log("splat map widht height: " + terrainData.alphamapWidth + "," + terrainData.alphamapHeight);
        }
    }
}
