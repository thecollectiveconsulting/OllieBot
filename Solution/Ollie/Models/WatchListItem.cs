using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ollie.Models
{
    public class WatchListItemProperties
    {
        [JsonPropertyName("watchlistItemId")]
        public string WatchlistItemId { get; set; }

        // Different for every Watchlist => Dynamic
        [JsonPropertyName("itemsKeyValue")]
        public dynamic ItemsKeyValue { get; set; }

        public string ItemsKeyValueString 
        {
            get
            {
                return GetItemsKeyValueListOption();
            }
        }

        public List<string> WatchListFields 
        {
            get
            {
                return fields();
            }
        }

        public List<string> fields()
        {
            var fields = new List<string>();
            foreach (JProperty field in ItemsKeyValue)
            {
                fields.Add(field.Name);
            }

            return fields;
        }


        private string GetItemsKeyValueListOption()
        {
            var option = "";

            foreach (JProperty testprop in ItemsKeyValue)
            {
                option += $"{testprop.Value} ";
            }

            return option;
        }

        //private string itemsKeyValue;

        //[JsonPropertyName("itemsKeyValue")]
        //public string ItemsKeyValue
        //{
        //    get { return itemsKeyValue; }
        //    set 
        //    {
        //        foreach (JProperty testprop in ItemsKeyValue)
        //        {
        //            itemsKeyValue += $"{testprop.Value} ";
        //        }
        //    }
        //}


    }

    public class WatchListItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("properties")]
        public WatchListItemProperties Properties { get; set; }
    }


}
