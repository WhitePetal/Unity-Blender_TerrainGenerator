using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blender
{
    public class OpsCallback
    {
        public string info;
        public Action<string> cb;

        public OpsCallback(string info)
        {
            this.info = info;
        }

        public OpsCallback(string info, Action<string> cb)
        {
            this.info = info;
            this.cb = cb;
        }
    }
}
