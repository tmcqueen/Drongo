using System.Buffers;
using System.Text;

namespace Drongo.Core.Parsing;

public interface ISipParser
{
    SipRequestParseResult ParseRequest(ReadOnlySequence<byte> data);
    SipResponseParseResult ParseResponse(ReadOnlySequence<byte> data);
}

public sealed class SipRequestParseResult
{
    public bool IsSuccess { get; }
    public Messages.SipRequest? Request { get; }
    public string? ErrorMessage { get; }
    public int? ErrorPosition { get; }

    private SipRequestParseResult(bool isSuccess, Messages.SipRequest? request, string? errorMessage, int? errorPosition)
    {
        IsSuccess = isSuccess;
        Request = request;
        ErrorMessage = errorMessage;
        ErrorPosition = errorPosition;
    }

    public static SipRequestParseResult Success(Messages.SipRequest request) =>
        new(true, request, null, null);

    public static SipRequestParseResult Failure(string message, int? position = null) =>
        new(false, null, message, position);
}

public sealed class SipResponseParseResult
{
    public bool IsSuccess { get; }
    public Messages.SipResponse? Response { get; }
    public string? ErrorMessage { get; }
    public int? ErrorPosition { get; }

    private SipResponseParseResult(bool isSuccess, Messages.SipResponse? response, string? errorMessage, int? errorPosition)
    {
        IsSuccess = isSuccess;
        Response = response;
        ErrorMessage = errorMessage;
        ErrorPosition = errorPosition;
    }

    public static SipResponseParseResult Success(Messages.SipResponse response) =>
        new(true, response, null, null);

    public static SipResponseParseResult Failure(string message, int? position = null) =>
        new(false, null, message, position);
}

public sealed class SipParser : ISipParser
{
    public SipRequestParseResult ParseRequest(ReadOnlySequence<byte> data)
    {
        try
        {
            var reader = new SequenceReader<byte>(data);

            if (!TryReadLine(ref reader, out var requestLine))
                return SipRequestParseResult.Failure("Empty request");

            var requestLineStr = Encoding.ASCII.GetString(requestLine);
            var requestParts = requestLineStr.Split(' ', 3);

            if (requestParts.Length != 3)
                return SipRequestParseResult.Failure($"Invalid request line: {requestLineStr}");

            var method = Messages.SipMethodExtensions.ParseMethod(requestParts[0]);
            var uri = Messages.SipUri.Parse(requestParts[1]);
            var sipVersion = requestParts[2];

            if (!sipVersion.StartsWith("SIP/"))
                return SipRequestParseResult.Failure($"Invalid SIP version: {sipVersion}");

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadOnlyMemory<byte> body = default;

            while (TryReadLine(ref reader, out var line))
            {
                if (line.IsEmpty)
                {
                    var remaining = data.Slice(reader.Position);
                    if (!remaining.IsEmpty)
                        body = remaining.ToArray();
                    break;
                }

                var headerStr = Encoding.ASCII.GetString(line);
                var colonIndex = headerStr.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var headerName = headerStr[..colonIndex].Trim();
                var headerValue = headerStr[(colonIndex + 1)..].Trim();
                headers[headerName] = headerValue;
            }

            return SipRequestParseResult.Success(new Messages.SipRequest(method, uri, sipVersion, headers, body));
        }
        catch (Messages.SipParseException ex)
        {
            return SipRequestParseResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return SipRequestParseResult.Failure($"Parse error: {ex.Message}");
        }
    }

    public SipResponseParseResult ParseResponse(ReadOnlySequence<byte> data)
    {
        try
        {
            var reader = new SequenceReader<byte>(data);

            if (!TryReadLine(ref reader, out var statusLine))
                return SipResponseParseResult.Failure("Empty response");

            var statusLineStr = Encoding.ASCII.GetString(statusLine);
            var statusParts = statusLineStr.Split(' ', 3);

            if (statusParts.Length < 2)
                return SipResponseParseResult.Failure($"Invalid status line: {statusLineStr}");

            if (!statusParts[0].StartsWith("SIP/"))
                return SipResponseParseResult.Failure($"Invalid SIP version: {statusParts[0]}");

            if (!int.TryParse(statusParts[1], out var statusCode))
                return SipResponseParseResult.Failure($"Invalid status code: {statusParts[1]}");

            var reasonPhrase = statusParts.Length > 2 ? statusParts[2] : string.Empty;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadOnlyMemory<byte> body = default;

            while (TryReadLine(ref reader, out var line))
            {
                if (line.IsEmpty)
                {
                    var remaining = data.Slice(reader.Position);
                    if (!remaining.IsEmpty)
                        body = remaining.ToArray();
                    break;
                }

                var headerStr = Encoding.ASCII.GetString(line);
                var colonIndex = headerStr.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var headerName = headerStr[..colonIndex].Trim();
                var headerValue = headerStr[(colonIndex + 1)..].Trim();
                headers[headerName] = headerValue;
            }

            return SipResponseParseResult.Success(new Messages.SipResponse(statusCode, reasonPhrase, statusParts[0], headers, body));
        }
        catch (Exception ex)
        {
            return SipResponseParseResult.Failure($"Parse error: {ex.Message}");
        }
    }

    private static bool TryReadLine(ref SequenceReader<byte> reader, out ReadOnlySequence<byte> line)
    {
        line = default;
        var start = reader.Position;

        while (reader.TryRead(out var b))
        {
            if (b == '\r')
            {
                if (reader.TryPeek(out var next) && next == '\n')
                {
                    reader.Advance(1);
                }
                break;
            }

            if (b == '\n')
                break;
        }

        if (reader.Position.Equals(start))
            return false;

        line = reader.Sequence.Slice(start, reader.Position);
        return true;
    }
}
