namespace AirSecurityDroneControl.BuildingBlocks.Contracts;

public enum SensorType
{
    Camera,
    Acoustic,
    Rf,
    Radar,
    ExternalApi
}

public enum ClassificationType
{
    Drone,
    Bird,
    Helicopter,
    RandomNoise,
    Interference,
    Unknown
}

public enum ThreatLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    Open,
    UnderInvestigation,
    Resolved,
    Dismissed
}

public record GeoPoint(double Latitude, double Longitude, double? AltitudeMeters = null);

public record DetectionEvent(
    Guid DetectionId,
    string SensorNodeId,
    SensorType SensorType,
    ClassificationType Classification,
    GeoPoint Position,
    DateTimeOffset TimestampUtc,
    double Confidence,
    double? HeadingDegrees = null,
    double? SpeedMps = null);

public record FusedTrack(
    Guid TrackId,
    IReadOnlyCollection<Guid> DetectionIds,
    GeoPoint EstimatedPosition,
    double EstimatedSpeedMps,
    double EstimatedHeadingDegrees,
    double Confidence,
    DateTimeOffset LastUpdateUtc);

public record ProtectedZone(
    Guid ZoneId,
    string Name,
    GeoPoint Center,
    double RadiusMeters,
    bool Sensitive = false);

public record ThreatAssessment(
    Guid AssessmentId,
    Guid TrackId,
    double Score,
    ThreatLevel Level,
    string Summary,
    DateTimeOffset TimestampUtc);

public record IncidentCase(
    Guid IncidentId,
    Guid TrackId,
    Guid ThreatAssessmentId,
    IncidentStatus Status,
    string Zone,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc = null);
