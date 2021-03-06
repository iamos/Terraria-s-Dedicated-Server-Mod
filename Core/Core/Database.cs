﻿using System;
using OTA.Command;
using System.Linq;
using OTA.Data;
using TDSM.Core.Data;
using OTA.Logging;
using TDSM.Core.ServerCharacters;
using TDSM.Core.ServerCharacters.Tables;
using TDSM.Core.Command;
using TDSM.Core.Data.Permissions;
using TDSM.Core.Data.Models;
using OTA;
using OTA.Permissions;
using System.Data;
using Dapper.Contrib.Extensions;
using OTA.Data.Dapper.Extensions;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using OTA.Plugin;

namespace TDSM.Core
{
    public partial class Entry
    {
        public const String Setting_Mana = "SSC_Mana";
        public const String Setting_MaxMana = "SSC_MaxMana";
        public const String Setting_Health = "SSC_Health";
        public const String Setting_MaxHealth = "SSC_MaxHealth";

        [TDSMComponent(ComponentEvent.Enabled)]
        public static void SetupDatabase(Entry plugin)
        {
#if ENTITY_FRAMEWORK_6
            Storage.IsAvailable = OTA.Data.EF6.OTAContext.HasConnection;

            if (Storage.IsAvailable) ProgramLog.Admin.Log("Entity framework has a registered connection.");
            else ProgramLog.Admin.Log("Entity framework has no registered connection.");

            //if (Storage.IsAvailable)
            //    using (var ctx = new TContext())
            //    {
            //        ctx.APIAccounts.Add(new TDSM.Core.Data.Models.APIAccount()
            //        {
            //            Username = "Test",
            //            Password = "Testing"
            //        });

            //        ctx.SaveChanges();
            //    }

            //#elif ENTITY_FRAMEWORK_7
            //using (var ctx = new TContext())
            //{
            //    ctx.Database.EnsureCreated();
            //    Storage.IsAvailable = true;
            //}
#endif
        }

        //protected override void DatabaseInitialising(System.Data.Entity.DbModelBuilder builder)
        //{
        //    base.DatabaseInitialising(builder);

        //    using (var dbc = new TDSM.Core.Data.TContext())
        //    {
        //        dbc.CreateModel(builder);
        //    }
        //}

        protected override void DatabaseCreated()
        {
            base.DatabaseCreated();

            //using (var ctx = new TContext())
            //{
            //    ProgramLog.Admin.Log("Creating default groups...");
            //    CreateDefaultGroups(ctx);

            //    ProgramLog.Admin.Log("Creating default SSC values...");
            //    DefaultLoadoutTable.PopulateDefaults(ctx, true, CharacterManager.StartingOutInfo);
            //}
        }

#if ENTITY_FRAMEWORK_7
        public void CreateDefaultGroups(TContext ctx)
#elif DAPPER
        public static void CreateDefaultGroups(IDbConnection ctx, IDbTransaction txn)
#endif
        {
            var pc = OTA.Commands.CommandManager.Parser.GetTDSMCommandsForAccessLevel(AccessLevel.PLAYER);
            var ad = OTA.Commands.CommandManager.Parser.GetTDSMCommandsForAccessLevel(AccessLevel.OP);
            var op = OTA.Commands.CommandManager.Parser.GetTDSMCommandsForAccessLevel(AccessLevel.CONSOLE); //Funny how these have now changed

            var additionalGuestNodes = new[]
            {
                "ota.help",
                "terraria.playing",
                "terraria.time",
                "ota.plugins"
            };

            CreateGroup("Guest", true, null, 255, 255, 255, pc
                    .Where(x => !String.IsNullOrEmpty(x.Node))
                    .Select(x => x.Node)
                    .Concat(additionalGuestNodes)
                    .Distinct()
                    .ToArray(), ctx, txn, "[Guest] ");
            CreateGroup("Player", false, "Guest", 255, 255, 255, pc
                    .Where(x => !String.IsNullOrEmpty(x.Node))
                    .Select(x => x.Node)
                    .Concat(additionalGuestNodes)
                    .Distinct()
                    .ToArray(), ctx, txn, "[Player] ");
            CreateGroup("Admin", false, "Guest", 240, 131, 77, ad
                    .Where(x => !String.IsNullOrEmpty(x.Node))
                    .Select(x => x.Node)
                    .Distinct()
                    .ToArray(), ctx, txn, "[Admin] ");
            CreateGroup("Operator", false, "Admin", 77, 166, 240, op
                    .Where(x => !String.IsNullOrEmpty(x.Node))
                    .Select(x => x.Node)
                    .Distinct()
                    .ToArray(), ctx, txn, "[OP] ");
        }

