using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FlickrNet;

namespace FlickrSyncProvider.Domain
{
    public class MachineTag
    {
        public string Ns { get; set; }
        public string Predicate { get; set; }
        public string Value { get; set; }

        public MachineTag(string ns, string predicate, string value)
        {
            Ns = ns;
            Predicate = predicate;
            Value = value;
        }

        public override string ToString()
        {
            var preparedValue = Value;
            if (preparedValue.Contains(" "))
                preparedValue = "\"" + preparedValue + "\"";

            return string.Format("{0}:{1}={2}", Ns, Predicate, preparedValue);
        }

        public static List<MachineTag> ParseMachineTags(string tags)
        {
            var result = new List<MachineTag>();
            foreach (var machineTag in Regex.Split(tags, @"\s"))
            {
                var matches = Regex.Matches(machineTag, @"^([0-9a-zA-Z]+):([0-9a-zA-Z]+)=(.+)$");
                if (matches.Count == 0) continue;
                if (matches[0].Groups.Count < 4) continue;
                result.Add(new MachineTag(matches[0].Groups[1].Value, matches[0].Groups[2].Value, matches[0].Groups[3].Value.Trim(new[] { '"' })));
            }
            return result;
        }

        public static List<MachineTag> ParseTags(Collection<PhotoInfoTag> tags)
        {
            var result = new List<MachineTag>();
            foreach (var tag in tags.Where(t => t.IsMachineTag))
            {
                result.AddRange(ParseMachineTags(tag.Raw));
            }
            return result;
        }

        public static string AsString(IEnumerable<MachineTag> tags)
        {
            var sb = new StringBuilder();
            foreach (var machineTag in tags)
            {
                sb.Append(machineTag);
                sb.Append(" ");
            }
            return sb.ToString().TrimEnd(' ');
        }
    }
}