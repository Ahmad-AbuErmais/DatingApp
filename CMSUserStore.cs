﻿using EskaCMS.Core.Data;
using EskaCMS.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace EskaCMS.Core.Extensions
{
    public class CMSUserStore : UserStore<User, Role, EskaDCMSDbContext, long, IdentityUserClaim<long>, UserRole,
        IdentityUserLogin<long>, IdentityUserToken<long>, IdentityRoleClaim<long>>
    {
        public CMSUserStore(EskaDCMSDbContext context, IdentityErrorDescriber describer) : base(context, describer)
        {
        }
    }
    //public class CMSUserStore : UserStore<User, Role, EskaCMSDbContext, long, IdentityUserClaim<long>, UserRole,
    //    IdentityUserLogin<long>, IdentityUserToken<long>, IdentityRoleClaim<long>>
    //{
    //    public CMSUserStore(EskaCMSDbContext context, IdentityErrorDescriber describer) : base(context, describer)
    //    {
    //    }
    //}
    //public class CMSMySqlUserStore : UserStore<User, Role, EskaCMSMySqlDbContext, long, IdentityUserClaim<long>, UserRole,
    //    IdentityUserLogin<long>, IdentityUserToken<long>, IdentityRoleClaim<long>>
    //{
    //    public CMSMySqlUserStore(EskaCMSMySqlDbContext context, IdentityErrorDescriber describer) : base(context, describer)
    //    {
    //    }
    //}
}