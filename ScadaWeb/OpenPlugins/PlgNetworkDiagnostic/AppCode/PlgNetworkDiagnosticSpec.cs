using Scada.Web.Plugins;
using Scada.Web.Shell;
using System.Collections.Generic;

namespace Scada.Web.Plugins
{
    public class PlgNetworkDiagnosticSpec : PluginSpec
    {
        public override string Name => Localization.UseRussian ? "Net" : "Сеть";

        public override string Descr => Localization.UseRussian ? "Network data view" : "Представление данных о сети";

        public override string Version => "0.0.0.1";

        /// <summary>
        /// Получить элементы меню, доступные пользователю
        /// </summary>
        public override List<MenuItem> GetMenuItems(UserData userData)
        {
            //if (userData.UserRights.ConfigRight)
            //{
                var menuItems = new List<MenuItem>();
                var networkMenuItem = MenuItem.FromStandardMenuItem(StandardMenuItems.Network);
                networkMenuItem.Subitems.Add(new MenuItem(Localization.UseRussian ? "Diagnostic" : "Диагностика", "~/plugins/Network/Diagnostic.aspx"));
                networkMenuItem.Subitems.Add(new MenuItem(Localization.UseRussian ? "Events" : "События", "~/plugins/Network/Events.aspx"));
                menuItems.Add(networkMenuItem);

                return menuItems;
            /*}
            else
            {
                return null;
            }*/
        }
    }
}