﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Outbox.Sql {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SQL {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SQL() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Outbox.Sql.SQL", typeof(SQL).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to with CTE as (
        ///    select top (@BatchSize) *
        ///    from dbo.Outbox
        ///    where
        ///        ProcessedAtUtc is not null or RetryCount &lt;= @MaxRetryCount
        ///    order by SeqNum
        ///)
        ///delete from CTE
        ///.
        /// </summary>
        internal static string Delete {
            get {
                return ResourceManager.GetString("Delete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to insert into dbo.Outbox (
        ///    MessageId,
        ///    MessageType,
        ///    Topic,
        ///    PartitionId,
        ///    Payload
        ///) values (
        ///    @MessageId,
        ///    @MessageType,
        ///    @Topic,
        ///    @PartitionId,
        ///    @Payload
        ///);
        ///
        ///select CONVERT(bigint, SCOPE_IDENTITY());
        ///.
        /// </summary>
        internal static string InsertDefault {
            get {
                return ResourceManager.GetString("InsertDefault", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to insert into dbo.Outbox (
        ///    MessageId,
        ///    MessageType,
        ///    Topic,
        ///    PartitionId,
        ///    Payload,
        ///    RetryCount,
        ///    LockedAtUtc,
        ///    GeneratedAtUtc,
        ///    LastErrorAtUtc,
        ///    ProcessedAtUtc
        ///) values (
        ///    @MessageId,
        ///    @MessageType,
        ///    @Topic,
        ///    @PartitionId,
        ///    @Payload,
        ///    @RetryCount,
        ///    @LockedAtUtc,
        ///    @GeneratedAtUtc,
        ///    @LastErrorAtUtc,
        ///    @ProcessedAtUtc
        ///);
        ///
        ///select CONVERT(bigint, SCOPE_IDENTITY());
        ///.
        /// </summary>
        internal static string InsertRaw {
            get {
                return ResourceManager.GetString("InsertRaw", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select
        ///    SeqNum,
        ///    MessageId,
        ///    MessageType,
        ///    Topic,
        ///    PartitionId,
        ///    RetryCount,
        ///    LockedAtUtc,
        ///    GeneratedAtUtc,
        ///    LastErrorAtUtc,
        ///    ProcessedAtUtc,
        ///    Payload
        ///from dbo.Outbox
        ///where SeqNum = @SeqNum;
        ///.
        /// </summary>
        internal static string Select {
            get {
                return ResourceManager.GetString("Select", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --declare @BatchSize int = 10;
        ///--declare @MaxRetryCount int = 3;
        ///--declare @LockTimeoutInSeconds int = 120;
        ///
        ///declare @RowsToProcess table ( -- the definition must be in sync with dbo.Outbox table
        ///    SeqNum bigint not null,
        ///    MessageId       varchar(36)     not null,
        ///    MessageType     varchar(512)    not null,
        ///    Topic           varchar(128)    not null,
        ///    PartitionId     varchar(32)     null,
        ///    RetryCount      tinyint         not null,
        ///    LockedAtUtc     datetime2       null,
        ///    Gene [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SelectForProcessing {
            get {
                return ResourceManager.GetString("SelectForProcessing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select
        ///    SeqNum,
        ///    MessageId,
        ///    MessageType,
        ///    Topic,
        ///    PartitionId,
        ///    RetryCount,
        ///    LockedAtUtc,
        ///    GeneratedAtUtc,
        ///    LastErrorAtUtc,
        ///    ProcessedAtUtc,
        ///    Payload
        ///from dbo.OutboxProcessed
        ///where SeqNum = @SeqNum;
        ///.
        /// </summary>
        internal static string SelectProcessed {
            get {
                return ResourceManager.GetString("SelectProcessed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to truncate table dbo.Outbox;
        ///.
        /// </summary>
        internal static string Truncate {
            get {
                return ResourceManager.GetString("Truncate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to with CTE as (
        ///    select top (@BatchSize) *
        ///    from dbo.Outbox
        ///    where
        ///        LockedAtUtc is not null and DATEDIFF(ss, LockedAtUtc, GETUTCDATE()) &gt; @LockTimeoutInSeconds
        ///    order by SeqNum
        ///)
        ///update CTE set LockedAtUtc = null
        ///.
        /// </summary>
        internal static string Unlock {
            get {
                return ResourceManager.GetString("Unlock", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to declare @SeqNum bigint = 8;
        ///declare @Move bit = 1;
        ///--declare @MaxRetryCount tinyint = 2;
        ///--declare @RetryCount tinyint;
        ///-- select * from dbo.Outbox
        ///-- select * from dbo.OutboxProcessed
        ///--update dbo.Outbox set RetryCount = 2 where SeqNum=9
        ///
        ///begin tran;
        ///
        ///if @Move = 1
        ///begin
        ///    insert into dbo.OutboxProcessed (
        ///            SeqNum,
        ///            MessageId,
        ///            MessageType,
        ///            Topic,
        ///            PartitionId,
        ///            RetryCount,
        ///            LockedAtUtc,
        ///            Generated [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string UpdateSuccessful {
            get {
                return ResourceManager.GetString("UpdateSuccessful", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --declare @SeqNum bigint = 9;
        ///--declare @MaxRetryCount tinyint = 2;
        ///--declare @RetryCount tinyint;
        ///-- select * from dbo.Outbox
        ///-- select * from dbo.OutboxProcessed
        ///--update dbo.Outbox set RetryCount = 2 where SeqNum=9
        ///
        ///begin tran
        ///
        ///declare @RetryCount tinyint;
        ///update dbo.Outbox set
        ///    LastErrorAtUtc = GETUTCDATE(),
        ///    RetryCount = RetryCount + 1,
        ///    @RetryCount = RetryCount + 1
        ///where SeqNum = @SeqNum;
        ///
        ///if @RetryCount &gt; @MaxRetryCount
        ///begin
        ///
        ///insert into dbo.OutboxProcessed (
        ///        Seq [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string UpdateUnsuccessful {
            get {
                return ResourceManager.GetString("UpdateUnsuccessful", resourceCulture);
            }
        }
    }
}
