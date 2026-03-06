namespace TripTogether.Application.DTOs.PollDTO;

public class FinalizePollDto
{
    public Guid PollId { get; set; }
    public Guid SelectedOptionId { get; set; }
}
