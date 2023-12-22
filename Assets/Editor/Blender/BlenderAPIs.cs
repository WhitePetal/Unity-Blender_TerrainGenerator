using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using UnityEngine;

namespace Blender
{
    public class BlenderAPIs
    {
        public bool launched;
        public bool hasExited;

        private Thread processThread;
        private Process cmdProcess;
        private ProcessStartInfo processInfo;

        private readonly object waitCountLock = new object();
        private int waitCount;

        private Queue<OpsCallback> opsCallbackQueue;

        public void Launch()
        {
            hasExited = false;

            opsCallbackQueue = new Queue<OpsCallback>();
            processThread = new Thread(new ThreadStart(LaunchBlenderPythonConsole));
            processThread.Start();
        }

        private void LaunchBlenderPythonConsole()
        {
            processInfo = new ProcessStartInfo(@"/Users/baiaoxiang/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender", "--python-console");
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardInput = true;

            using (cmdProcess = new Process())
            {
                cmdProcess.StartInfo = processInfo;
                cmdProcess.OutputDataReceived += ProcessOutputReceiver;
                cmdProcess.ErrorDataReceived += ProcessErrorReceiver;
                cmdProcess.Start();
                cmdProcess.BeginOutputReadLine();
                cmdProcess.BeginErrorReadLine();
                cmdProcess.StandardInput.AutoFlush = true;
                cmdProcess.StandardInput.WriteLine("import bpy");
                cmdProcess.StandardInput.WriteLine("main_window = None");
                cmdProcess.StandardInput.WriteLine("view3d_area = None");
                cmdProcess.StandardInput.WriteLine("dopesheet_area = None");
                cmdProcess.StandardInput.WriteLine("properties_area = None");
                launched = true;
                cmdProcess.WaitForExit();
            }
        }

        private void GetView3DArea()
        {
            cmdProcess.StandardInput.WriteLine("main_window = bpy.context.window_manager.windows[0]");
            cmdProcess.StandardInput.WriteLine("for area in main_window.screen.areas:");
            cmdProcess.StandardInput.WriteLine("    if area.type == 'VIEW_3D':");
            cmdProcess.StandardInput.WriteLine("        view3d_area = area");
            cmdProcess.StandardInput.WriteLine("\n");
        }

        private void GetDopeSheetArea()
        {
            cmdProcess.StandardInput.WriteLine("main_window = bpy.context.window_manager.windows[0]");
            cmdProcess.StandardInput.WriteLine("for area in main_window.screen.areas:");
            cmdProcess.StandardInput.WriteLine("    if area.type == 'DOPESHEET_EDITOR':");
            cmdProcess.StandardInput.WriteLine("        dopesheet_area = area");
            cmdProcess.StandardInput.WriteLine("\n");
        }

        private void GetPropertiesArea()
        {
            cmdProcess.StandardInput.WriteLine("main_window = bpy.context.window_manager.windows[0]");
            cmdProcess.StandardInput.WriteLine("for area in main_window.screen.areas:");
            cmdProcess.StandardInput.WriteLine("    if area.type == 'PROPERTIES':");
            cmdProcess.StandardInput.WriteLine("        properties_area = area");
            cmdProcess.StandardInput.WriteLine("\n");
        }

        private void OverrideContextInvoke(string window, string area, params string[] cmd)
        {
            cmdProcess.StandardInput.WriteLine(string.Format("with bpy.context.temp_override(window={0}, area={1}):", window, area));
            for(int i = 0; i < cmd.Length; ++i)
            {
                cmdProcess.StandardInput.WriteLine("    " + cmd[i]);
            }
            cmdProcess.StandardInput.WriteLine("\n");
        }

        private void ProcessOutputReceiver(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                lock (waitCountLock)
                {
                    if (waitCount > 0 && (e.Data.Contains("FINISHED") || e.Data.Contains("PASS_THROUGH") || e.Data.Contains("CANCELLED")))
                    {
                        if(opsCallbackQueue.Count > 0)
                        {
                            ProcessOpsCB(opsCallbackQueue.Dequeue(), e.Data);
                        }
                        --waitCount;
                    }
                    else
                    {
                        UnityEngine.Debug.Log(e.Data);
                    }
                }
            }
        }

        private void ProcessOpsCB(OpsCallback cb, string data)
        {
            UnityEngine.Debug.Log(cb.info);
            if(cb.cb != null)
            {
                cb.cb.Invoke(data);
            }
            UnityEngine.Debug.Log(data);
        }

        private void ProcessErrorReceiver(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                UnityEngine.Debug.LogError(e.Data);
        }

        private void AddWaitCount(OpsCallback callback)
        {
            lock (waitCountLock)
            {
                ++waitCount;
                opsCallbackQueue.Enqueue(callback);
            }
        }