        public static void PopulateDefaults(Data.Models.Migrations.CreateAndSeed migration)
        {
            migration.Execute.WithConnection((ctx, transaction) =>
            {
                CreateDefaultGroups(ctx, transaction);
                DefaultLoadoutTable.PopulateDefaults(ctx, transaction, true, CharacterManager.StartingOutInfo);
            });
        }

        [Hook]
        void OnPlayerRegistered(ref HookContext ctx, ref Events.HookArgs.PlayerRegistered args)
        {
            if (!String.IsNullOrEmpty(Config.DefaultPlayerGroup))
                foreach (var groupName in Config.DefaultPlayerGroup.Split(','))
                {
                    var group = args.Connection.Single<Group>(new { Name = groupName }, transaction: args.Transaction);

                    //Temporary until the need for more than one group
                    if (args.Connection.Where<PlayerGroup>(new { PlayerId = args.Player.Id }, transaction: args.Transaction).Any(x => x.GroupId > 0))
                        throw new NotSupportedException("A player can only be associated to one group, please assign a parent to the desired group");

                    args.Connection.Insert(new PlayerGroup()
                    {
                        GroupId = group.Id,
                        PlayerId = args.Player.Id
                    }, transaction: args.Transaction);
                }
        }

#if ENTITY_FRAMEWORK_7
        static void CreateGroup(string name, bool guest, string parent, byte r, byte g, byte b, string[] nodes, TContext ctx,
                                string chatPrefix = null,
                                string chatSuffix = null)
#elif DAPPER
        static void CreateGroup(string name, bool guest, string parent, byte r, byte g, byte b, string[] nodes, IDbConnection ctx, IDbTransaction txn,
                                string chatPrefix = null,
                                string chatSuffix = null)
#endif

        {
#if ENTITY_FRAMEWORK_7
            var grp = new Group()
            {
                Name = name,
                ApplyToGuests = guest,
                Parent = parent,
                Chat_Red = r,
                Chat_Green = g,
                Chat_Blue = b,
                Chat_Prefix = chatPrefix,
                Chat_Suffix = chatSuffix
            };
            ctx.Groups.Add(grp);

            ctx.SaveChanges(); //Save to get the ID

            foreach (var nd in nodes)
            {
                var node = ctx.Nodes.SingleOrDefault(x => x.Node == nd && x.Permission == Permission.Permitted);
                if (node == null)
                {
                    ctx.Nodes.Add(node = new NodePermission()
                        {
                            Node = nd,
                            Permission = Permission.Permitted
                        });

                    ctx.SaveChanges();
                }

                ctx.GroupNodes.Add(new GroupNode()
                    {
                        GroupId = grp.Id,
                        NodeId = node.Id 
                    });
            }

            ctx.SaveChanges();
#elif DAPPER
            var grp = new Group()
            {
                Name = name,
                ApplyToGuests = guest,
                Parent = parent,
                Chat_Red = r,
                Chat_Green = g,
                Chat_Blue = b,
                Chat_Prefix = chatPrefix,
                Chat_Suffix = chatSuffix
            };


            grp.Id = ctx.Insert(grp, txn);
            foreach (var nd in nodes)
            {
                var node = ctx.SingleOrDefault<PermissionNode>(new { Node = nd, Permission = Permission.Permitted }, transaction: txn);
                if (node == null)
                {
                    node = new PermissionNode()
                    {
                        Node = nd,
                        Permission = Permission.Permitted
                    };
                    node.Id = ctx.Insert(node, transaction: txn);
                }

                ctx.Insert(new GroupNode()
                {
                    GroupId = grp.Id,
                    NodeId = node.Id
                }, transaction: txn);
            }
#endif
        }
    }
}