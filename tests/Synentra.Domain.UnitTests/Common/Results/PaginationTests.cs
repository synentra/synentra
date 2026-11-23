using FluentAssertions;
using Synentra.Diagnostics;
using Synentra.Domain.Common.Results;

namespace Synentra.Domain.Tests;

public sealed class PaginationTests
{
    [Fact]
    public void PageRequest_ShouldApplyDefaultsAndClampInputs()
    {
        new PageRequest().Should().Be(new PageRequest(1, 20));
        new PageRequest(null, null).Should().Be(new PageRequest(1, 20));
        new PageRequest(0, -1).Should().Be(new PageRequest(1, 20));
        new PageRequest(-4, 0).Should().Be(new PageRequest(1, 20));
        new PageRequest(3, 1).Should().Be(new PageRequest(3, 1));
        new PageRequest(3, 500).Should().Be(new PageRequest(3, 200));
        PageRequest.DefaultPageSize.Should().Be(20);
        PageRequest.MaxPageSize.Should().Be(200);
    }

    [Fact]
    public void Page_ShouldExposeItemsAndTotalCount()
    {
        var page = new Page<string>(["a", "b"], 9);
        page.Items.Should().Equal("a", "b");
        page.TotalCount.Should().Be(9);
    }

    [Fact]
    public void PaginatedResult_Success_ShouldExposePagingMetadata()
    {
        var request = new PageRequest(2, 2);
        var result = PaginatedResult<string>.Success(["c", "d"], request, 5);

        result.IsSuccess.Should().BeTrue();
        result.Diagnostic.Should().BeNull();
        result.Items.Should().Equal("c", "d");
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResult_ShouldHandleFirstLastAndEmptyPages()
    {
        var first = PaginatedResult<int>.Success([1], new PageRequest(1, 2), 4);
        first.HasPreviousPage.Should().BeFalse();
        first.HasNextPage.Should().BeTrue();

        var last = PaginatedResult<int>.Success([3, 4], new PageRequest(2, 2), 4);
        last.HasPreviousPage.Should().BeTrue();
        last.HasNextPage.Should().BeFalse();

        var empty = PaginatedResult<int>.Empty(new PageRequest(1, 10));
        empty.IsSuccess.Should().BeTrue();
        empty.Items.Should().BeEmpty();
        empty.IsEmpty.Should().BeTrue();
        empty.TotalCount.Should().Be(0);
        empty.TotalPages.Should().Be(0);
        empty.HasPreviousPage.Should().BeFalse();
        empty.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResult_Failure_ShouldExposeDiagnosticAndEmptyItems()
    {
        var diagnostic = Diagnostic.Define("TST", 2, "Not found", DiagnosticCategory.NotFound);
        var result = PaginatedResult<int>.Failure(diagnostic);
        result.IsSuccess.Should().BeFalse();
        result.Diagnostic.Should().Be(diagnostic);
        result.Items.Should().BeEmpty();
        result.IsEmpty.Should().BeTrue();
        result.PageNumber.Should().Be(0);
        result.PageSize.Should().Be(0);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task PaginatedResult_ShouldSupportAsyncFactories()
    {
        var request = new PageRequest(1, 5);
        var success = await PaginatedResult<int>.SuccessAsync([1, 2], request, 2);
        var failureDiagnostic = Diagnostic.Define("TST", 3, "Conflict", DiagnosticCategory.Conflict);
        var failure = await PaginatedResult<int>.FailureAsync(failureDiagnostic);
        var empty = await PaginatedResult<int>.EmptyAsync(request);

        success.Items.Should().Equal(1, 2);
        failure.IsSuccess.Should().BeFalse();
        failure.Diagnostic.Should().Be(failureDiagnostic);
        empty.IsSuccess.Should().BeTrue();
        empty.IsEmpty.Should().BeTrue();
    }
}
