namespace MU5PrototypeProject.Utilities
{
    public static class MaintainURL
    {
        public static string ReturnURL(HttpContext httpContext, string ControllerName)
        {
            string cookieName = ControllerName + "URL";
            string SearchText = "/" + ControllerName + "?";
            string returnURL = httpContext.Request.Headers["Referer"].ToString();
            if (returnURL.Contains(SearchText))
            {
                returnURL = returnURL[returnURL.LastIndexOf(SearchText)..];
                CookieHelper.CookieSet(httpContext, cookieName, returnURL, 30);
                return returnURL;
            }
            else
            {
                returnURL = httpContext.Request.Cookies[cookieName] ?? "/" + ControllerName;
                return returnURL;
            }
        }
    }
}