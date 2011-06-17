﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Facebook.Web;
using System.Web.Security;
using Appleseed.Framework.Site.Configuration;
using Facebook;
using Appleseed.Framework.Security;
using Appleseed.Framework;
using System.Web.Profile;
using blowery.Web.HttpCompress;
using Appleseed.Framework.UI.WebControls;
using Appleseed.Framework.Providers.AppleseedMembershipProvider;
using System.Net.Mail;
using Appleseed.Framework.Helpers;
using System.Text;
using Appleseed.Framework.Settings;
using Twitterizer;
using System.Security.Cryptography;

namespace Appleseed.DesktopModules.CoreModules.SignIn
{
    public partial class SignInSocialNetwork : SignInControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var facebookContext = GetFacebookWebContext();
            if (facebookContext != null) {
                appId.Value = PortalSettings.CustomSettings["SITESETTINGS_FACEBOOK_APP_ID"].ToString();
                if (facebookContext.IsAuthenticated()) {
                    //Here is were i check if the user login via facebook
                    FacebookSignInMethod();
                }
            } else {
                //TODO: ocultar boton y mostrar warning
                loginfb_div.Visible = false;
                errfb.Visible = true;
            }

            try { 
                var TwitterRequestToken = GetTwitterRequestToken();
                if (TwitterRequestToken != null) {
                    Uri authenticationUri = OAuthUtility.BuildAuthorizationUri(TwitterRequestToken.Token, true);

                    string url = authenticationUri.AbsoluteUri;
                    LogIn.Text = "Log in Twitter";
                    LogIn.NavigateUrl = url;



                } else {
                    //TODO: ocultar boton y mostrar warning
                    logintwit_div.Visible = false;
                    errtwit.Visible = true;
                }
             } catch (TwitterizerException ex) {
                logintwit_div.Visible = false;
                errtwit.Visible = true;
            }
        }

        private void UpdateProfile()
        {
            var client = new FacebookWebClient();
            dynamic me = client.Get("me");

            ProfileManager.Provider.ApplicationName = PortalSettings.PortalAlias;
            ProfileBase profile = ProfileBase.Create(me.email);
            profile.SetPropertyValue("Email", me.email);
            profile.SetPropertyValue("Name", me.name);
            try {
                profile.Save();
            } catch (Exception exc) {
                ErrorHandler.Publish(LogLevel.Error, "Error al salvar un perfil", exc);
            }
        }

        #region Properties

        public override Guid GuidID
        {
            get
            {
                return new Guid("{008D65E2-FB7E-4B00-BC2E-7491388BACDE}");
            }
        }

        #endregion

        #region Facebook Method

        /// <summary>
        /// check if facebook settings were setting up from the portal settings, if not update the facebooksettings section of the web config file
        /// </summary>
        internal FacebookWebContext GetFacebookWebContext()
        {
            if (PortalSettings.CustomSettings.ContainsKey("SITESETTINGS_FACEBOOK_APP_ID") &&
                !PortalSettings.CustomSettings["SITESETTINGS_FACEBOOK_APP_ID"].ToString().Equals(string.Empty) &&
                PortalSettings.CustomSettings.ContainsKey("SITESETTINGS_FACEBOOK_APP_SECRET") &&
                !PortalSettings.CustomSettings["SITESETTINGS_FACEBOOK_APP_SECRET"].ToString().Equals(string.Empty)) {
                string appId = PortalSettings.CustomSettings["SITESETTINGS_FACEBOOK_APP_ID"].ToString();
                var appSecret = PortalSettings.CustomSettings["SITESETTINGS_FACEBOOK_APP_SECRET"].ToString();

                if (FacebookWebContext.Current.Settings != null) {
                    var facebookConfigurationSection = new FacebookConfigurationSection();
                    facebookConfigurationSection.AppId = appId;
                    facebookConfigurationSection.AppSecret = appSecret;
                    return new FacebookWebContext(facebookConfigurationSection);
                }
            }

            return null;
        }

        private void FacebookSignInMethod()
        {
            var client = new FacebookWebClient();
            dynamic me = client.Get("me");
            Session["CameFromSocialNetwork"] = true;
            if (Membership.GetUser(me.email) == null) {
                
                Session["FacebookUserName"] = me.email;
                Session["FacebookName"] = me.name;

                Session["FacebookPassword"] = GeneratePasswordHash(me.email);
                string urlRegister = ConvertRelativeUrlToAbsoluteUrl("~/DesktopModules/CoreModules/Register/Register.aspx");
                Response.Redirect(urlRegister);
            } else
                PortalSecurity.SignOn(me.email, GeneratePasswordHash(me.email));
            
        }

        public string GeneratePasswordHash(string thisPassword)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] tmpSource;
            byte[] tmpHash;

            tmpSource = ASCIIEncoding.ASCII.GetBytes(thisPassword); // Turn password into byte array
            tmpHash = md5.ComputeHash(tmpSource);

            StringBuilder sOutput = new StringBuilder(tmpHash.Length);
            for (int i = 0; i < tmpHash.Length; i++) {
                sOutput.Append(tmpHash[i].ToString("X2"));  // X2 formats to hexadecimal
            }
            return sOutput.ToString();
        }

        public override void Logoff()
        {
            var context = GetFacebookWebContext();
            if (context != null && context.IsAuthenticated()) {
                context.DeleteAuthCookie();
            }
            Session.RemoveAll();
        }

        #endregion Facebook Methods

        #region Twitter Method

        internal OAuthTokenResponse GetTwitterRequestToken()
        {
            if (PortalSettings.CustomSettings.ContainsKey("SITESETTINGS_TWITTER_APP_ID") &&
                !PortalSettings.CustomSettings["SITESETTINGS_TWITTER_APP_ID"].ToString().Equals(string.Empty) &&
                PortalSettings.CustomSettings.ContainsKey("SITESETTINGS_TWITTER_APP_SECRET") &&
                !PortalSettings.CustomSettings["SITESETTINGS_TWITTER_APP_SECRET"].ToString().Equals(string.Empty)) {
                string appId = PortalSettings.CustomSettings["SITESETTINGS_TWITTER_APP_ID"].ToString();
                var appSecret = PortalSettings.CustomSettings["SITESETTINGS_TWITTER_APP_SECRET"].ToString();

                string server = ConvertRelativeUrlToAbsoluteUrl("~/DesktopModules/CoreModules/SignIn/LogInTweeter.aspx");
                Session["TwitterAppId"] = appId;
                Session["TwitterAppSecret"] = appSecret;

                OAuthTokenResponse requestToken = OAuthUtility.GetRequestToken(appId, appSecret, server);

                return requestToken;

            }

            return null;
        }

        public string ConvertRelativeUrlToAbsoluteUrl(string relativeUrl)
        {

            if (Request.IsSecureConnection)

                return string.Format("https://{0}{1}", Request.Url.Host, Page.ResolveUrl(relativeUrl));

            else

                return string.Format("http://{0}{1}", Request.Url.Host, Page.ResolveUrl(relativeUrl));

        }

        #endregion
    }
}