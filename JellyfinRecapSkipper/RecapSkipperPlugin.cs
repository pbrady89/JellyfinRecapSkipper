using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;
using Microsoft.Extensions.Logging;

namespace JellyfinRecapSkipper
{
    public class RecapSegmentProvider : IMediaSegmentProvider
    {
        private readonly ILogger<RecapSegmentProvider> _logger;
        private readonly ILibraryManager _libraryManager;

        public RecapSegmentProvider(
            ILogger<RecapSegmentProvider> logger,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _logger.LogInformation("=== RECAP SKIPPER: RecapSegmentProvider constructed ===");
        }

        public string Name => "Recap Skipper";

        public ValueTask<bool> Supports(BaseItem item)
        {
            _logger.LogInformation("=== RECAP SKIPPER: Supports called for {Name} ({Type})", item.Name, item.GetType().Name);
            return ValueTask.FromResult(item is Episode);
        }

        public async Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
            MediaSegmentGenerationRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== RECAP SKIPPER: GetMediaSegments called for {Id}", request.ItemId);
            var segments = new List<MediaSegmentDto>();

            try
            {
                var item = _libraryManager.GetItemById(request.ItemId);

                if (item is not Episode episode)
                {
                    _logger.LogInformation("=== RECAP SKIPPER: Item is not an episode, skipping");
                    return segments;
                }

                var videoPath = episode.Path;

                if (string.IsNullOrEmpty(videoPath))
                    return segments;

                var jsonPath = Path.ChangeExtension(videoPath, null) + ".recap.json";
                _logger.LogInformation("=== RECAP SKIPPER: Looking for JSON at {Path}", jsonPath);

                if (!File.Exists(jsonPath))
                {
                    _logger.LogInformation("=== RECAP SKIPPER: No JSON found at {Path}", jsonPath);
                    return segments;
                }

                var json = await File.ReadAllTextAsync(jsonPath, cancellationToken);
                var data = JsonSerializer.Deserialize<RecapData>(json);

                if (data?.RecapEndSeconds == null)
                {
                    _logger.LogInformation("=== RECAP SKIPPER: No recap timestamp in JSON");
                    return segments;
                }

                var recapEndTicks = TimeSpan.FromSeconds(data.RecapEndSeconds.Value).Ticks;

                segments.Add(new MediaSegmentDto
                {
                    StartTicks = 0,
                    EndTicks = recapEndTicks,
                    Type = MediaSegmentType.Recap,
                    ItemId = episode.Id
                });

                _logger.LogInformation(
                    "=== RECAP SKIPPER: Recap segment added for {Name}: 0 to {End}s",
                    episode.Name,
                    data.RecapEndSeconds.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== RECAP SKIPPER: Error reading recap data");
            }

            return segments;
        }
    }

    public class RecapData
    {
        public string? Video { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("recap_end_seconds")]
        public double? RecapEndSeconds { get; set; }

        public string? GeneratedAt { get; set; }
    }
}