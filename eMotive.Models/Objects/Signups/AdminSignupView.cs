﻿using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace eMotive.Models.Objects.Signups
{
    public class AdminSignupView
    {
        public IEnumerable<SignupsMod.Signup> Signups { get; set; }

        public IDictionary<string, List<SignupsMod.Signup>> GetSignupsGroupedByGroup()
        {
            if (!Signups.HasContent())
                return null;

            var dict = Signups.GroupBy(m => m.Group.Name).ToDictionary(k => k.Key, v => v.ToList());

            return dict;
        }
    }
}
