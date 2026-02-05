namespace TripTogether.Application.DTOs.FriendshipDTO;

public class SearchUsersDto
{
    public string SearchTerm { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
