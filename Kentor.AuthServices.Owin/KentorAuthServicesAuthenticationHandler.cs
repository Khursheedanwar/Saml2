﻿using Kentor.AuthServices.Configuration;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kentor.AuthServices.Owin
{
    class KentorAuthServicesAuthenticationHandler : AuthenticationHandler<KentorAuthServicesAuthenticationOptions>
    {
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var result = CommandFactory.GetCommand("acs").Run(Context.ToHttpRequestData());

            var properties = new AuthenticationProperties()
            {
                RedirectUri = result.Location.ToString()
            };

            return Task.FromResult<AuthenticationTicket>(
                new MultipleIdentityAuthenticationTicket(result.Principal.Identities, properties));
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                var challenge = Helper.LookupChallenge(Options.AuthenticationType, AuthenticationMode.Passive);

                if (challenge != null)
                {
                    string idp;
                    if (!challenge.Properties.Dictionary.TryGetValue("idp", out idp))
                    {
                        object objIdp = null;
                        Context.Environment.TryGetValue("KentorAuthServices.idp", out objIdp);
                        idp = objIdp as string;
                    }
                    var result = SignInCommand.CreateResult(idp, challenge.Properties.RedirectUri, Context.Request.Uri);
                    Response.Redirect(result.Location.ToString());
                }
            }

            return Task.FromResult(0);
        }

        public override async Task<bool> InvokeAsync()
        {
            if (KentorAuthServicesSection.Current.AssertionConsumerServiceUrl == Request.Uri)
            {
                var ticket = (MultipleIdentityAuthenticationTicket)await AuthenticateAsync();

                Context.Authentication.SignIn(ticket.Properties, ticket.Identities.ToArray());

                Response.Redirect(ticket.Properties.RedirectUri);

                return true;
            }

            return false;
        }
    }
}
