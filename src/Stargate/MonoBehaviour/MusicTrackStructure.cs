using System.Text.Json.Serialization;

namespace MonoBehaviour;

// TODO: CHANGE THE PROPETY NAMES???
public record CommentContainer(
    TrackComment Comment);

public record TrackComment(
    double? marker,
    string? commentType,
    string? comment);

public record TrackMarker(
    long? VAL);

public record MusicSection(
    int? sectionType,
    long? marker,
    string? comment);

public record MusicSignature(
    int? beats,
    double? marker,
    string? comment);

public record MusicTrackStructure(
    [property: JsonPropertyName("startBeat")] int? StartBeat,
    [property: JsonPropertyName("endBeat")] int? EndBeat,
    [property: JsonPropertyName("videoStartTime")] double? VideoStartTime,
    [property: JsonPropertyName("previewEntry")] double? PreviewEntry,
    [property: JsonPropertyName("previewLoopStart")] double? PreviewLoopStart,
    [property: JsonPropertyName("previewLoopEnd")] double? PreviewLoopEnd,
    [property: JsonPropertyName("previewDuration")] double? PreviewDuration,
    [property: JsonPropertyName("signatures")] SignatureContainer[] Signatures,
    [property: JsonPropertyName("markers")] TrackMarker[] Markers,
    [property: JsonPropertyName("sections")] SectionContainer[] Sections,
    [property: JsonPropertyName("comments")] CommentContainer[] Comments);

public record SectionContainer(
    MusicSection MusicSection);

public record SignatureContainer(
    MusicSignature MusicSignature);