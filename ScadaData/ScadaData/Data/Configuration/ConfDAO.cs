/*
 * Copyright 2017 Mikhail Shiryaev
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
 * Module   : ScadaData
 * Summary  : Access to the configuration database
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2017
 */

using Scada.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scada.Data.Configuration
{
    /// <summary>
    /// Access to the configuration database
    /// <para>Доступ к данным базы конфигурации</para>
    /// </summary>
    public class ConfDAO
    {
        /// <summary>
        /// Разделитель значений внутри поля таблицы
        /// </summary>
        protected static readonly char[] FieldSeparator = new char[] { ';' };

        /// <summary>
        /// Таблицы базы конфигурации
        /// </summary>
        protected BaseTables baseTables;


        /// <summary>
        /// Конструктор, ограничивающий создание объекта без параметров
        /// </summary>
        protected ConfDAO()
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ConfDAO(BaseTables baseTables)
        {
            this.baseTables = baseTables ?? throw new ArgumentNullException("baseTables");
        }


        /// <summary>
        /// Получить наименования произвольных сущностей, ключ - идентификатор или номер сущности
        /// </summary>
        protected SortedList<int, string> GetNames(DataTable table)
        {
            SortedList<int, string> names = new SortedList<int, string>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
            {
                names.Add((int)row[0], (string)row["Name"]);
            }

            return names;
        }


        /// <summary>
        /// Получить свойства входных каналов, упорядоченные по возрастанию номеров каналов
        /// </summary>
        public List<InCnlProps> GetInCnlProps()
        {
            DataTable tblInCnl = baseTables.InCnlTable;
            DataView viewObj = baseTables.ObjTable.DefaultView;
            DataView viewKP = baseTables.KPTable.DefaultView;
            DataView viewParam = baseTables.ParamTable.DefaultView;
            DataView viewFormat = baseTables.FormatTable.DefaultView;
            DataView viewUnit = baseTables.UnitTable.DefaultView;

            // установка сортировки для последующего поиска строк
            viewObj.Sort = "ObjNum";
            viewKP.Sort = "KPNum";
            viewParam.Sort = "ParamID";
            viewFormat.Sort = "FormatID";
            viewUnit.Sort = "UnitID";

            // создание и заполнение списка
            List<InCnlProps> cnlPropsList = new List<InCnlProps>(tblInCnl.Rows.Count);

            foreach (DataRow inCnlRow in tblInCnl.Rows)
            {
                InCnlProps cnlProps = new InCnlProps
                {

                    // определение свойств, не использующих внешних ключей
                    CnlNum = (int)inCnlRow["CnlNum"],
                    CnlName = (string)inCnlRow["Name"],
                    CnlTypeID = (int)inCnlRow["CnlTypeID"],
                    ObjNum = (int)inCnlRow["ObjNum"],
                    KPNum = (int)inCnlRow["KPNum"],
                    Signal = (int)inCnlRow["Signal"],
                    FormulaUsed = (bool)inCnlRow["FormulaUsed"],
                    Formula = (string)inCnlRow["Formula"],
                    Averaging = (bool)inCnlRow["Averaging"],
                    ParamID = (int)inCnlRow["ParamID"],
                    FormatID = (int)inCnlRow["FormatID"],
                    UnitID = (int)inCnlRow["UnitID"],
                    CtrlCnlNum = (int)inCnlRow["CtrlCnlNum"],
                    EvEnabled = (bool)inCnlRow["EvEnabled"],
                    EvSound = (bool)inCnlRow["EvSound"],
                    EvOnChange = (bool)inCnlRow["EvOnChange"],
                    EvOnUndef = (bool)inCnlRow["EvOnUndef"],
                    LimLowCrash = (double)inCnlRow["LimLowCrash"],
                    LimLow = (double)inCnlRow["LimLow"],
                    LimHigh = (double)inCnlRow["LimHigh"],
                    LimHighCrash = (double)inCnlRow["LimHighCrash"]
                };

                // определение наименования объекта
                int objRowInd = viewObj.Find(cnlProps.ObjNum);
                if (objRowInd >= 0)
                    cnlProps.ObjName = (string)viewObj[objRowInd]["Name"];

                // определение наименования КП
                int kpRowInd = viewKP.Find(cnlProps.KPNum);
                if (kpRowInd >= 0)
                    cnlProps.KPName = (string)viewKP[kpRowInd]["Name"];

                // определение наименования параметра и имени файла значка
                int paramRowInd = viewParam.Find(cnlProps.ParamID);
                if (paramRowInd >= 0)
                {
                    DataRowView paramRowView = viewParam[paramRowInd];
                    cnlProps.ParamName = (string)paramRowView["Name"];
                    cnlProps.IconFileName = (string)paramRowView["IconFileName"];
                }

                // определение формата вывода
                int formatRowInd = viewFormat.Find(inCnlRow["FormatID"]);
                if (formatRowInd >= 0)
                {
                    DataRowView formatRowView = viewFormat[formatRowInd];
                    cnlProps.ShowNumber = (bool)formatRowView["ShowNumber"];
                    cnlProps.DecDigits = (int)formatRowView["DecDigits"];
                }

                // определение размерностей
                int unitRowInd = viewUnit.Find(cnlProps.UnitID);
                if (unitRowInd >= 0)
                {
                    DataRowView unitRowView = viewUnit[unitRowInd];
                    cnlProps.UnitName = (string)unitRowView["Name"];
                    cnlProps.UnitSign = (string)unitRowView["Sign"];
                    string[] unitArr = cnlProps.UnitArr =
                        cnlProps.UnitSign.Split(FieldSeparator, StringSplitOptions.None);
                    for (int j = 0; j < unitArr.Length; j++)
                        unitArr[j] = unitArr[j].Trim();
                    if (unitArr.Length == 1 && unitArr[0] == "")
                        cnlProps.UnitArr = null;
                }

                cnlPropsList.Add(cnlProps);
            }

            return cnlPropsList;
        }

        /// <summary>
        /// Получить свойства каналов управления, упорядоченные по возрастанию номеров каналов
        /// </summary>
        public List<CtrlCnlProps> GetCtrlCnlProps()
        {
            DataTable tblCtrlCnl = baseTables.CtrlCnlTable;
            DataView viewObj = baseTables.ObjTable.DefaultView;
            DataView viewKP = baseTables.KPTable.DefaultView;
            DataView viewCmdVal = baseTables.CmdValTable.DefaultView;

            // установка сортировки для последующего поиска строк
            viewObj.Sort = "ObjNum";
            viewKP.Sort = "KPNum";
            viewCmdVal.Sort = "CmdValID";

            // создание и заполнение списка
            List<CtrlCnlProps> ctrlCnlPropsList = new List<CtrlCnlProps>(tblCtrlCnl.Rows.Count);

            foreach (DataRow ctrlCnlRow in tblCtrlCnl.Rows)
            {
                CtrlCnlProps ctrlCnlProps = new CtrlCnlProps
                {

                    // определение свойств, не использующих внешних ключей
                    CtrlCnlNum = (int)ctrlCnlRow["CtrlCnlNum"],
                    CtrlCnlName = (string)ctrlCnlRow["Name"],
                    CmdTypeID = (int)ctrlCnlRow["CmdTypeID"],
                    ObjNum = (int)ctrlCnlRow["ObjNum"],
                    KPNum = (int)ctrlCnlRow["KPNum"],
                    CmdNum = (int)ctrlCnlRow["CmdNum"],
                    CmdValID = (int)ctrlCnlRow["CmdValID"],
                    FormulaUsed = (bool)ctrlCnlRow["FormulaUsed"],
                    Formula = (string)ctrlCnlRow["Formula"],
                    EvEnabled = (bool)ctrlCnlRow["EvEnabled"]
                };

                // определение наименования объекта
                int objRowInd = viewObj.Find(ctrlCnlProps.ObjNum);
                if (objRowInd >= 0)
                    ctrlCnlProps.ObjName = (string)viewObj[objRowInd]["Name"];

                // определение наименования КП
                int kpRowInd = viewKP.Find(ctrlCnlProps.KPNum);
                if (kpRowInd >= 0)
                    ctrlCnlProps.KPName = (string)viewKP[kpRowInd]["Name"];

                // определение значений команды
                int cmdValInd = viewCmdVal.Find(ctrlCnlProps.CmdValID);
                if (cmdValInd >= 0)
                {
                    DataRowView cmdValRowView = viewCmdVal[cmdValInd];
                    ctrlCnlProps.CmdValName = (string)cmdValRowView["Name"];
                    ctrlCnlProps.CmdVal = (string)cmdValRowView["Val"];
                    string[] cmdValArr = ctrlCnlProps.CmdValArr =
                        ctrlCnlProps.CmdVal.Split(FieldSeparator, StringSplitOptions.None);
                    for (int j = 0; j < cmdValArr.Length; j++)
                        cmdValArr[j] = cmdValArr[j].Trim();
                    if (cmdValArr.Length == 1 && cmdValArr[0] == "")
                        ctrlCnlProps.CmdValArr = null;
                }

                ctrlCnlPropsList.Add(ctrlCnlProps);
            }

            return ctrlCnlPropsList;
        }

        /// <summary>
        /// Получить наименования объектов, ключ - номер объекта
        /// </summary>
        public SortedList<int, string> GetObjNames()
        {
            return GetNames(baseTables.ObjTable);
        }

        /// <summary>
        /// Получить наименования КП, ключ - номер КП
        /// </summary>
        public SortedList<int, string> GetKPNames()
        {
            return GetNames(baseTables.KPTable);
        }

        /// <summary>
        /// Получить свойства статусов входных каналов, ключ - статус
        /// </summary>
        public SortedList<int, CnlStatProps> GetCnlStatProps()
        {
            DataTable tblEvType = baseTables.EvTypeTable;
            _ = tblEvType.Rows.Count;
            SortedList<int, CnlStatProps> cnlStatPropsList = new SortedList<int, CnlStatProps>(tblEvType.Rows.Count);

            foreach (DataRow row in tblEvType.Rows)
            {
                CnlStatProps cnlStatProps = new CnlStatProps((int)row["CnlStatus"])
                {
                    Color = (string)row["Color"],
                    Name = (string)row["Name"]
                };

                cnlStatPropsList.Add(cnlStatProps.Status, cnlStatProps);
            }

            return cnlStatPropsList;
        }
    }
}
