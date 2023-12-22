using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace Blender
{
    public static class EnumTool
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return value.ToString();
        }

        public static string GetDescriptions<T>(this T[] value) where T : Enum
        {
            string result = "{";
            for (int i = 0; i < value.Length; ++i)
            {
                result += GetDescription(value[i]);
                result += ",";
            }
            result += "}";
            return result;
        }
    }
}
