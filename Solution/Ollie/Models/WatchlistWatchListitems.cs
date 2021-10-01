using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ollie.Models
{
    public class WatchlistWatchListitems
    {
        public WatchList WatchList { get; set; }
        public List<WatchListItem> WatchListItems { get; set; }
    }
}
