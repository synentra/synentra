using Synentra.Domain.Common.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Synentra.UnitTests.Diagnostics;

/// <summary>
/// Walks every Synentra assembly and yields every reachable
/// <see cref="Diagnostic"/> declaration (static field, static property,
/// or static factory method) along with its declaring member.
/// </summary>
internal static class DiagnosticCatalog
{
    private static readonly string[] KnownAssemblies =
    {
        "Synentra.Domain",
        "Synentra.Application",
        "Synentra.Infrastructure",
        "Synentra"
    };

    public static IEnumerable<DiagnosticDeclaration> Enumerate()
    {
        foreach (var assembly in SynentraAssemblies())
            foreach (var type in SafeTypes(assembly))
            {
                EnsureInitialized(type);

                foreach (var declaration in FromFields(type)) yield return declaration;
                foreach (var declaration in FromProperties(type)) yield return declaration;
                foreach (var declaration in FromMethods(type)) yield return declaration;
            }
    }

    private static IEnumerable<DiagnosticDeclaration> FromFields(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(Diagnostic)) continue;
            if (field.GetValue(null) is Diagnostic value)
                yield return new DiagnosticDeclaration(value, field);
        }
    }

    private static IEnumerable<DiagnosticDeclaration> FromProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.PropertyType != typeof(Diagnostic)) continue;
            if (property.GetGetMethod()?.IsStatic != true) continue;
            if (property.GetValue(null) is Diagnostic value)
                yield return new DiagnosticDeclaration(value, property);
        }
    }

    private static IEnumerable<DiagnosticDeclaration> FromMethods(Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.ReturnType != typeof(Diagnostic)) continue;
            if (method.GetParameters().Length == 0) continue;

            var value = TryInvoke(method);
            if (value is not null)
                yield return new DiagnosticDeclaration(value, method);
        }
    }

    private static Diagnostic? TryInvoke(MethodInfo method)
    {
        var args = method.GetParameters()
            .Select(p => DummyArgument(p.ParameterType))
            .ToArray();

        try { return (Diagnostic?)method.Invoke(null, args); }
        catch { return null; }    // a factory that rejects its own dummy args is a design smell flagged elsewhere
    }

    private static object? DummyArgument(Type type)
    {
        if (type == typeof(string)) return "x";        // non-empty for ThrowIfNullOrWhiteSpace
        if (type.IsValueType) return Activator.CreateInstance(type);
        return null;
    }

    private static void EnsureInitialized(Type type)
    {
        try { RuntimeHelpers.RunClassConstructor(type.TypeHandle); }
        catch { /* static ctor failure surfaces in its own test */ }
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static IEnumerable<Assembly> SynentraAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Synentra.", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var name in KnownAssemblies)
        {
            if (loaded.Any(a => a.GetName().Name == name)) continue;

            try { loaded.Add(Assembly.Load(new AssemblyName(name))); }
            catch (FileNotFoundException) { /* not referenced by the test project — skip */ }
        }

        return loaded;
    }
}

internal sealed record DiagnosticDeclaration(Diagnostic Diagnostic, MemberInfo Member)
{
    public string MemberName =>
        $"{Member.DeclaringType?.FullName ?? "<unknown>"}.{Member.Name}";

    public string AssemblyName =>
        Member.DeclaringType?.Assembly.GetName().Name ?? "<unknown>";
}