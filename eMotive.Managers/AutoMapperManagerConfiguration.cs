﻿using System;
using System.Reflection;
using AutoMapper;
using System.Linq;
using Extensions;
using eMotive.Models.Objects.Search;
using eMotive.Repository.Objects.News;
using eMotive.Repository.Objects.Pages;
using eMotive.Repository.Objects.Signups;
using eMotive.Repository.Objects.Users;
using Profile = eMotive.Repository.Objects.Users.Profile;
using mUsers = eMotive.Models.Objects.Users;
using mRoles = eMotive.Models.Objects.Roles;
using mNews = eMotive.Models.Objects.News;
using mPages = eMotive.Models.Objects.Pages;
using mSignups = eMotive.Models.Objects.Signups;
using mod = eMotive.Models.Objects.SignupsMod;
using emSearch = eMotive.Search.Objects.Search;

namespace eMotive.Managers
{
    public class AutoMapperManagerConfiguration
    {
        public static void Configure()
        {
           // var assembly = Assembly.GetExecutingAssembly();

         //   var test = Activator.CreateInstance();
            ConfigureUserMapping();
            ConfigureSearchMapping();
            ConfigureNewsMapping();
            ConfigurePageMapping();
            ConfigureSignupMapping();
        }

        private static void ConfigureSignupMapping()
        {
            Mapper.CreateMap<mod.Signup, Signup>().ForMember(m => m.IdGroup, o => o.MapFrom(n => n.Group.ID));
            Mapper.CreateMap<mod.Slot, Slot>();
            Mapper.CreateMap<mod.Group, Group>();
            Mapper.CreateMap<mod.UserSignup, UserSignup>();
            Mapper.CreateMap<mod.SessionAttendance, SessionAttendance>();

            Mapper.CreateMap<Signup, mod.Signup>();
            Mapper.CreateMap<UserSignup, mod.UserSignup>();
            Mapper.CreateMap<Slot, mod.Slot>();
            Mapper.CreateMap<Group, mod.Group>();
            Mapper.CreateMap<SessionAttendance, mod.SessionAttendance>();

            Mapper.CreateMap<Signup, mSignups.Signup>();

            Mapper.CreateMap<Slot, mSignups.Slot>().ForMember(m => m.TotalPlacesAvailable, o => o.MapFrom(m => m.PlacesAvailable));
            Mapper.CreateMap<UserSignup, mSignups.UserSignup>();
            Mapper.CreateMap<Group, mSignups.Group>();
            Mapper.CreateMap<mSignups.Group, Group>();
            Mapper.CreateMap<mSignups.SessionAttendance, SessionAttendance>();
            Mapper.CreateMap<SessionAttendance, mSignups.SessionAttendance>();
            Mapper.CreateMap<mSignups.Signup, mSignups.SessionDay>().ForMember(m => m.Group, o => o.MapFrom(n => n.Group.Name))
                                                                    .ForMember(m => m.MainPlaces, o => o.MapFrom(n => n.Slots.Sum(p => p.TotalPlacesAvailable)))
                                                                    .ForMember(m => m.PlacesLeft, o => o.MapFrom(n => n.Slots.Sum(p => p.TotalPlacesAvailable) - n.Slots.Sum(p => p.ApplicantsSignedUp.HasContent() ? p.ApplicantsSignedUp.Count : 0)));
        }

        private static void ConfigureUserMapping()
        {
            Mapper.CreateMap<User, mUsers.User>();
            Mapper.CreateMap<mUsers.User, User>();

            Mapper.CreateMap<Role, mRoles.Role>();
            Mapper.CreateMap<mRoles.Role, Role>();

            Mapper.CreateMap<ApplicantData, mUsers.ApplicantData>();
            Mapper.CreateMap<mUsers.ApplicantData, ApplicantData>();

            Mapper.CreateMap<Profile, mUsers.Profile>();
            Mapper.CreateMap<mUsers.SCEData, SCEData>();
            Mapper.CreateMap<SCEData, mUsers.SCEData>();
        }

        private static void ConfigureSearchMapping()
        {
            Mapper.CreateMap<BasicSearch, emSearch>().ForMember(m => m.CurrentPage, o => o.MapFrom(m => m.Page));
        }

        private static void ConfigureNewsMapping()
        {
            Mapper.CreateMap<NewsItem, mNews.NewsItem>().ForMember(m => m.Author, o => o.Ignore());
            Mapper.CreateMap<mNews.NewsItem, NewsItem>().ForMember(m => m.AuthorID, o => o.MapFrom(n => n.Author.ID));
        }

        private static void ConfigurePageMapping()
        {
            Mapper.CreateMap<Page, mPages.Page>();
            Mapper.CreateMap<mPages.Page, Page>();

            Mapper.CreateMap<PartialPage, mPages.PartialPage>();
            Mapper.CreateMap<mPages.PartialPage, PartialPage>();
        }
    }
}
