using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Blender
{
    [Description]
    public enum ObjectType
    {
        [Description("'ARMATURE'")] ARMATURE,
        [Description("'CAMERA'")] CAMERA,
        [Description("'EMPTY'")] EMPTY,
        [Description("'LIGHT'")] LIGHT,
        [Description("'MESH'")] MESH,
        [Description("'OTHER'")] OTHER
    };

    public enum ColorsType
    {
        [Description("'NONE'")] NONE,
        [Description("'SRGB'")] SRGB,
        [Description("'LINEAR'")] LINEAR
    };

    public enum Axis
    {
        [Description("'X'")] X,
        [Description("'Y'")] Y,
        [Description("'Z'")] Z,
        [Description("'-X'")] X_Neg,
        [Description("'-Y'")] Y_Neg,
        [Description("'-Z'")] Z_Neg,

    };

    public enum ArmatureNodeType
    {
        [Description("'NULL'")] NULL,
        [Description("'ROOT'")] ROOT,
        [Description("'LIMBNODE'")] LIMBNODE
    };

    public enum PathMode
    {
        [Description("'AUTO'")] AUTO,
        [Description("'ABSOLUTE'")] ABSOLUTE,
        [Description("'RELATIVE'")] RELATIVE,
        [Description("'MATCH'")] MATCH,
        [Description("'STRIP'")] STRIP,
        [Description("'COPY'")] COPY
    };

    public enum ApplyScaleOptions
    {
        [Description("'FBX_SCALE_NONE'")] FBX_SCALE_NONE,
        [Description("'FBX_SCALE_UNITS'")] FBX_SCALE_UNITS,
        [Description("'FBX_SCALE_CUSTOM'")] FBX_SCALE_CUSTOM,
        [Description("'FBX_SCALE_ALL'")] FBX_SCALE_ALL
    };

    public enum MeshSmoothType
    {
        [Description("'OFF'")] OFF,
        [Description("'FACE'")] FACE,
        [Description("'EDGE'")] EDGE
    };

    public enum BatchMode
    {
        [Description("'OFF'")] OFF,
        [Description("'SCENE'")] SCENE,
        [Description("'COLLECTION'")] COLLECTION,
        [Description("'SCENE_COLLECTION'")] SCENE_COLLECTION,
        [Description("'ACTIVE_SCENE_COLLECTION'")] ACTIVE_SCENE_COLLECTION
    }
}
