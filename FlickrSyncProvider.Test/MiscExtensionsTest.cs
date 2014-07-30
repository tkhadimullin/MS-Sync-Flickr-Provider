using System;
using System.Collections.Generic;
using System.Globalization;
using FlickrSyncProvider.Domain;
using FlickrSyncProvider.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class MiscExtensionsTest
    {
        [TestMethod]
        public void MiscExtensions_ListToString()
        {
            var tags = new List<MachineTag>()
                {
                    new MachineTag("ns1", "predicate1", "value1"),
                    new MachineTag("ns2", "predicate2", "value2"),
                    new MachineTag("ns3", "predicate3", "value3"),
                };
            var tagsAsString = tags.ListToString();
            Assert.AreEqual("ns1:predicate1=value1 ns2:predicate2=value2 ns3:predicate3=value3 ", tagsAsString);
        }

        [TestMethod]
        public void MiscExtensions_UnixTimeStampToDateTime()
        {
            var now = DateTime.Now;
            int intTimestamp = now.ToUnixTimestamp();
            string strTimestamp = intTimestamp.ToString(CultureInfo.InvariantCulture);
            var fromStrTimestamp = strTimestamp.UnixTimeStampToDateTime();
            Assert.AreEqual(now.ToLongTimeString(), ((DateTime)fromStrTimestamp).ToLongTimeString());

            var unparsableStr = "this is supposed to be digits only. so will fail";
            var dateRepresentation = unparsableStr.UnixTimeStampToDateTime();
            Assert.IsNull(dateRepresentation);
        }

        [TestMethod]
        public void MiscExtensions_ToUnixTimestamp()
        {
            DateTime dt = new DateTime();
            var res = dt.ToUnixTimestamp();
            Assert.AreEqual(0, res);
        }

        [TestMethod]
        public void MiscExtensions_EnsureTrailingSlash()
        {
            var pathWSlash = @"root\level1\level2\";
            var pathWOSlash = @"root\level1\level2";
            Assert.AreEqual(pathWSlash.EnsureTrailingSlash(), pathWOSlash.EnsureTrailingSlash());
        }
    }
}
