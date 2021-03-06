﻿//      )                             *     
//   ( /(        *   )       (      (  `    
//   )\()) (   ` )  /( (     )\     )\))(   
//  ((_)\  )\   ( )(_)))\ ((((_)(  ((_)()\  
// __ ((_)((_) (_(_())((_) )\ _ )\ (_()((_) 
// \ \ / / (_) |_   _|| __|(_)_\(_)|  \/  | 
//  \ V /  | | _ | |  | _|  / _ \  | |\/| | 
//   |_|   |_|(_)|_|  |___|/_/ \_\ |_|  |_| 
// 
// This file is subject to the terms and conditions defined in
// file 'License.txt', which is part of this source code package.
// 
// Copyright © Yi.TEAM. All rights reserved.
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Yisoft.Crontab.Extensions;

namespace Yisoft.Crontab.Filters
{
	/// <summary>
	/// Handles step values (i.e. */5, 2/7)
	/// <remarks>
	/// For example, */5 in the minutes field indicates every 5 minutes
	/// </remarks>
	/// </summary>
	public class StepFilter : ICronFilter, ITimeFilter
	{
		private int? _firstCache;

		/// <summary>
		/// Constructs a new RangeFilter instance
		/// </summary>
		/// <param name="start">The start of the range</param>
		/// <param name="step">step</param>
		/// <param name="kind">The crontab field kind to associate with this filter</param>
		public StepFilter(int start, int step, CrontabFieldKind kind)
		{
			var maxValue = Constants.MaximumDateTimeValues[kind];

			if (step <= 0 || step > maxValue) throw new CrontabException($"Steps = {step} is out of bounds for <{Enum.GetName(typeof(CrontabFieldKind), kind)}> field");

			Start = start;
			Step = step;
			Kind = kind;

			var filters = new List<SpecificFilter>();

			for (var evalValue = Start; evalValue <= maxValue; evalValue++) if (_IsMatch(evalValue)) filters.Add(new SpecificFilter(evalValue, Kind));

			SpecificFilters = filters;
		}

		public int Start { get; }

		public int Step { get; }

		/// <summary>
		/// Returns a list of specific filters that represents this step filter
		/// </summary>
		public IEnumerable<SpecificFilter> SpecificFilters { get; }

		public CrontabFieldKind Kind { get; }

		/// <summary>
		/// Checks if the value is accepted by the filter
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <returns>True if the value matches the condition, False if it does not match.</returns>
		public bool IsMatch(DateTime value)
		{
			int evalValue;

			switch (Kind)
			{
				case CrontabFieldKind.Second:
					evalValue = value.Second;
					break;
				case CrontabFieldKind.Minute:
					evalValue = value.Minute;
					break;
				case CrontabFieldKind.Hour:
					evalValue = value.Hour;
					break;
				case CrontabFieldKind.Day:
					evalValue = value.Day;
					break;
				case CrontabFieldKind.Month:
					evalValue = value.Month;
					break;
				case CrontabFieldKind.DayOfWeek:
					evalValue = value.DayOfWeek.ToCronDayOfWeek();
					break;
				case CrontabFieldKind.Year:
					evalValue = value.Year;
					break;
				default: throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
			}

			return _IsMatch(evalValue);
		}

		public int? Next(int value)
		{
			if (Kind == CrontabFieldKind.Day
			    || Kind == CrontabFieldKind.Month
			    || Kind == CrontabFieldKind.DayOfWeek) throw new CrontabException("Cannot call Next for Day, Month or DayOfWeek types");

			var max = Constants.MaximumDateTimeValues[Kind];

			var newValue = (int?) value + 1;

			while (newValue < max && !_IsMatch(newValue.Value)) newValue++;

			if (newValue >= max) newValue = null;

			return newValue;
		}

		public int First()
		{
			if (_firstCache.HasValue) return _firstCache.Value;

			if (Kind == CrontabFieldKind.Day
			    || Kind == CrontabFieldKind.Month
			    || Kind == CrontabFieldKind.DayOfWeek) throw new CrontabException("Cannot call First for Day, Month or DayOfWeek types");

			var max = Constants.MaximumDateTimeValues[Kind];

			var newValue = 0;

			while (newValue < max && !_IsMatch(newValue)) newValue++;

			if (newValue > max) throw new CrontabException($"Next value for {ToString()} on field {Enum.GetName(typeof(CrontabFieldKind), Kind)} could not be found!");

			_firstCache = newValue;

			return newValue;
		}

		private bool _IsMatch(int evalValue) { return evalValue >= Start && (evalValue - Start) % Step == 0; }

		public override string ToString() { return $"{(Start == 0 ? "*" : Start.ToString())}/{Step}"; }
	}
}
