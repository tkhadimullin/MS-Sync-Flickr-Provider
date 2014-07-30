using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlickrNet;
using FlickrSyncProvider.Domain;

namespace FlickrSyncProvider.Extensions
{
    public static class MiscExtensions
    {
        public static string ListToString(this IEnumerable<MachineTag> tags)
        {
            var sb = new StringBuilder();
            foreach (var machineTag in tags.OrderBy(x => x.Ns).ThenBy(y => y.Predicate))
            {
                sb.AppendFormat("{0} ", machineTag);
            }
            return sb.ToString();
        }

        public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTime? UnixTimeStampToDateTime(this string unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            double value;
            if (double.TryParse(unixTimeStamp, out value))
            {
                return value.UnixTimeStampToDateTime();
            }
            return null;
        }

        public static DateTime UnixTimeStampToDateTime(this ulong unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            return ((double)unixTimeStamp).UnixTimeStampToDateTime();            
        }

        public static int ToUnixTimestamp(this DateTime dateTime)
        {
            if (dateTime == new DateTime(1, 1, 1))
                return 0;
            return (int)(dateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static string EnsureTrailingSlash(this string path)
        {
            return path.TrimEnd(new[] { '\\' }) + "\\";
        }

        public static Photoset ToPhotoset(this ContextSet contextSet)
        {
            return new Photoset
            {
                PhotosetId = contextSet.PhotosetId,
                Title = contextSet.Title
            };
        }
    }
}