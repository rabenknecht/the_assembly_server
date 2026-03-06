public class VoteOptionRecord
{
    public VoteOptionRecord(string? votingWhat, string[]? votedBy)
    {
        this.votedBy = votedBy;
        this.votingWhat = votingWhat;
    }


    public string? votingWhat { get; set; }
    public string[]? votedBy { get; set; }
}