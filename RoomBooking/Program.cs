﻿// Google Client ID: 525016583199-77s56n08s4uuoir2oppc8gs1biv5t6q9.apps.googleusercontent.com
// Google client secret: hTMyupyDfY8LFFCVnghhBPzK

using System;
using Starcounter;
using RoomBooking.ViewModels;
using System.Linq;
using RoomBooking.ViewModels.Screens;
using System.Collections.Specialized;
using Screens.Common;
using RoomBooking.ViewModels.Partials;

namespace RoomBooking
{
    class Program
    {
        static void Main()
        {

            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());

            Screens.Common.Utils.RegisterHooks();

            Room.RegisterHooks();

            RoomBookingEvent.RegisterHooks();
            RoomBookingEvent.RegisterTimer();

            UserRoomRelation.RegisterHooks();

            RoomScreenRelation.RegisterHooks();

            Handle.GET("/RoomBooking", (Request request) =>
            {
                MainPage mainPage = GetMainPage();

                try
                {
                    User user = UserSession.GetSignedInUser();
                    if (user == null)
                    {
                        ViewModels.MessageBox.Show("Access Denied", "You need to be signed in");
                        return mainPage;
                    }

                    UserRoomRelation userRoomRelation = Program.AssureDefaultUserRoom(user);

                    RoomsPage roomsPage = new RoomsPage();
                    mainPage.Content = roomsPage;
                }
                catch( Exception e)
                {
                    ViewModels.ErrorMessageBox.Show(e);
                }

                return mainPage;
            });


            RegisterScreenHandlers();
            RegisterRoomHandlers();
        }

        internal static void RegisterScreenHandlers()
        {

            #region Screen (mapping)

            Handle.GET("/RoomBooking/screenContent/{?}", (Func<string, Response>)((string screenId) =>
            {
                try
                {
                    Screen screen = Db.FromId(screenId) as Screen;

                    RoomScreenRelation roomScreenRelation = Db.SQL<RoomScreenRelation>("SELECT o FROM RoomBooking.RoomScreenRelation o WHERE o.Screen=?", screen).FirstOrDefault();
                    return Db.Scope(() =>
                    {
                        ScreenContentPage mainScreenPage = Program.AssureScreenContentPage();
                        mainScreenPage.Data = roomScreenRelation;
                        return mainScreenPage;
                    });
                }
                catch( Exception e)
                {
                    ViewModels.Screens.ErrorMessageBox.Show(e);
                    return Program.AssureScreenContentPage();
                }
            }));

            Blender.MapUri("/RoomBooking/screenContent/{?}", "screenContent");

            Handle.GET("/RoomBooking/partial/screen/{?}", (string screenId) =>
            {
                Screen screen = Db.FromId(screenId) as Screen;

                if (screen != null)
                {
                    User user = UserSession.GetSignedInUser();
                    if (user != null)
                    {
                        Program.AssureDefaultUserRoom(user);
                        ScreenPartial screenPartial = new ScreenPartial();
                        screenPartial.Data = screen;
                        return screenPartial;
                    }
                }
                return new Json();
            });

            Blender.MapUri("/RoomBooking/partial/screen/{?}", "screen");

            #endregion
        }


        internal static void RegisterRoomHandlers()
        {

            #region Room

            Handle.GET("/roomBooking/rooms", (Request request) =>
            {
                MainPage mainPage = GetMainPage();
                User user = UserSession.GetSignedInUser();
                if (user == null)
                {
                    ViewModels.MessageBox.Show("Access Denied", "You must be signed in");
                    return mainPage;
                }

                RoomsPage roomsPage = new RoomsPage();
                mainPage.Content = roomsPage;
                return mainPage;
            });

            Handle.GET("/roomBooking/addroom", (Request request) =>
            {
                MainPage mainPage = GetMainPage();
                User user = UserSession.GetSignedInUser();

                if (user == null)
                {
                    ViewModels.MessageBox.Show("Access Denied", "You must be signed in");
                    return mainPage;
                }

                return Db.Scope<MainPage>(() =>
                {
                    RoomPage roomPage = new RoomPage();
                    mainPage.Content = roomPage;
                    roomPage.Data = new Room();

                    UserRoomRelation userRoomRelation = new UserRoomRelation();
                    userRoomRelation.Room = roomPage.Data;
                    userRoomRelation.User = user;

                    return mainPage;
                });
            });

            Handle.GET("/roomBooking/rooms/{?}", (string id, Request request) =>
            {
                MainPage mainPage = GetMainPage();

                User user = UserSession.GetSignedInUser();
                if (user == null)
                {
                    ViewModels.MessageBox.Show("Access Denied", "You must be signed in");
                    return mainPage;
                }


                Room room = Db.SQL<Room>("SELECT o FROM RoomBooking.Room o WHERE o.ObjectID=?", id).FirstOrDefault();
                if (room == null)
                {
                    ViewModels.MessageBox.Show("Not found", "Room not found"); // TODO: Show page error instead of popup
                    mainPage.Content = new RoomsPage();
                    return mainPage;
                }

                return Db.Scope<MainPage>(() =>
                {
                    RoomPage roomPage = new RoomPage();
                    roomPage.Data = room;
                    mainPage.Content = roomPage;
                    return mainPage;
                });

            });

            #endregion
        }



        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        internal static UserRoomRelation AssureDefaultUserRoom(User user)
        {

            UserRoomRelation userRoomRelation = Db.SQL<UserRoomRelation>("SELECT o FROM RoomBooking.UserRoomRelation o WHERE o.User = ?", user).FirstOrDefault();
            if (userRoomRelation == null)
            {
                Db.Transact(() =>
                {
                    userRoomRelation = new UserRoomRelation();
                    userRoomRelation.User = user;
                    userRoomRelation.Room = new Room() { Name = "Default", Description = "This is your default room" };
                });
            }

            return userRoomRelation;
        }


        internal static MainPage GetMainPage()
        {
            var session = Session.Ensure();

            MainPage mainPage = session.Store[nameof(MainPage)] as MainPage;

            if (mainPage == null)
            {
                mainPage = new MainPage();
                session.Store[nameof(MainPage)] = mainPage;
            }

            return mainPage;
        }

        internal static ScreenContentPage AssureScreenContentPage()
        {

            var session = Session.Ensure();

            ScreenContentPage mainPage = session.Store[nameof(ScreenContentPage)] as ScreenContentPage;

            if (mainPage == null)
            {
                mainPage = new ScreenContentPage();
                session.Store[nameof(ScreenContentPage)] = mainPage;
            }

            return mainPage;
        }


        internal static void PushChanges()
        {
            Session.ForAll((s, sessionId) =>
            {
                s.CalculatePatchAndPushOnWebSocket();
            });
        }

    }
}