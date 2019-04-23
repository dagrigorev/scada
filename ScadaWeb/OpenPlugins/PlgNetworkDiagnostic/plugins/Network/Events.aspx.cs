using System;

namespace Scada.Web.Plugins.Network
{
    public partial class Events : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AppData appData = AppData.GetAppData();
            UserData userData = UserData.GetUserData();

            // проверка входа в систему и прав
            userData.CheckLoggedOn(true);

            if (!userData.UserRights.ConfigRight)
                throw new ScadaException(CommonPhrases.NoRights);
        }
    }
}