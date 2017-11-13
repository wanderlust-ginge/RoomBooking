﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Simplified.Ring3;

namespace RoomBooking
{

    [Database]
    public class Room
    {
        public string Name { get; set; }

        public string Description;
        public string TimeZoneId;
        public bool DaylightSavings;

        public TimeZoneInfo TimeZoneInfo {
            get {

                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneId);
                }
                catch (Exception)
                {
                    return TimeZoneInfo.Local;
                }
            }
        }

        public double TimeOffset {

            get {
                return TimeZoneInfo.GetUtcOffset(DateTime.UtcNow).TotalMinutes;

            }
        }
    }

  
}