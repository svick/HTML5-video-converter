using System;
using System.Linq;

namespace Video_converter
{
	static class TimeSpanExtension
	{
		class Unit
		{
			static readonly string oneUnitFormat = "{0} {1}";

			Func<TimeSpan, int> selector;
			string[] forms;

			public Unit(Func<TimeSpan, int> selector, string[] forms)
			{
				this.selector = selector;
				this.forms = forms;
			}

			public bool NotNull(TimeSpan timeSpan)
			{
				return selector(timeSpan) != 0;
			}

			public string Format(TimeSpan timeSpan)
			{
				return string.Format(oneUnitFormat, selector(timeSpan), pickForm(selector(timeSpan)));
			}

			string pickForm(int number)
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

		static readonly Unit[] units = new Unit[] {
			new Unit(ts => ts.Days, new string[] { "den", "dny", "dní" }),
			new Unit(ts => ts.Hours, new string[] { "hodina", "hodiny", "hodin" }),
			new Unit(ts => ts.Minutes, new string[] { "minuta", "minuty", "minut" }),
			new Unit(ts => ts.Seconds, new string[] { "sekunda", "sekundy", "sekund" })
		};

		public static string ToLongString(this TimeSpan timeSpan)
		{
			string result = null;
			foreach (Unit unit in units)
			{
				if (result != null)
				{
					result += " a " + unit.Format(timeSpan);
					break;
				}
				else
					if (unit.NotNull(timeSpan))
						result = unit.Format(timeSpan);
			}

			if (result == null)
				result = units.Last().Format(timeSpan);

			return result;
		}
	}
}