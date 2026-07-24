namespace Qubix.Core.Authorization;

public static class ApplicationRoles
{
    public const string Participant = "Participant";

    public const string Organizer = "Organizer";

    public static readonly IReadOnlyCollection<string> All =
        [Participant, Organizer];
}
