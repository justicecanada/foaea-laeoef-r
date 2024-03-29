﻿using FOAEA3.Model.Enums;
using FOAEA3.Model.Interfaces;
using System.Collections.Concurrent;

namespace FOAEA3.Model
{
    /// <summary>
    /// List of FoaEvents from the FoaMessages tablle
    /// </summary>
    /// <remarks>
    /// Note that the FoaEvents variable should be of type Dictionary<EventCode, FoaEventData>() but there is a limitation in the json conversion code in .Net Core 3.x that only 
    /// allow json conversion of Dictionary<string, object>() types (the first "type" must be string) so this was the workaround for supporting APIs with this kind of object.
    /// Hopefully, this limitation will be removed in future .Net Core versions and we can change this back to the way it should be.
    /// </remarks>
    public class FoaEventDataDictionary : IMessageList
    {
        public MessageDataList Messages { get; set; }

        public ConcurrentDictionary<string, FoaEventData> FoaEvents { get; set; }

        public FoaEventDataDictionary()
        {
            Messages = new MessageDataList();
            FoaEvents = new ConcurrentDictionary<string, FoaEventData>();
        }

        public FoaEventData this[EventCode code]
        {
            get => FoaEvents[((int)code).ToString()];
            set => FoaEvents[((int)code).ToString()] = value;
        }

        public bool ContainsKey(EventCode code)
        {
            return FoaEvents.ContainsKey(((int)code).ToString());
        }

    }
}
