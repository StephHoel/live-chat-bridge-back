using System;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Helpers;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Helpers;

public class OperationExecutorTests
{
    private static readonly NullLogger<OperationExecutorTests> _logger = new();

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult_WhenOperationSucceeds()
    {
        var result = await OperationExecutor.ExecuteAsync(
            _logger,
            "TestOperation",
            () => Task.FromResult(Result<string>.Ok("hello")));

        Assert.True(result.Success);
        Assert.Equal("hello", result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailResult_WhenOperationReturnsFailure()
    {
        var result = await OperationExecutor.ExecuteAsync(
            _logger,
            "TestOperation",
            () => Task.FromResult(Result<string>.Fail("business error", HttpStatusCode.BadRequest)));

        Assert.False(result.Success);
        Assert.Equal("business error", result.Error);
        Assert.Equal(HttpStatusCode.BadRequest, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsInternalServerError_WhenOperationThrows()
    {
        var result = await OperationExecutor.ExecuteAsync<string>(
            _logger,
            "TestOperation",
            () => throw new InvalidOperationException("boom"));

        Assert.False(result.Success);
        Assert.Equal("Erro inesperado", result.Error);
        Assert.Equal(HttpStatusCode.InternalServerError, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotThrow_WhenVoidOperationThrows()
    {
        // A exceção deve ser capturada internamente sem propagar
        var exception = await Record.ExceptionAsync(() =>
            OperationExecutor.ExecuteAsync(
                _logger,
                "VoidOperation",
                () => throw new InvalidOperationException("boom")));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ExecuteAsync_Void_CompletesSuccessfully_WhenOperationSucceeds()
    {
        var executed = false;

        var exception = await Record.ExceptionAsync(() =>
            OperationExecutor.ExecuteAsync(
                _logger,
                "VoidOperation",
                async () =>
                {
                    await Task.Yield();
                    executed = true;
                }));

        Assert.Null(exception);
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessFailure_DoesNotTransformToException()
    {
        // Garantia: Result.Fail retornado pela operação não deve ser transformado em exceção
        Result<int> captured = null;

        var result = await OperationExecutor.ExecuteAsync(
            _logger,
            "TestOperation",
            () =>
            {
                captured = Result<int>.Fail("not found", HttpStatusCode.NotFound);
                return Task.FromResult(captured);
            });

        Assert.NotNull(captured);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.ErrorType);
    }
}
