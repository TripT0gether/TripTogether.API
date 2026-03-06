namespace TripTogether.Application.Helpers;

public static class TimeSlotHelper
{
    public static void ValidateTimeLogic(TimeOnly? startTime, TimeOnly? endTime)
    {
        if (startTime.HasValue && endTime.HasValue)
        {
            if (endTime.Value <= startTime.Value)
            {
                throw ErrorHelper.BadRequest("EndTime must be after StartTime.");
            }
        }
    }
}
