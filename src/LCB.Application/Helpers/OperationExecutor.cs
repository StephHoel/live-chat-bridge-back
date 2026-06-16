using System.Diagnostics;
using System.Net;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Helpers;

/// <summary>
/// Centraliza o padrão de execução com logging estruturado, tratamento de erros e medição de tempo.
/// </summary>
public static class OperationExecutor
{
    /// <summary>
    /// Executa uma operação que retorna <see cref="Result{T}"/>, envolvendo-a com
    /// logs de início/fim, tempo de execução e captura de exceções inesperadas.
    /// </summary>
    public static async Task<Result<T>> ExecuteAsync<T>(
        ILogger logger,
        string operationName,
        Func<Task<Result<T>>> operation)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Starting {OperationName}", operationName);

        try
        {
            var result = await operation();
            sw.Stop();

            if (result.Success)
                logger.LogInformation(
                    "Completed {OperationName} with success in {ElapsedMs}ms",
                    operationName, sw.ElapsedMilliseconds);
            else
                logger.LogWarning(
                    "Completed {OperationName} with failure in {ElapsedMs}ms: {Error}",
                    operationName, sw.ElapsedMilliseconds, result.Error);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "Unexpected error in {OperationName} after {ElapsedMs}ms",
                operationName, sw.ElapsedMilliseconds);

            return Result<T>.Fail("Erro inesperado", HttpStatusCode.InternalServerError);
        }
        finally
        {
            logger.LogInformation("Finishing {OperationName}", operationName);
        }
    }

    /// <summary>
    /// Executa uma operação sem retorno de resultado, envolvendo-a com
    /// logs de início/fim, tempo de execução e captura de exceções inesperadas.
    /// </summary>
    public static async Task ExecuteAsync(
        ILogger logger,
        string operationName,
        Func<Task> operation)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Starting {OperationName}", operationName);

        try
        {
            await operation();
            sw.Stop();
            logger.LogInformation(
                "Completed {OperationName} in {ElapsedMs}ms",
                operationName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "Unexpected error in {OperationName} after {ElapsedMs}ms",
                operationName, sw.ElapsedMilliseconds);
        }
        finally
        {
            logger.LogInformation("Finishing {OperationName}", operationName);
        }
    }
}
