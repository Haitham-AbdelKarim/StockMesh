using Application.Common.Models;
using Application.DTOs.Expenses;
using Application.Features.Expenses.Commands.RecordExpense;
using Application.Features.Expenses.Queries.GetExpenses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/")]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExpensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<ExpenseResponse>> RecordExpense(
        [FromBody] RecordExpenseCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<PaginatedList<ExpenseResponse>>> GetExpenses(
        [FromQuery] GetExpensesQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }
}