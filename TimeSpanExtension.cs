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

		public static string[] getLocalizedForms(string unitName)
		{
			return new string[]
			{
				App.GetLocalizedString(unitName + "1"),
				App.GetLocalizedString(unitName + "24"),
				App.GetLocalizedString(unitName + "5")
			};
		}

		static readonly Unit[] units = new Unit[] {
			new Unit(ts => ts.Days, getLocalizedForms("Day")),
			new Unit(ts => ts.Hours, getLocalizedForms("Hour")),
			new Unit(ts => ts.Minutes, getLocalizedForms("Minute")),
			new Unit(ts => ts.Seconds, getLocalizedForms("Second"))
		};

		public static string ToLongString(this TimeSpan timeSpan)
		{
			string result = null;
			foreach (Unit unit in units)
			{
				if (result != null)
				{
					result += " " + App.GetLocalizedString("And") + " " + unit.Format(timeSpan);
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