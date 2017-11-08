using Starcounter;
using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Templates;
using System.Threading;

namespace RoomBooking.ViewModels.Screens
{
    partial class ScreenContentPage : Json, IBound<RoomScreenRelation>
    {

        public void OnActiveEvent()
        {
            this.ContentPartial = GetDefaultPage();
        }



        protected override void OnData()
        {
            base.OnData();

            if (this.Data != null)
            {
                this.Setup();
            }
            else
            {
                this.CalendarPartial = new Json();
                this.ContentPartial = new Json();
            }
        }


        public RoomBookingEvent ActiveEvent {
            get {
                if (this.Data == null) return null;
                DateTime utcNow = DateTime.UtcNow;
                RoomBookingEvent roomBookingEvent = Db.SQL<RoomBookingEvent>("SELECT o FROM RoomBooking.RoomBookingEvent o WHERE o.Room = ? AND ? >= o.BeginUtcDate AND ? < o.EndUtcDate AND o.EndUtcDate >= o.BeginUtcDate ORDER BY o.BeginUtcDate", this.Data.Room, utcNow, utcNow).FirstOrDefault();
                return roomBookingEvent;
            }
        }

        private void Setup()
        {
            CalendarPage calendarePage = new CalendarPage();

            calendarePage.Init(this.Data.Room.TimeZoneInfo);

            calendarePage.OnEventSelected = (roomBookingEvent) =>
            {
                if (calendarePage.Transaction != null)
                {
                    calendarePage.Transaction.Rollback();
                }
                this.ContentPartial = CreateEventPage(roomBookingEvent);
            };

            calendarePage.OnNewBooking = (beginUtcDate, endUtcDate) =>
            {
                if (calendarePage.Transaction != null)
                {
                    calendarePage.Transaction.Rollback();
                }

                this.ContentPartial = this.CreateNewBookingPage(this.Data.Room, beginUtcDate, endUtcDate);

            };

            this.CalendarPartial = calendarePage;
            this.ContentPartial = GetDefaultPage();

            // this.RegisterTimer();
        }


        #region Pages

        /// <summary>
        /// Show default page
        /// Free, Busy or Event
        /// </summary>
        private Json GetDefaultPage()
        {
            if (this.Data == null) return new Json();

            // If user is viewing an event do not switch the view
            if (this.ContentPartial is EventPage)
            {
                return this.ContentPartial;
            }

            // Get current event
            RoomBookingEvent roomBookingEvent = this.ActiveEvent;
            if (roomBookingEvent != null)
            {
                if (this.ContentPartial is BusyPage)
                {
                    if (((BusyPage)this.ContentPartial).Booking.Data.Equals(roomBookingEvent))
                    {
                        return this.ContentPartial;
                    }
                }
                return CreateBusyPage(roomBookingEvent);
            }

            if (this.ContentPartial is FreePage)
            {
                return this.ContentPartial;
            }
            return CreateFreePage(this.Data.Room);
        }


        /// <summary>
        /// Create Event page
        /// </summary>
        /// <param name="roomBookingEvent"></param>
        /// <returns></returns>
        private EventPage CreateEventPage(RoomBookingEvent roomBookingEvent)
        {
            EventPage eventPage = new EventPage() { Data = roomBookingEvent };
            eventPage.OnClose = () =>
            {
                this.ContentPartial = new Json();   // Workaround
                this.ContentPartial = GetDefaultPage();
            };

            return eventPage;
        }

        /// <summary>
        /// Create Free page
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private FreePage CreateFreePage(Room room)
        {
            FreePage freePage = new FreePage();
            freePage.Init(room);
            freePage.OnNewBooking = () => this.ContentPartial = CreateNewBookingPage(room, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Quick booking");   // TODO:

            return freePage;
        }

        /// <summary>
        /// Create Busy page
        /// </summary>
        /// <param name="roomBookingEvent"></param>
        /// <returns></returns>
        private BusyPage CreateBusyPage(RoomBookingEvent roomBookingEvent)
        {
            BusyPage busyPage = new BusyPage();
            busyPage.Booking.Data = roomBookingEvent;
            busyPage.OnClose = () => this.ContentPartial = GetDefaultPage();
            busyPage.OnClaim = () =>
            {
                this.ContentPartial = CreateNewBookingPage(roomBookingEvent.Room, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

                Db.Transact(() =>
                {
                    roomBookingEvent.Delete();  // TODO: Do not delete "claimed" booking
                });
            };

            return busyPage;
        }

        /// <summary>
        /// Create New booking page
        /// </summary>
        /// <param name="room"></param>
        /// <param name="defaultBeginUtcDate"></param>
        /// <param name="defaultEndUtcDate"></param>
        /// <returns></returns>
        private NewBookingPage CreateNewBookingPage(Room room, DateTime defaultBeginUtcDate, DateTime defaultEndUtcDate, string title = null)
        {
            RoomBookingEvent roomBookingEvent = new RoomBookingEvent()
            {
                BeginUtcDate = defaultBeginUtcDate,
                EndUtcDate = defaultEndUtcDate,
                Room = room,
                Title = title
            };

            NewBookingPage newBookingPage = new NewBookingPage() { Data = roomBookingEvent };
            newBookingPage.OnClose = () => this.ContentPartial = GetDefaultPage();

            return newBookingPage;
        }

        #endregion


        //#region Timer


        //public void RegisterTimer()
        //{
        //    EventTimer = new Timer(TimerCallback);
        //    SetEventTimer();
        //}

        //private Timer EventTimer;

        //private void SetEventTimer()
        //{
        //    DateTime utcNow = DateTime.UtcNow;

        //    RoomBookingEvent firstEvent = RoomBookingEvent.GetNextEvent(this.Data.Room);
        //    if (firstEvent != null)
        //    {
        //        TimeSpan timeSpan = firstEvent.BeginUtcDate - utcNow;
        //        EventTimer.Change(timeSpan, TimeSpan.FromTicks(-1));
        //    }
        //}

        //public void TimerCallback(Object state)
        //{
        //    Scheduling.ScheduleTask(() =>
        //    {
        //        // Set timer to next event
        //        SetEventTimer();

        //        this.ContentPartial = GetDefaultPage();

        //        Program.PushChanges();

        //    }, false);

        //}


        //#endregion

    }
}