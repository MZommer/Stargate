using System.Text.Json.Serialization;

namespace MonoBehaviour;

public record KaraokeClipContainer(
    KaraokeClip KaraokeClip);

public record DanceTapeData(
    int? TapeClock,
    int? TapeBarCount,
    byte? FreeResourcesAfterPlay,
    string MapName,
    string SoundwichEvent,
    MotionClipData[] MotionClips,
    PictogramClipData[] PictoClips,
    GoldEffectClipData[] GoldEffectClips,
    HideHudClipData[] HideHudClips);

public record HideHudClipData(
    int? StartTime,
    int? Duration,
    byte? IsActive);
public record GoldEffectClipData(
    int? StartTime,
    int? Duration,
    int? GoldEffectType,
    long? Id,
    long? TrackId,
    byte? IsActive);

public record MoveModels(
    [property: JsonPropertyName("list")] UAFList[] List,
    [property: JsonPropertyName("keyCollision")] int? KeyCollision);

public record CoachData(
    uint? GoldMovesCount,
    uint? StandardMovesCount);

public record KaraokeClip(
    int? StartTime,
    int? Duration,
    string Lyrics,
    byte? IsActive,
    long? TrackId,
    float? Pitch,
    byte? IsEndOfLine,
    int? ContentType,
    long? Id,
    int? SemitoneTolerance,
    int? StartTimeTolerance,
    int? EndTimeTolerance);

public record KaraokeData(
    int? TapeClock,
    TapeTrackContainer[] Tracks,
    KaraokeClipContainer[] Clips,
    int? TapeBarCount,
    string SoundwichEvent,
    string MapName,
    byte? FreeResourcesAfterPlay);

public record UAFList(
    string Key,
    Value Value);

public record MotionClipData(
    int? StartTime,
    int? Duration,
    long? Id,
    long? TrackId,
    byte? IsActive,
    string MoveName,
    byte? GoldMove,
    int? CoachId,
    int? MoveType,
    string? Color);

public record PPtr(
    [property: JsonPropertyName("m_FileID")] int? FileID,
    [property: JsonPropertyName("m_PathID")] long? PathID);

public record PictogramClipData(
    int? StartTime,
    int? Duration,
    long? Id,
    long? TrackId,
    byte? IsActive,
    string PictoPath,
    uint? CoachCount);

public record Resource(
    PPtr MoveAsset,
    TuningValues TuningValues);

public record SongData(
    string MapName,
    SongDesc SongDesc,
    KaraokeData KaraokeData,
    DanceTapeData DanceData,
    PPtr TrackData,
    MoveModels CameraMoveModels,
    MoveModels HandDeviceMoveModels,
    PPtr PictogramAtlas,
    CoachData[] FullBodyCoachDatas,
    CoachData[] HandOnlyCoachDatas);

public record SongDesc(
    string MapName,
    int? JDVersion,
    int? OriginalJDVersion,
    string Artist,
    string DancerName,
    string Title,
    string Credits,
    int? NumCoach,
    int? MainCoach,
    int? Difficulty,
    int? SweatDifficulty);

public record TapeTrack(
    long? Id,
    string Name
);

public record TapeTrackContainer(
    TapeTrack TapeTrack);

public record TuningValues(
    double? ScoreScale,
    double? ScoreSmoothing);

public record Value(
    Resource[] Resources,
    [property: JsonPropertyName("m_FileID")] int? FileID,
    [property: JsonPropertyName("m_PathID")] long? PathID);
