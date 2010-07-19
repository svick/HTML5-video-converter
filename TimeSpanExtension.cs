using System;

namespace Video_converter
{
	static class TimeSpanExtension
	{
		static readonly string[] secondForms = new string[] { "sekunda", "sekundy", "sekund" };
		static readonly string[] minuteForms = new string[] { "minuta", "minuty", "minut" };
		static readonly string[] hourForms = new string[] { "hodina", "hodiny", "hodin" };
		static readonly string[] dayForms = new string[] { "den", "dny", "dní" };

		static readonly string oneUnitFormat = "{0} {1}";

		public static string ToLongString(this TimeSpan timeSpan)
		{
			if (timeSpan.Days != 0)
				return DayString(timeSpan) + " a " + HourString(timeSpan);
			else if (timeSpan.Hours != 0)
				return HourString(timeSpan) + " a " + MinuteString(timeSpan);
			else if (timeSpan.Minutes != 0)
				return MinuteString(timeSpan) + " a " + SecondString(timeSpan);
			else
				return SecondString(timeSpan);
		}

		public static string DayString(TimeSpan timeSpan)
		{
			return string.Format(oneUnitFormat, timeSpan.Days, pickForm(timeSpan.Days, dayForms));
		}

		public static string HourString(TimeSpan timeSpan)
		{
			return string.Format(oneUnitFormat, timeSpan.Hours, pickForm(timeSpan.Hours, hourForms));
		}

		public static string MinuteString(TimeSpan timeSpan)
		{
			return string.Format(oneUnitFormat, timeSpan.Minutes, pickForm(timeSpan.Minutes, minuteForms));
		}

		public static string SecondString(TimeSpan timeSpan)
		{
			return string.Format(oneUnitFormat, timeSpan.Seconds, pickForm(timeSpan.Seconds, secondForms));
		}

		static string pickForm(int number, string[] forms)
		{
			number = Math.Abs(number);
			if (number == 1)
				return forms[0];
			else if (number <= 4)
				return forms[1];
			else
				return forms[2];
		}
	}
}