using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using TapSystem.Shared.Models;
using TapSystem.Api.Services; 

namespace TapSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TapController : ControllerBase
{
    private readonly ITapMessageProcessor _processor;

    public TapController(ITapMessageProcessor processor)
    {
        _processor = processor;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SkipStatusCodePages]
    public ValueTask<IActionResult> PostAsync([FromBody] TapRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateTapRequest(request))
        {
            return new ValueTask<IActionResult>(BadRequest());
        }

        // Création directe de la struct - pas besoin de pool
        var message = new TapMessage(
            Guid.NewGuid(),
            request.CardId,
            request.StationId,
            request.TapType,
            request.Timestamp,
            request.Payload ?? ReadOnlyMemory<byte>.Empty
        );

        return ProcessMessageAsync(message, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<IActionResult> ProcessMessageAsync(TapMessage message, CancellationToken cancellationToken)
    {
        await _processor.ProcessAsync(message, cancellationToken);
        return Accepted();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateTapRequest(TapRequest request)
    {
        return !string.IsNullOrEmpty(request.CardId) &&
               !string.IsNullOrEmpty(request.StationId) &&
                request.Timestamp != default;
    }
}
