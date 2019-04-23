/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : PlgWebPage
 * Summary  : Web page view specification
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;

namespace Scada.Web.Plugins.ReactRenderer
{
    /// <summary>
    /// Web page view specification
    /// <para>Спецификация представления веб-страницы</para>
    /// </summary>
    public class ReactRendererViewSpec : ViewSpec
    {
        /// <summary>
        /// Получить код типа представления
        /// </summary>
        public override string TypeCode
        {
            get
            {
                return "rrv";
            }
        }

        /// <summary>
        /// Получить ссылку на иконку типа представлений
        /// </summary>
        public override string IconUrl
        {
            get
            {
                return "~/plugins/ReactRenderer/assets/images/rricon.png";
            }
        }
        
        /// <summary>
        /// Получить тип представления
        /// </summary>
        public override Type ViewType
        {
            get
            {
                return typeof(ReactRendererView);
            }
        }


        /// <summary>
        /// Получить ссылку на представление с заданным идентификатором
        /// </summary>
        public override string GetUrl(int viewID)
        {
            return "~/plugins/ReactRenderer/Root.aspx?viewID=" + viewID;
        }
    }
}