﻿using Dapper.Contrib.Extensions;
using FluentMigrator;
using FluentMigrator.Builders.Insert;
using FluentMigrator.Model;
using OTA;
using OTA.Data.Dapper.Extensions;
using OTA.Permissions;
using TDSM.Core.Command;
using System.Linq;
using System;

namespace TDSM.Core.Data.Models.Migrations
{
    [Migration(1)]
    public partial class CreateAndSeed : Migration
    {
        public override void Up()
        {
            Player_Up();
            PermissionNode_Up();
            Group_Up();

            GroupNode_Up();
            PlayerNode_Up();
            PlayerGroup_Up();

            SlotItem_Up();
            LoadoutItem_Up();
            Character_Up();

            APIAccount_Up();
            APIAccountRole_Up();

            Seed();
        }

        public override void Down()
        {
            APIAccountRole_Down();
            APIAccount_Down();

            Character_Down();
            LoadoutItem_Down();
            SlotItem_Down();

            PlayerGroup_Down();
            PlayerNode_Down();
            GroupNode_Down();

            Group_Down();
            PermissionNode_Down();
            Player_Down();
        }

        void Seed()
        {
            Entry.CreateDefaultGroups(this);
        }
    }
}