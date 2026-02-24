using TripTogether.Domain.Enums;

namespace TripTogether.Application.Helpers;

public static class TimeSlotHelper
{
    public static void ValidateTimeLogic(TimeOnly? startTime, TimeOnly? endTime, TimeSlot? scheduleSlot)
    {
        if (startTime.HasValue && endTime.HasValue)
        {
            if (endTime.Value <= startTime.Value)
            {
                throw ErrorHelper.BadRequest("EndTime must be after StartTime.");
            }
        }

        if (scheduleSlot.HasValue && startTime.HasValue)
        {
            var expectedSlot = GetTimeSlotFromTime(startTime.Value);
            if (expectedSlot != scheduleSlot.Value)
            {
                var slotRange = GetTimeSlotRange(scheduleSlot.Value);
                throw ErrorHelper.BadRequest(
                    $"StartTime {startTime.Value:HH:mm} doesn't match ScheduleSlot {scheduleSlot.Value}. " +
                    $"Expected time range: {slotRange}");
            }
        }
    }

    public static TimeSlot GetTimeSlotFromTime(TimeOnly time)
    {
        var hour = time.Hour;

        return hour switch
        {
            >= 6 and < 11 => TimeSlot.Morning,
            >= 11 and < 13 => TimeSlot.Lunch,
            >= 13 and < 17 => TimeSlot.Afternoon,
            >= 17 and < 19 => TimeSlot.Dinner,
            >= 19 and < 23 => TimeSlot.Evening,
            _ => TimeSlot.LateNight
        };
    }

    public static string GetTimeSlotRange(TimeSlot slot)
    {
        return slot switch
        {
            TimeSlot.Morning => "06:00 - 10:59",
            TimeSlot.Lunch => "11:00 - 12:59",
            TimeSlot.Afternoon => "13:00 - 16:59",
            TimeSlot.Dinner => "17:00 - 18:59",
            TimeSlot.Evening => "19:00 - 22:59",
            TimeSlot.LateNight => "23:00 - 05:59",
            _ => "Unknown"
        };
    }
}
