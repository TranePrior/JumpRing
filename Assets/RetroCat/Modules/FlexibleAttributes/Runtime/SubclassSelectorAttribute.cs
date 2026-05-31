using System;
using UnityEngine;

namespace RetroCat.Modules.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
        public Type BaseType { get; }

        public SubclassSelectorAttribute(Type baseType)
        {
            BaseType = baseType;
        }
    }
}