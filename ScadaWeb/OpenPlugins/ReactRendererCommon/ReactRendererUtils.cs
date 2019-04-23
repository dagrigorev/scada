using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scada.Web.Plugins.ReactRenderer
{
    public class ReactRendererUtils
    {
        public static void OnStart() {
            ReactConfig.Configure();
        }
    }
}
