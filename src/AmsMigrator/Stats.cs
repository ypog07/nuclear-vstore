using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AmsMigrator
{
    public static class Stats
    {
        public static object Lock = new object();
        public static ConcurrentDictionary<string, long> Collector = new ConcurrentDictionary<string, long>();
    }
}
