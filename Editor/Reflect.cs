using System;
using System.Reflection;
using UnityEngine;

namespace TilemapTools
{
    public static class Reflect
    {
        private static bool GetTypeFromTypeName(string typeName, out Type reflectType)
        {
            reflectType = Type.GetType(typeName);
            if (reflectType == null)
            {
                Debug.LogError("Reflect: Type [" + typeName + "] not found");
            }
            return reflectType != null;
        }

        public static object InvokeMethod(Type type, string name, BindingFlags flags, object obj, object[] parameters)
        {
            MethodInfo reflectMethod = type.GetMethod(name, flags);
            if (reflectMethod == null)
            {
                Debug.LogError("Reflect: Method [" + name + "] not found in type [" + type + "]");
                return null;
            }

            return reflectMethod.Invoke(obj, parameters);
        }
        public static object InvokeMethod(string type, string name, BindingFlags flags, object obj, object[] parameters)
        {
            if (GetTypeFromTypeName(type, out Type reflectType))
            {
                return InvokeMethod(reflectType, name, flags, obj, parameters);
            }
            return null;
        }

        public static object GetField(Type type, string name, BindingFlags flags, object obj)
        {
            FieldInfo reflectField = type.GetField(name, flags);
            if (reflectField == null)
            {
                Debug.LogError("Reflect: Field [" + name + "] not found in type [" + type + "]");
                return null;
            }

            return reflectField.GetValue(obj);
        }
        public static object GetField(string type, string name, BindingFlags flags, object obj)
        {
            if (GetTypeFromTypeName(type, out Type reflectType))
            {
                return GetField(reflectType, name, flags, obj);
            }
            return null;
        }

        public static void SetField(Type type, string name, BindingFlags flags, object obj, object value)
        {
            FieldInfo reflectField = type.GetField(name, flags);
            if (reflectField == null)
            {
                Debug.LogError("Reflect: Field [" + name + "] not found in type [" + type + "]");
            }

            reflectField.SetValue(obj, value);
        }
        public static void SetField(string type, string name, BindingFlags flags, object obj, object value)
        {
            if (GetTypeFromTypeName(type, out Type reflectType))
            {
                SetField(reflectType, name, flags, obj, value);
            }
        }

        public static object GetProperty(Type type, string name, BindingFlags flags, object obj, object[] parameters)
        {
            PropertyInfo reflectField = type.GetProperty(name, flags);
            if (reflectField == null)
            {
                Debug.LogError("Reflect: Property [" + name + "] not found in type [" + type + "]");
                return null;
            }

            return reflectField.GetValue(obj, parameters);
        }
        public static object GetProperty(string type, string name, BindingFlags flags, object obj, object[] parameters)
        {
            if (GetTypeFromTypeName(type, out Type reflectType))
            {
                return GetProperty(reflectType, name, flags, obj, parameters);
            }
            return null;
        }
    }
}