        public void WaitForPorcess()
        {
            int allWaitCount = waitCount;
            while(true)
            {
                lock (waitCountLock)
                {
                    UnityEditor.EditorUtility.DisplayProgressBar("等待Blender后台操作...", (allWaitCount - waitCount) + "/" + allWaitCount, 1.0f - waitCount * 1f / allWaitCount);
                    if (waitCount == 0)
                    {
                        UnityEditor.EditorUtility.ClearProgressBar();
                        return;
                    }
                }
            }
        }

        public void Close()
        {
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                cmdProcess.StandardInput.WriteLine(@"bpy.ops.wm.quit_blender()");
                ++waitCount;
                cmdProcess.CloseMainWindow();
                cmdProcess.Close();
            }
            hasExited = true;
        }

        public void SaveBlenderFile(string filePath)
        {
            AddWaitCount(new OpsCallback("Save File Ops, filePath: " + filePath));
            cmdProcess.StandardInput.WriteLine(string.Format(@"bpy.ops.wm.save_as_mainfile(filepath='{0}')", filePath));
        }

        public void OpenBlenderFile(string filePath)
        {
            AddWaitCount(new OpsCallback("Open File Ops, filePath: " + filePath));
            cmdProcess.StandardInput.WriteLine(string.Format(@"bpy.ops.wm.open_mainfile(filepath='{0}')", filePath));
        }

        public void SelectObject(params string[] objNames)
        {
            if (objNames == null) return;
            GetView3DArea();
            AddWaitCount(new OpsCallback("Select All Ops, action: DESELECT"));
            OverrideContextInvoke("main_window", "view3d_area",
                "bpy.ops.object.select_all(action='DESELECT')",
                "bpy.context.view_layer.objects.active = None");

            for(int i = 0; i < objNames.Length; ++i)
            {
                OverrideContextInvoke("main_window", "view3d_area",
                    string.Format("bpy.context.view_layer.objects.active = bpy.context.view_layer.objects['{0}']", objNames[i]),
                    "bpy.context.view_layer.objects.active.select_set(state=True)");
            }
        }

        public void ExportFBX(string filePath, bool check_existing = true, string filter_glob = "*.fbx", bool use_selection = false, bool use_visible = false,
            bool use_active_collection = false, float global_scale = 1f, bool apply_uint_scale = true, ApplyScaleOptions apply_scale_options = ApplyScaleOptions.FBX_SCALE_NONE, bool use_space_transform = true,
            bool bake_space_transform = false, ObjectType[] object_types = null, bool use_mesh_modifiers = true,
            bool use_mesh_modifiers_render = true, MeshSmoothType mesh_smooth_type = MeshSmoothType.OFF, ColorsType colors_type = ColorsType.SRGB, bool prioritize_active_color = false, bool use_subsurf = false,
            bool use_mesh_edges = false, bool use_tspace = false, bool use_triangles = false, bool use_custom_props = false, bool add_leaf_bones = true, Axis primary_bone_axis = Axis.Y,
            Axis secondary_bone_axis = Axis.X, bool use_armature_deform_only = false, ArmatureNodeType armature_nodetype = ArmatureNodeType.NULL, bool bake_anim = true, bool bake_anim_use_all_bones = true,
            bool bake_anim_use_nla_strips = true, bool bake_anim_use_all_actions = true, bool bake_anim_force_startend_keying = true, float bake_anim_step = 1f,
            float bake_anim_simplify_factor = 1f, PathMode path_mode = PathMode.AUTO, bool embed_textures = false, BatchMode batch_mode = BatchMode.OFF, bool use_batch_own_dir = true, bool use_metadata = true,
            Axis axis_forward = Axis.Z_Neg, Axis axis_up = Axis.Y)
        {
            if (object_types == null)
            {
                object_types = new ObjectType[] { ObjectType.ARMATURE, ObjectType.CAMERA, ObjectType.EMPTY, ObjectType.LIGHT, ObjectType.MESH, ObjectType.OTHER};
            }

            GetView3DArea();

            AddWaitCount(new OpsCallback("Export Fbx Ops, filePath: " + filePath));
            string cmd = string.Format(@"bpy.ops.export_scene.fbx(filepath='{0}', check_existing={1}, filter_glob='{2}', use_selection={3}, use_visible={4},
                use_active_collection={5}, global_scale={6}, apply_unit_scale={7}, apply_scale_options={8}, use_space_transform={9},
                bake_space_transform={10}, object_types={11}, use_mesh_modifiers={12},
                use_mesh_modifiers_render={13}, mesh_smooth_type={14}, colors_type={15}, prioritize_active_color={16}, use_subsurf={17},
                use_mesh_edges={18}, use_tspace={19}, use_triangles={20}, use_custom_props={21}, add_leaf_bones={22}, primary_bone_axis={23},
                secondary_bone_axis={24}, use_armature_deform_only={25}, armature_nodetype={26}, bake_anim={27}, bake_anim_use_all_bones={28},
                bake_anim_use_nla_strips={29}, bake_anim_use_all_actions={30}, bake_anim_force_startend_keying={31}, bake_anim_step={32},
                bake_anim_simplify_factor={33}, path_mode={34}, embed_textures={35}, batch_mode={36}, use_batch_own_dir={37}, use_metadata={38},
                axis_forward={39}, axis_up={40})",
                filePath, check_existing, filter_glob, use_selection, use_visible,
                use_active_collection, global_scale, apply_uint_scale, apply_scale_options.GetDescription(), use_space_transform,
                bake_space_transform, object_types.GetDescriptions(), use_mesh_modifiers,
                use_mesh_modifiers_render, mesh_smooth_type.GetDescription(), colors_type.GetDescription(), prioritize_active_color, use_subsurf,
                use_mesh_edges, use_tspace, use_triangles, use_custom_props, add_leaf_bones, primary_bone_axis.GetDescription(),
                secondary_bone_axis.GetDescription(), use_armature_deform_only, armature_nodetype.GetDescription(), bake_anim, bake_anim_use_all_bones,
                bake_anim_use_nla_strips, bake_anim_use_all_actions, bake_anim_force_startend_keying, bake_anim_step,
                bake_anim_simplify_factor, path_mode.GetDescription(), embed_textures, batch_mode.GetDescription(), use_batch_own_dir, use_metadata,
                axis_forward.GetDescription(), axis_up.GetDescription());
            OverrideContextInvoke("main_window", "view3d_area", cmd);
            cmdProcess.StandardInput.WriteLine("\n");
            WaitForPorcess();
        }

        public void PlayAnimation()
        {
            AddWaitCount(new OpsCallback("Play Animation"));
            GetDopeSheetArea();
            for(int i = 0; i < 400; ++i)
            {
                OverrideContextInvoke("main_window", "dopesheet_area",
                    "print('CURRENT_FRAME:' + str(bpy.context.scene.frame_current))",
                    "bpy.context.scene.frame_set(i)"
                );
            }
            cmdProcess.StandardInput.WriteLine("print(\"{'FINISHED'}\")");
        }

        public void PauseAnimation(bool restoreFrame)
        {
            GetDopeSheetArea();
            OverrideContextInvoke("main_window", "dopesheet_area", "print(bpy.context.scene.frame_current)");
            OverrideContextInvoke("main_window", "dopesheet_area", string.Format("bpy.ops.screen.animation_cancel(restore_frame={0})", restoreFrame));
        }

        public void TranslaveSelectObj(Vector3 translate)
        {
            AddWaitCount(new OpsCallback("Translate Object Ops"));
            cmdProcess.StandardInput.WriteLine(string.Format(@"bpy.ops.transform.translate(value={0},
                orient_type='GLOBAL', orient_matrix=((1, 0, 0), (0, 1, 0), (0, 0, 1)), orient_matrix_type='GLOBAL',
                constraint_axis=(True, False, False), mirror=False, use_proportional_edit=False, proportional_edit_falloff='SMOOTH',
                proportional_size=1, use_proportional_connected=False, use_proportional_projected=False, snap=False, snap_elements={{'INCREMENT'}},
                use_snap_project=False, snap_target='CLOSEST', use_snap_self=True, use_snap_edit=True, use_snap_nonedit=True, use_snap_selectable=False,
                alt_navigation=True)", translate));
        }

        public void DuplicateObject(string objName, bool linked = false)
        {
            SelectObject(objName);
            GetView3DArea();
            AddWaitCount(new OpsCallback("Duplicate Object Ops, objName: " + objName));
            OverrideContextInvoke("main_window", "view3d_area",
                string.Format(@"bpy.ops.object.duplicate(linked={0}, mode='TRANSLATION')", linked),
                "duplicate_obj = bpy.context.selected_objects[0]"
            );
        }

        public void ApplyModifier(string modiferName)
        {
            GetPropertiesArea();
            AddWaitCount(new OpsCallback("Apply Modifer, modifer: " + modiferName));
            OverrideContextInvoke("main_window", "properties_area", "bpy.context.space_data.context = 'MODIFIER'", string.Format("bpy.ops.object.modifier_apply(modifier='{0}')", modiferName));
        }

        public void DeleteObject(bool use_global = false, bool confirm = false)
        {
            GetView3DArea();
            AddWaitCount(new OpsCallback("Delete Object"));
            OverrideContextInvoke("main_window", "view3d_area", string.Format("bpy.ops.object.delete(use_global={0}, confirm={1})", use_global, confirm));
        }

        public void DeleteObject(bool use_global = false, bool confirm = false, params string[] objNames)
        {
            for(int i = 0; i < objNames.Length; ++i)
            {
                SelectObject(objNames[i]);
                DeleteObject(use_global, confirm);
            }
        }
    }

}