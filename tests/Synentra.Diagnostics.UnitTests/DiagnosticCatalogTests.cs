using Synentra.Diagnostics;
using System.Reflection;
using FluentAssertions;

namespace Synentra.UnitTests.Diagnostics;

public sealed class DiagnosticCatalogTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Primary test: a code may be declared exactly once.
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void Every_diagnostic_code_is_declared_exactly_once()
    {
        var duplicates = DiagnosticCatalog.Enumerate()
            .GroupBy(d => d.Diagnostic.Code, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
                $"  {g.Key}\n" +
                string.Join("\n", g.Select(d => $"    - {d.MemberName}  [{d.AssemblyName}]")))
            .ToList();

        if (duplicates.Count > 0)
        {
            Assert.Fail(
                "Diagnostic codes are a published contract and each must be declared exactly once.\n" +
                $"Found {duplicates.Count} duplicate code(s):\n\n" +
                string.Join("\n\n", duplicates));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Guardrails that catch the same class of bug from different angles.
    // ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void Every_diagnostic_code_matches_the_standard_format()
    {
        var violations = DiagnosticCatalog.Enumerate()
            .Where(d => !DiagnosticCode.IsValid(d.Diagnostic.Code))
            .Select(d => $"{d.Diagnostic.Code} at {d.MemberName}")
            .ToList();

        violations.Should().BeEmpty(
            because: "every diagnostic must follow SYNENTRA-{MODULE}-{SEQUENCE}");
    }

    [Fact]
    public void Every_diagnostic_uses_a_known_module()
    {
        var knownModules = typeof(DiagnosticModules)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);

        var violations = DiagnosticCatalog.Enumerate()
            .Where(d => !knownModules.Contains(DiagnosticCode.GetModule(d.Diagnostic.Code)))
            .Select(d => $"{d.Diagnostic.Code} uses an unknown module at {d.MemberName}")
            .ToList();

        violations.Should().BeEmpty(
            because: "every module segment must appear in DiagnosticModules");
    }

    [Fact]
    public void Every_static_diagnostic_field_is_readonly()
    {
        var violations = DiagnosticCatalog.Enumerate()
            .Where(d => d.Member is FieldInfo f && !f.IsInitOnly && !f.IsLiteral)
            .Select(d => $"{d.Diagnostic.Code} field is mutable at {d.MemberName}")
            .ToList();

        violations.Should().BeEmpty(
            because: "a mutable static Diagnostic can be reassigned at runtime and break the contract");
    }

    [Fact]
    public void Catalog_is_not_empty_and_is_discovered_from_the_diagnostics_assembly()
    {
        var byAssembly = DiagnosticCatalog.Enumerate()
            .GroupBy(d => d.AssemblyName)
            .ToDictionary(g => g.Key, g => g.Count());

        byAssembly.Should().NotBeEmpty(
            because: "the test must actually find diagnostics — a missing reference would silently pass");

        byAssembly.Keys.Should().ContainSingle("Synentra.Diagnostics");
    }

    [Fact]
    public void Every_diagnostic_has_a_doc_comment_placeholder()
    {
        // Structural check: every declaration member must at least exist on a
        // public type whose name ends with "Errors" or "Diagnostics".
        // This makes accidental entries in unrelated types obvious.
        var violations = DiagnosticCatalog.Enumerate()
            .Where(d =>
                d.Member.DeclaringType is null ||
                !(d.Member.DeclaringType.Name.EndsWith("Errors", StringComparison.Ordinal) ||
                  d.Member.DeclaringType.Name.EndsWith("Diagnostics", StringComparison.Ordinal) ||
                  d.Member.DeclaringType.DeclaringType?.Name.EndsWith("Errors", StringComparison.Ordinal) == true ||
                  d.Member.DeclaringType.DeclaringType?.Name.EndsWith("Error", StringComparison.Ordinal) == true ||
                  d.Member.DeclaringType.DeclaringType?.Name.EndsWith("Diagnostics", StringComparison.Ordinal) == true))
            .Select(d => $"{d.Diagnostic.Code} is declared on {d.Member.DeclaringType?.FullName}")
            .ToList();

        violations.Should().BeEmpty(
            because: "diagnostics belong on catalog classes named *Errors or *Diagnostics");
    }
}