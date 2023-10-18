// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;

namespace IdentityServer4.Test
{
    /// <summary>
    /// Store for test users
    /// </summary>
    public class UserStore
    {
        private readonly IdentityContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserStore"/> class.
        /// </summary>
        /// <param name="context">The Identity Context to get the user related data from database</param>
        public UserStore(IdentityContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public bool ValidateCredentials(string username, string password)
        {
            var user = FindByUsername(username);

            if (user != null)
            {
                if (string.IsNullOrWhiteSpace(user.Password) && string.IsNullOrWhiteSpace(password))
                {
                    return true;
                }

                return user.Password.Equals(password.ToSha256());
            }

            return false;
        }

        /// <summary>
        /// Finds the user by subject identifier.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns></returns>
        public User FindBySubjectId(string subjectId)
        {
            AspNetUser user = _context.AspNetUsers.ToList().FirstOrDefault(x => GetSubId(x.Id) == subjectId);
            if (user != null)
                return new User()
                {
                    SubjectId = GetSubId(user.Id),
                    Username = user.UserName,
                    IsActive = true,
                    Claims = GetClaims(user),
                    Password = user.PasswordHash
                };
            else return null;
        }

        /// <summary>
        /// Finds the user by username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public User FindByUsername(string username)
        {
            AspNetUser user = _context.AspNetUsers.FirstOrDefault(x => x.UserName == username);
            if (user != null)
                return new User()
                {
                    SubjectId = GetSubId(user.Id),
                    Username = user.UserName,
                    IsActive = true,
                    Claims = GetClaims(user),
                    Password = user.PasswordHash
                };
            else
                return null;
        }

        /// <summary>
        /// Finds the claims for the iout user
        /// </summary>
        /// <param name="loggedUser">The user identifier.</param>
        /// <returns></returns>
        public List<Claim> GetClaims(AspNetUser loggedUser)
        {
            var userClaims = _context.Set<AspNetUserClaim>().Where(n => n.UserId == loggedUser.Id).ToList(); ;

            List<Claim> claimlist = new List<Claim>();
            
            var roles = (from user in _context.Set<AspNetUser>()
                                          join userrole in _context.Set<AspNetUserRole>() on user.Id equals userrole.UserId
                                          join role in _context.Set<AspNetRole>() on userrole.RoleId equals role.Id
                                          select role.Name).ToList();



            foreach (var role in roles) { claimlist.Add(new Claim("role", role)); }

            var roleclaims = (from user in _context.Set<AspNetUser>()
                              join userrole in _context.Set<AspNetUserRole>() on user.Id equals userrole.UserId
                              join roleclaim in _context.Set<AspNetRoleClaim>() on userrole.RoleId equals roleclaim.RoleId
                              where user.Id == loggedUser.Id
                              select new Claim(roleclaim.ClaimType, roleclaim.ClaimValue)).ToList().GroupBy(x => x.Value).Select(y => y.First());

            claimlist.AddRange(roleclaims);

            foreach (AspNetUserClaim claim in userClaims)
            {
                claimlist.Add(new Claim(claim.ClaimType, claim.ClaimValue));
            }

            return claimlist;
        }

        /// <summary>
        /// Finds the user by external provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public User FindByExternalProvider(string provider, string userId)
        {
            return new User();
        }

        /// <summary>
        /// Automatically provisions a user.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        /// IMPORTANT!!!!
        /// we need to update this method to add a new user to the database in order to support 
        /// external login if needed!!
        public User AutoProvisionUser(string provider, string userId, List<Claim> claims)
        {
            // create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            foreach (var claim in claims)
            {
                // if the external system sends a display name - translate that to the standard OIDC name claim
                if (claim.Type == ClaimTypes.Name)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                }
                // if the JWT handler has an outbound mapping to an OIDC claim use that
                else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
                {
                    filtered.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
                }
                // copy the claim as-is
                else
                {
                    filtered.Add(claim);
                }
            }

            // if no display name was provided, try to construct by first and/or last name
            if (!filtered.Any(x => x.Type == JwtClaimTypes.Name))
            {
                var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
                var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // create a new unique subject id
            var sub = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

            // check if a display name is available, otherwise fallback to subject id
            var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? sub;

            // create new user
            var user = new User
            {
                SubjectId = sub,
                Username = name,
                ProviderName = provider,
                ProviderSubjectId = userId,
                Claims = filtered
            };

            // add user to in-memory store
            //_users.Add(user);

            return user;
        }
        /// <summary>
        /// Calculates the subject id from the user id
        /// </summary>
        /// <param name="input">The userId</param>
        /// <returns></returns>
        public static string GetSubId(string input)
        {
            return GetNumbers(input);
        }

        private static string GetNumbers(string input)
        {
            string output = new string(input.Where(c => char.IsDigit(c)).ToArray());
            if (output.Length >= 7)
                return output.Substring(0, 7);
            else
                return output;
        }

    }
}