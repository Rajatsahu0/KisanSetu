using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.VoiceIntelligence.Models;

/// <summary>
/// Represents a voice query history item stored in Firestore
/// </summary>
[FirestoreData]
public record VoiceQueryHistoryItem
{
    [FirestoreProperty] public string QueryId { get; init; } = string.Empty;
    [FirestoreProperty] public string FarmerId { get; init; } = string.Empty;
    [FirestoreProperty] public string Transcription { get; init; } = string.Empty;
    [FirestoreProperty] public string ResponseText { get; init; } = string.Empty;
    [FirestoreProperty] public string Dialect { get; init; } = string.Empty;
    [FirestoreProperty] public double Confidence { get; init; }
    [FirestoreProperty] public string AudioS3Key { get; init; } = string.Empty;
    [FirestoreProperty] public string ResponseAudioS3Key { get; init; } = string.Empty;
    [FirestoreProperty] public DateTime Timestamp { get; init; }
    [FirestoreProperty] public bool IsFavorite { get; init; }
    // Note: MandiPrice list needs to be Firestore-compatible too, or stored as JSON string
    [FirestoreProperty] public string PricesJson { get; init; } = "[]";
}
