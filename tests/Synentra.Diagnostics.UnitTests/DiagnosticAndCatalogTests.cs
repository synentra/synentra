using System.Reflection;
using Synentra.Diagnostics.Catalog;
using FluentAssertions;

namespace Synentra.Diagnostics.Tests;

public class DiagnosticAndCatalogTests
{
    [Fact]
    public void Define_creates_diagnostic_with_expected_values()
    {
        var diagnostic = Diagnostic.Define("AUTH", 12, "Test message", DiagnosticCategory.Forbidden);
        diagnostic.Code.Should().Be("SYNENTRA-AUTH-0012");
        diagnostic.Message.Should().Be("Test message");
        diagnostic.Category.Should().Be(DiagnosticCategory.Forbidden);
        diagnostic.Sequence.Should().Be(12);
    }

    [Theory]
    [InlineData(null)][InlineData("")][InlineData(" ")]
    public void Define_rejects_blank_message(string? message)
        => FluentActions.Invoking(() => Diagnostic.Define("AUTH", 1, message!)).Should().Throw<ArgumentException>();

    [Fact]
    public void ToString_formats_category_code_and_message()
    {
        var diagnostic = Diagnostic.Define("AUTH", 1, "Denied", DiagnosticCategory.Forbidden);
        diagnostic.ToString().Should().Be("forbidden SYNENTRA-AUTH-0001: Denied");
    }

    [Fact]
    public void Every_catalog_diagnostic_is_well_formed_and_declared_once()
    {
        var declarations = GetCatalogDiagnostics();
        var diagnostics = declarations.Select(d => d.Diagnostic).ToArray();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().OnlyContain(d => DiagnosticCode.IsValid(d.Code));
        diagnostics.Should().OnlyContain(d => d.Message != null && d.Message.Length > 0);
        diagnostics.Should().OnlyContain(d => Enum.IsDefined(d.Category));

        var duplicates = declarations.GroupBy(d => d.Diagnostic.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(d => d.MemberName))}")
            .ToArray();

        duplicates.Should().BeEmpty(
            because: "diagnostic codes are published identifiers and must be declared exactly once");
    }

    [Fact]
    public void Catalog_parameterized_methods_return_valid_diagnostics()
    {
        foreach (var declaration in GetCatalogDiagnostics().Where(d => d.Member is MethodInfo))
        {
            declaration.Diagnostic.Code.Should().MatchRegex($"^SYNENTRA-[A-Z0-9]+-[0-9]{{4}}$", declaration.MemberName);
            declaration.Diagnostic.Message.Should().NotBeNullOrWhiteSpace(declaration.MemberName);
        }
    }

    private static IReadOnlyList<DiagnosticDeclaration> GetCatalogDiagnostics()
    {
        var catalogTypes = typeof(ApprovalDiagnostics).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(ApprovalDiagnostics).Namespace && t.IsClass && t.IsAbstract && t.IsSealed);
        var declarations = new List<DiagnosticDeclaration>();

        foreach (var type in catalogTypes)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
                         .Where(f => f.FieldType == typeof(Diagnostic)))
            {
                if (field.GetValue(null) is Diagnostic diagnostic)
                    declarations.Add(new(diagnostic, field));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .Where(m => m.ReturnType == typeof(Diagnostic) && m.GetParameters().Length > 0))
            {
                var args = method.GetParameters()
                    .Select(p => (object)(p.Name?.Contains("version", StringComparison.OrdinalIgnoreCase) == true ? "v1" : "sample"))
                    .ToArray();
                if (method.Invoke(null, args) is Diagnostic diagnostic)
                    declarations.Add(new(diagnostic, method));
            }
        }

        return declarations;
    }

    private sealed record DiagnosticDeclaration(Diagnostic Diagnostic, MemberInfo Member)
    {
        public string MemberName => $"{Member.DeclaringType?.FullName}.{Member.Name}";
    }

    [Fact]
    public void Diagnostic_modules_constants_are_valid_module_identifiers()
    {
        typeof(DiagnosticModules).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Should().OnlyContain(module => DiagnosticCode.IsValidModule(module));
    }
}
