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
        internal static string Insert {
            get {
                return ResourceManager.GetString("Insert", resourceCulture);
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
    }
}
