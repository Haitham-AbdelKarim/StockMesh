using Application.Common.Models;
using FluentAssertions;

namespace StockMesh.Application.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_WithNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Kind.Should().BeNull();
    }

    [Fact]
    public void NotFound_SetsFailureKind()
    {
        var result = Result.NotFound("missing");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("missing");
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public void Conflict_SetsFailureKind()
    {
        var result = Result.Conflict("exists");

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
    }

    [Fact]
    public void BadRequest_SetsFailureKind()
    {
        var result = Result.BadRequest("invalid");

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public void Unauthorized_SetsFailureKind()
    {
        var result = Result.Unauthorized("denied");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("denied");
        result.Kind.Should().Be(FailureKind.Unauthorized);
    }

    [Fact]
    public void Forbidden_SetsFailureKind()
    {
        var result = Result.Forbidden("denied");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("denied");
        result.Kind.Should().Be(FailureKind.Forbidden);
    }

    [Fact]
    public void Validation_SetsFailureKind_AndCarriesErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Email"] = ["Email is required."] };

        var result = Result<object>.Validation(errors);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Kind.Should().Be(FailureKind.Validation);
        result.ValidationErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void NonGenericValidation_CarriesErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = ["Name must not be empty."] };

        var result = Result.Validation(errors);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Validation);
        result.ValidationErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void From_CopiesStateOfAnotherResult()
    {
        var source = Result<int>.NotFound("missing");

        var result = Result.From(source);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("missing");
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ValueIsDefault_WithNullFailureKind()
    {
        var result = Result<int>.Failure("nope");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(0);
        result.Error.Should().Be("nope");
        result.Kind.Should().BeNull();
    }
}