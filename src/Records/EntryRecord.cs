public record class EntryRecord
(
    string? question,
    DateTimeOffset startsWhen,
    DateTimeOffset endsWhen,
    VoteOptionRecord[]? voteOptions
);