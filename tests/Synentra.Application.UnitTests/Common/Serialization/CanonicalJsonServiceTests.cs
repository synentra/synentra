using System.Collections;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Synentra.Application.Common.Serialization;
using Xunit;

namespace Synentra.Application.Tests.Common.Serialization;

public sealed class CanonicalJsonServiceTests
{
    private readonly CanonicalJsonService _sut = new();

    [Fact]
    public void CanonicalizeArgumentDigest_WhenArgumentsAreEmpty_ReturnsValidDigest()
    {
        var args = new Dictionary<string, object>();

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().NotBeNullOrWhiteSpace();
        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenCalledWithSameArguments_ReturnsSameDigest()
    {
        var args = new Dictionary<string, object>
        {
            ["name"] = "product",
            ["quantity"] = 10
        };

        var first = _sut.CanonicalizeArgumentDigest(args);
        var second = _sut.CanonicalizeArgumentDigest(args);

        first.Should().Be(second);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenDictionaryOrderChanges_ReturnsSameDigest()
    {
        var firstArgs = new Dictionary<string, object>
        {
            ["name"] = "product",
            ["quantity"] = 10
        };

        var secondArgs = new Dictionary<string, object>
        {
            ["quantity"] = 10,
            ["name"] = "product"
        };

        var first = _sut.CanonicalizeArgumentDigest(firstArgs);
        var second = _sut.CanonicalizeArgumentDigest(secondArgs);

        first.Should().Be(second);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenArgumentValueChanges_ReturnsDifferentDigest()
    {
        var firstArgs = new Dictionary<string, object>
        {
            ["quantity"] = 10
        };

        var secondArgs = new Dictionary<string, object>
        {
            ["quantity"] = 20
        };

        var first = _sut.CanonicalizeArgumentDigest(firstArgs);
        var second = _sut.CanonicalizeArgumentDigest(secondArgs);

        first.Should().NotBe(second);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsNull_CanonicalizesNull()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = null!
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsString_CanonicalizesString()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = "hello"
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanonicalizeArgumentDigest_WhenValueIsBoolean_CanonicalizesBoolean(
        bool value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsInt_CanonicalizesInt(
        int value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsLong_CanonicalizesLong(
        long value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)1)]
    [InlineData((short)-1)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MinValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsShort_CanonicalizesShort(
        short value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData(byte.MaxValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsByte_CanonicalizesByte(
        byte value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.25)]
    [InlineData(-1.25)]
    public void CanonicalizeArgumentDigest_WhenValueIsDecimal_CanonicalizesDecimal(decimal value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1.5d)]
    [InlineData(-1.5d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsDouble_CanonicalizesDouble(
        double value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1.5f)]
    [InlineData(-1.5f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void CanonicalizeArgumentDigest_WhenValueIsFloat_CanonicalizesFloat(
        float value)
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = value
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsJsonElement_CanonicalizesJsonElement()
    {
        using var document = JsonDocument.Parse("""{"name":"product","quantity":10}""");

        var args = new Dictionary<string, object>
        {
            ["value"] = document.RootElement
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsDictionary_CanonicalizesDictionary()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["name"] = "product",
                ["quantity"] = 10
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenNestedDictionaryOrderChanges_ReturnsSameDigest()
    {
        var firstArgs = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["a"] = 1,
                ["b"] = 2
            }
        };

        var secondArgs = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["b"] = 2,
                ["a"] = 1
            }
        };

        var first = _sut.CanonicalizeArgumentDigest(firstArgs);
        var second = _sut.CanonicalizeArgumentDigest(secondArgs);

        first.Should().Be(second);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenDictionaryHasMultipleEntries_HandlesCommaSeparator()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsEnumerable_CanonicalizesEnumerable()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new List<object>
            {
                1,
                2,
                3
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenEnumerableIsEmpty_CanonicalizesEmptyEnumerable()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = Array.Empty<object>()
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenEnumerableContainsMultipleItems_HandlesCommaSeparator()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new List<object>
            {
                "first",
                "second",
                "third"
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenEnumerableContainsNestedValues_CanonicalizesNestedValues()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new List<object>
            {
                1,
                "two",
                true,
                null!,
                new Dictionary<string, object>
                {
                    ["nested"] = 3
                }
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsUnsupportedObject_UsesToString()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new UnsupportedValue("custom")
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsCustomEnumerable_CanonicalizesEnumerable()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new CustomEnumerable()
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsStringEnumerable_CanonicalizesEnumerable()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new[] { "a", "b", "c" }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenArgumentsContainMultipleValues_ReturnsDigest()
    {
        var args = new Dictionary<string, object>
        {
            ["null"] = null!,
            ["string"] = "text",
            ["bool"] = true,
            ["int"] = 10,
            ["long"] = 20L,
            ["short"] = (short)30,
            ["byte"] = (byte)40,
            ["decimal"] = 50.5m,
            ["double"] = 60.5d,
            ["float"] = 70.5f,
            ["list"] = new List<object> { 1, 2 },
            ["map"] = new Dictionary<string, object>
            {
                ["nested"] = "value"
            }
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenCultureChanges_ReturnsSameDigestForNumericValues()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            var args = new Dictionary<string, object>
            {
                ["decimal"] = 1234.56m,
                ["double"] = 1234.56d,
                ["float"] = 1234.56f,
                ["int"] = 1234
            };

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariantDigest = _sut.CanonicalizeArgumentDigest(args);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var frenchDigest = _sut.CanonicalizeArgumentDigest(args);

            frenchDigest.Should().Be(invariantDigest);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CanonicalizeArgumentDigest_WhenValueIsUnsupportedObject_UsesObjectStringRepresentation()
    {
        var args = new Dictionary<string, object>
        {
            ["value"] = new UnsupportedValue("custom")
        };

        var digest = _sut.CanonicalizeArgumentDigest(args);

        digest.Should().HaveLength(64);
    }

    private sealed class UnsupportedValue(string value)
    {
        public override string ToString() => value;
    }

    private sealed class CustomEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return 1;
            yield return 2;
        }
    }
}