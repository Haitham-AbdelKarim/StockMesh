namespace Domain.Enums;

public enum ReservationStatus
{
    Pending,
    Success,
    Cancelled,

    // Appended last on purpose: the status is stored as an int, so inserting
    // Accepted earlier would reinterpret existing Success/Cancelled rows.
    // Accepted = owner committed to ship; ledger + payment still outstanding.
    Accepted
}