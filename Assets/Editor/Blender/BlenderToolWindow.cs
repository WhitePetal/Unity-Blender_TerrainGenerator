using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Threading;
using Blender;

public class BlenderToolWindow : EditorWindow
{
    private static BlenderToolWindow window;

    [MenuItem("工具/BlenderTool")]
    public static void ShowWindow()
    {
        if (window != null) window.Close();
        window = GetWindow<BlenderToolWindow>();
        window.OnInti();
    }

    private BlenderAPIs blender;

    private void OnInti()
    {
        blender = new BlenderAPIs();
        blender.Launch();
    }

    private void OnDestroy()
    {
        if(!blender.hasExited)
            blender.Close();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("保存文件"))
        {
            blender.SaveBlenderFile("/Users/baiaoxiang/Desktop/Test1.blend");
        }
        if (GUILayout.Button("移动立方体"))
        {
            blender.TranslaveSelectObj(new Vector3(20, 0, 0));
        }
        if (GUILayout.Button("导出FBX"))
        {
            blender.ExportFBX("/Users/baiaoxiang/BlenderTool/Assets/Test1.fbx", check_existing:false);
            AssetDatabase.Refresh();
            GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Test1.fbx"));
        }
        if (GUILayout.Button("导出FBX(仅选中)"))
        {
            blender.ExportFBX("/Users/baiaoxiang/BlenderTool/Assets/Test1.fbx", check_existing: false, use_selection: true);
            AssetDatabase.Refresh();
            GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Test1.fbx"));
        }
        if (GUILayout.Button("打开地形文件"))
        {
            blender.OpenBlenderFile("/Users/baiaoxiang/BlenderToolURP/Assets/Editor/ErosionSimulation/ErosionSimulation.blend");
        }
        if (GUILayout.Button("选择物体"))
        {
            blender.SelectObject("Erosion Simulation");
        }
        if (GUILayout.Button("播放动画"))
        {
            blender.PlayAnimation();
        }
        if (GUILayout.Button("暂停动画"))
        {
            blender.PauseAnimation(false);
        }
        if (GUILayout.Button("复制地形"))
        {
            blender.DuplicateObject("Erosion Simulation");
        }
        if (GUILayout.Button("应用地形修改器"))
        {
            blender.ApplyModifier("Erosion Simulation");
        }
        if (GUILayout.Button("删除选中对象"))
        {
            blender.DeleteObject(true);
        }

        if (GUILayout.Button("退出Blender"))
        {
            blender.Close();
        }
    }
}
