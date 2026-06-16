#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace LCB.UnitTest.Helpers;

internal static class ReflectionTestHelper
{
    internal static void InvokePrivate(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(target, args);
    }

    internal static T CreateInstance<T>() where T : class
    {
        var type = typeof(T);

        try
        {
            return (T)(Activator.CreateInstance(type, nonPublic: true) ?? RuntimeHelpers.GetUninitializedObject(type));
        }
        catch (MissingMethodException)
        {
            return (T)RuntimeHelpers.GetUninitializedObject(type);
        }
    }

    internal static object CreateNestedMemberInstance(object target, string memberName)
    {
        var type = target.GetType();
        var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Type memberType;

        if (prop is not null)
        {
            memberType = prop.PropertyType;
        }
        else
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? type.GetField($"<{memberName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            memberType = field!.FieldType;
        }

        try
        {
            return Activator.CreateInstance(memberType, nonPublic: true) ?? RuntimeHelpers.GetUninitializedObject(memberType);
        }
        catch (MissingMethodException)
        {
            return RuntimeHelpers.GetUninitializedObject(memberType);
        }
    }

    internal static void SetMemberValue(object target, string memberName, object? value)
    {
        var type = target.GetType();
        var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (prop?.CanWrite == true)
        {
            prop.SetValue(target, value);
            return;
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetField($"<{memberName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field!.SetValue(target, value);
    }
}
