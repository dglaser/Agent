//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using Agent;

namespace Agent
{
	[Serializable]
	[Flags]
	public enum ScheduleDaysOfWeek 
	{
		Monday		= 0x00000001,
		Tuesday		= 0x00000002,
		Wednesday	= 0x00000004,
		Thursday	= 0x00000008,
		Friday		= 0x00000010,
		Saturday	= 0x00000020,
		Sunday		= 0x00000040,
		Everyday = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
		Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
		Weekends = Saturday | Sunday
};

	[Serializable]
	public class FixedIntervalScheduler : AgentScheduler
	{
    private TimeSpan _rescheduleRequest = TimeSpan.Zero;

    public ScheduleDaysOfWeek DaysOfWeek = ScheduleDaysOfWeek.Everyday;
		public TimeSpan Frequency;
		public TimeSpan StartTime = new TimeSpan(0, 0, 0, 0, 0);
		public TimeSpan EndTime = new TimeSpan(0, 23, 59, 59, 999);

    public DateTime LastScheduled;

    public override bool CheckSchedule() 
    {
      bool scheduled = false;
      if ( CheckIfScheduled() )
      {
        LastScheduled = DateTime.Now;
        _rescheduleRequest = TimeSpan.Zero;
        scheduled = true;
        FireScheduled();
      }
      return scheduled;
    }

    public override void RequestRescheduling(TimeSpan when)
    {
      _rescheduleRequest = when;
    }

    private ScheduleDaysOfWeek ConvertDateTimeDayOfWeek(DateTime dateTime) 
		{
			switch(dateTime.DayOfWeek)
			{
				case DayOfWeek.Monday: return ScheduleDaysOfWeek.Monday;
				case DayOfWeek.Tuesday: return ScheduleDaysOfWeek.Tuesday;
				case DayOfWeek.Wednesday: return ScheduleDaysOfWeek.Wednesday;
				case DayOfWeek.Thursday: return ScheduleDaysOfWeek.Thursday;
				case DayOfWeek.Friday: return ScheduleDaysOfWeek.Friday;
				case DayOfWeek.Saturday: return ScheduleDaysOfWeek.Saturday;
				case DayOfWeek.Sunday: return ScheduleDaysOfWeek.Sunday;
			}
			throw(new ArgumentException("Invalid DayOfWeek", dateTime.DayOfWeek.ToString()));
		}

    private bool CheckIfScheduled() 
    {
      DateTime when = DateTime.Now;
      bool scheduled = false;
      if ( 
        //check Day Of Week is in schedule
        ((ConvertDateTimeDayOfWeek(when) & DaysOfWeek) != 0)
        //Check that Frequency has been set
        && (Frequency.Ticks > 0)
        //Check time is in range
        && (
        ((EndTime > StartTime) && (when.TimeOfDay >= StartTime) && (when.TimeOfDay <= EndTime))
        || ((EndTime < StartTime) && ((when.TimeOfDay >= StartTime) || (when.TimeOfDay <= EndTime)))
        )
        )
      {
        TimeSpan waitTime = Frequency;
        if ( (_rescheduleRequest != TimeSpan.Zero) && (_rescheduleRequest.Ticks < waitTime.Ticks) )
          waitTime = _rescheduleRequest;

        DateTime nextScheduled = LastScheduled.Add(waitTime);
        if ( nextScheduled <= when ) 
        {
          scheduled = true;
        }
      }

      return scheduled;
    }

	}//class
}
