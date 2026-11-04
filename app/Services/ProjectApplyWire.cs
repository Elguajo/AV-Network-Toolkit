using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NetworkProfileManager.Models;

namespace NetworkProfileManager.Services;

internal sealed class ApplyRequestExpiredException() : Exception("Confirmation expired.");

internal static class ProjectApplyWire
{
    internal const int MaxBytes = 256 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16 };
    internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    internal static T Parse<T>(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Message too large.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        RejectDuplicates(document.RootElement);
        return JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Empty message.");
    }
    private static void RejectDuplicates(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in node.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate property.");
                RejectDuplicates(property.Value);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) RejectDuplicates(item);
    }
    internal static void ValidateRequest(ProjectApplyRequest request, DateTimeOffset now)
    {
        if (request == null || request.Version != 1 || request.OperationId == Guid.Empty
            || request.ExpiresAt > now.AddMinutes(2)) throw new InvalidDataException("Invalid request.");
        ProjectApplyPolicy.ValidateTransition(request.Before, request.Desired, request.Recovery);
        if (request.ExpiresAt <= now) throw new ApplyRequestExpiredException();
    }
    internal static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellation)
    {
        var bytes = Encoding.UTF8.GetBytes(Serialize(value));
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Message too large.");
        await stream.WriteAsync(BitConverter.GetBytes(bytes.Length), cancellation);
        await stream.WriteAsync(bytes, cancellation);
        await stream.FlushAsync(cancellation);
    }
    internal static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellation)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellation);
        var size = BitConverter.ToInt32(header);
        if (size <= 0 || size > MaxBytes) throw new InvalidDataException("Invalid message length.");
        var bytes = new byte[size];
        await stream.ReadExactlyAsync(bytes, cancellation);
        return Parse<T>(new UTF8Encoding(false, true).GetString(bytes));
    }
}
