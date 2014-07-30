using System.ComponentModel;
using FlickrNet;
using FlickrSyncProvider.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class MachineTagTest
    {
        private BindingList<PhotoInfoTag> _tags;
        
        [TestInitialize]
        public void Init()
        {
            _tags = new BindingList<PhotoInfoTag>()
                {
                    new PhotoInfoTag {IsMachineTag = false, Raw = "not_machine_tag"},
                    new PhotoInfoTag {IsMachineTag = true, Raw = "mtag:key=value"},
                    new PhotoInfoTag {IsMachineTag = true, Raw = "mtag:key=value1"},
                };     
        }

        [TestMethod]
        public void MachineTag_ParseMachineTags()
        {
            var tags =
                MachineTag.ParseMachineTags(
                    "file:md5sum=5d9ff10c14bb31c1ceb2654853ab99c1 file:sha1sig=c1fb541e68df00f7440ddb878feccfb06f85d8e8");
            Assert.AreEqual(2, tags.Count);
            Assert.AreEqual("file", tags[0].Ns);
            Assert.AreEqual("md5sum", tags[0].Predicate);
            Assert.AreEqual("5d9ff10c14bb31c1ceb2654853ab99c1", tags[0].Value);

            var quotedTags =
                MachineTag.ParseMachineTags(
                    "file:md5sum=\"5d9ff10c14bb31c1ceb2654853ab99c1\"");
            Assert.AreEqual("5d9ff10c14bb31c1ceb2654853ab99c1", quotedTags[0].Value);
        }

        [TestMethod]
        public void MachineTag_ToString()
        {
            var mt = new MachineTag("namespace", "predicate", "value");
            Assert.AreEqual("namespace:predicate=value", mt.ToString());
        }

        [TestMethod]
        public void MachineTag_ParseTags()
        {

            var machineTags = MachineTag.ParseTags(_tags);
            Assert.AreEqual(2, machineTags.Count);
            Assert.AreEqual("mtag:key=value", machineTags[0].ToString());
            Assert.AreEqual("mtag:key=value1", machineTags[1].ToString());
        }

        [TestMethod]
        public void MachineTag_AsString()
        {
            var machineTags = MachineTag.ParseTags(_tags);
            Assert.AreEqual("mtag:key=value mtag:key=value1", MachineTag.AsString(machineTags));
        }
    }
}
