using System;
using System.Linq;
using Microsoft.Synchronization.SimpleProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class FlickrMediaObjectTest
    {
        private readonly string[] _allowedTypes = {typeof (string).Name, typeof (UInt64).Name};

        [TestMethod]
        public void FlickrMediaObject_GetSchema()
        {
            var schema = FlickrSyncProvider.Domain.FlickrMediaObject.GetSchema();
            Assert.AreEqual(10, schema.CustomFields.Count());
            foreach (CustomFieldDefinition customField in schema.CustomFields)
            {
                Assert.IsTrue(_allowedTypes.Contains(customField.FieldType.Name));                
            }
            Assert.AreEqual(1, schema.IdentityRules.Count());
            Assert.AreEqual(2, schema.IdentityRules.First().IdentityFieldIds.Count());
        }
    }
}
