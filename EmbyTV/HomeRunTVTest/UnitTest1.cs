using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeRunTV.Configuration;
using HomeRunTV.TunerHelpers;
using MediaBrowser.Common.Net;


using HomeRunTVTest.Interfaces;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeRunTVTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var username = "user";
            var password = "pass";
            string test = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";
            Debug.WriteLine(test);
            Assert.AreEqual("http://192.168.2.238:8866", test);
            return;
        }
        [TestMethod]
        private void GetHostFromUrl()
        {
            var username = "user";
            var password = "pass";
            string test = "{\"username\":\""+username+"\",\"password\":\""+password+"\"}";
            Debug.WriteLine(test);
            Assert.AreEqual("http://192.168.2.238:8866", test);
            return;
        }

    }


}
