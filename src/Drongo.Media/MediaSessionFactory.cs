using Microsoft.Extensions.Logging;

namespace Drongo.Media;

public interface IMediaSessionFactory
{
    IMediaSession CreateSession(MediaSessionOptions options);
}

public sealed class AudioMediaSessionFactory : IMediaSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public AudioMediaSessionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IMediaSession CreateSession(MediaSessionOptions options)
    {
        var logger = _loggerFactory.CreateLogger<AudioMediaSession>();
        return new AudioMediaSession(options, logger);
    }
}
