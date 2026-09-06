using Api.Controllers;
using Application.Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StockMesh.Api.IntegrationTests;

public class ResultActionTests
{
    private sealed class TestController : ControllerBase;

    [Fact]
    public void Success_ReturnsValue_WithDefaultStatus()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Success("ok"));

        var objectResult = action.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().Be("ok");
    }

    [Fact]
    public void Success_WithCustomStatus_UsesIt()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Success("ok"), StatusCodes.Status201Created);

        var objectResult = action.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be("ok");
    }

    [Fact]
    public void NotFoundFailure_Returns404()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.NotFound("missing"));

        var result = action.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ConflictFailure_Returns409()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Conflict("exists"));

        var result = action.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void UnauthorizedFailure_Returns401()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Unauthorized("denied"));

        var result = action.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void ForbiddenFailure_Returns403()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Forbidden("denied"));

        var result = action.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void DefaultFailure_Returns400()
    {
        var controller = new TestController();

        var action = controller.FromResult(Result<string>.Failure("invalid"));

        var result = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ValidationFailure_Returns422_WithStructuredErrors()
    {
        var controller = new TestController();
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."],
            ["Password"] = ["Password must be at least 8 characters."]
        };

        var action = controller.FromResult(Result<string>.Validation(errors));

        var result = action.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = result.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("Email");
        problem.Errors.Should().ContainKey("Password");
    }
}