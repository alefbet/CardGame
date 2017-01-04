using System;
using System.Collections.Generic;
using System.Text;

//using Windows.Security.Authentication.Web;
#if __ANDROID__
#else
using Facebook;
#endif

namespace WordGame
{
    class FacebookIntegration
    {
        static string LoginFacebookGetId()
        {
#if __ANDROID__
#else
            Facebook.FacebookClient client = new FacebookClient();
            client.AppId = "1892969250922974";
            client.AppSecret = "df13ef5e0074215b1e131dc58f9167ff";

#endif
            return "fail";        

        }
    }
}
