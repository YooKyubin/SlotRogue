using System;
using UnityEngine;

namespace SlotRogue.Core.Tooling
{
    public enum AutoWireSearchScope
    {
        Children,
        Parents,
        Scene,
        OpenScenes
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AutoWireAttribute : PropertyAttribute
    {
        public AutoWireAttribute()
            : this(null, AutoWireSearchScope.Scene)
        {
        }

        public AutoWireAttribute(string objectName)
            : this(objectName, AutoWireSearchScope.Scene)
        {
        }

        public AutoWireAttribute(AutoWireSearchScope scope)
            : this(null, scope)
        {
        }

        public AutoWireAttribute(string objectName, AutoWireSearchScope scope)
        {
            ObjectName = objectName;
            Scope = scope;
        }

        public string ObjectName { get; }

        public AutoWireSearchScope Scope { get; }

        public bool IncludeInactive { get; set; } = true;

        public bool AllowOverwrite { get; set; }
    }
}
