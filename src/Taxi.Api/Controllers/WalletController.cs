using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Commands.CreateWalletTopUp;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Application.Features.Wallet.Queries.GetWalletBalance;
using Taxi.Application.Features.Wallet.Queries.GetWalletTransactions;
using Taxi.Contracts.Requests.Wallet;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/wallet")]
public class WalletController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(WalletBalanceDto), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the current passenger's wallet (ride balance / Fat7i Saldo).")]
    [EndpointName("GetWalletBalance")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var result = await sender.Send(new GetWalletBalanceQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(PagedResult<WalletTransactionDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the current passenger's wallet transaction history, newest first.")]
    [EndpointName("GetWalletTransactions")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetWalletTransactionsQuery(page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("top-ups")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(WalletTopUpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Starts a Stripe-funded top-up of the wallet balance.")]
    [EndpointDescription("Creates a Stripe PaymentIntent and a pending wallet ledger entry. The balance is credited only after the payment_intent.succeeded webhook, never here.")]
    [EndpointName("CreateWalletTopUp")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CreateTopUp([FromBody] CreateWalletTopUpRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateWalletTopUpCommand(request.Amount), ct);
        return result.Match(Ok, Problem);
    }
}
