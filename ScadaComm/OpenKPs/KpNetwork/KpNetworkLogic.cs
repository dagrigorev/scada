/*
 * Copyright 2015 Mikhail Shiryaev
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
 * Module   : KpSms
 * Summary  : Device communication logic
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2015
 * 
 * Description
 * Device library for testing.
 */

using Scada.Comm.Channels;
using Scada.Data.Models;
using Scada.Network;
using System;
using System.Collections.Generic;
using static Scada.Data.Tables.SrezTableLight;

namespace Scada.Comm.Devices
{
    /// <summary>
    /// Device communication logic
    /// <para>Логика работы КП</para>
    /// </summary>
    public sealed class KpNetworkLogic : KPLogic
    {
        private static readonly Connection.TextStopCondition ReadStopCondition = 
            new Connection.TextStopCondition("OK");
        private Sniffer _sniffer;
        
        public KpNetworkLogic(int number)
            : base(number)
        {
            CanSendCmd = true;
            _sniffer = new Sniffer();
            _sniffer.OnPacketCatched += NewPacketCatched;

            List<TagGroup> tagGroups = new List<TagGroup>();
            TagGroup tagGroup = new TagGroup("Данные");
            tagGroup.KPTags.Add(new KPTag(1, "Исходный IP"));
            tagGroup.KPTags.Add(new KPTag(2, "Конечный IP"));
            tagGroup.KPTags.Add(new KPTag(3, "Количество пакетов"));
            tagGroups.Add(tagGroup);
            InitKPTags(tagGroups);
        }

        public override void OnCommLineStart()
        {
            base.OnCommLineStart();

            _sniffer.DoSniff();
        }

        public override void OnCommLineTerminate()
        {
            base.OnCommLineTerminate();
            _sniffer.Enabled = false;
        }

        public override void OnCommLineAbort()
        {
            base.OnCommLineAbort();
            _sniffer.Enabled = false;
        }

        public void NewPacketCatched(string source, string dest, uint count)
        {
            WriteToLog("Перехвачен новый пакет");
            SetCurData(0, Scada.Network.Utils.ToInt(source), 5);
            SetCurData(1, Scada.Network.Utils.ToInt(dest), 5);
            SetCurData(2, count, 5);
        }

        public override void Session()
        {
            base.Session();
            FinishRequest();
            CalcSessStats();
        }

        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);

            CalcCmdStats();
        }
    }
}
