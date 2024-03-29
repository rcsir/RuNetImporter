﻿using System.Collections.Generic;

namespace Smrf.AppLib
{
    public static class AttributeUtils
    {
        public struct Attribute
        {
            public string name, value, permission;
            public bool required;

            public Attribute(string name, string value)
            {
                this.name = name;
                this.value = value;
                this.permission = default(string);
                this.required = false;
            }
        
            public Attribute(string name, string value, string permission, bool required)
            {
                this.name = name;
                this.value = value;
                this.permission = permission;
                this.required = required;
            }
        }

        public static List<Attribute> UserAttributes = new List<Attribute>()
        {
            new Attribute("Name","name"),
            new Attribute("First Name","first_name"),
            new Attribute("Middle Name","middle_name"),
            new Attribute("Last Name","last_name"),
            new Attribute("Hometown","hometown_location"),
            new Attribute("Current Location","current_location"),
//  Add for OK
            new Attribute("Age", "age"),
            new Attribute("Status", "current_status"),
            new Attribute("Last online", "last_online"),
            new Attribute("Registered date","registered_date"),
            new Attribute("Anonym access","allows_anonym_access"),
//  End Add
            new Attribute("Birthday","birthday"),
            new Attribute("Picture","pic_small"),
            new Attribute("Profile Update Time","profile_update_time"),
            new Attribute("Timezone","timezone"),
            new Attribute("Religion","religion"),
            new Attribute("Sex","sex"),
            new Attribute("Relationship","relationship_status"),
            new Attribute("Political Views","political"),
            new Attribute("Activities","activities"),
            new Attribute("Interests","interests"),
            new Attribute("Music","music"),
            new Attribute("TV","tv"),
            new Attribute("Movies","movies"),
            new Attribute("Books","books"),
            new Attribute("Quotes","quotes"),
            new Attribute("About Me","about_me"),
            new Attribute("Online Presence","online_presence"),
            new Attribute("Locale","locale"),
            new Attribute("Website","website"),
        };
    }
}
