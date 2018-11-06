using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fifo.Test
{
    [TestClass]
    public class ThrottleHelperTest
    {
        private UserThrottleParam sammy = new UserThrottleParam()
        {
            UserName = "sammy",
            RoutesFrequencies = new Dictionary<string, int>()
            {
                { "r1", 5 }, { "r2", 10 }
            }
        };

        private UserThrottleParam pepe = new UserThrottleParam()
        {
            UserName = "pepe",
            RoutesFrequencies = new Dictionary<string, int>()
            {
                { "r1", 15 }, { "r3", 2 }, { "r4", 500 }
            }
        };

        [TestMethod]
        public void should_accept_one_request()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);

            h.Add("sammy", "r1");
            Assert.IsTrue(h.Check("sammy"));
        }

        [TestMethod]
        public void should_accept_under_limitation_request()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);

            for (int i = 0; i < 400; i++)
            {
                h.Add("pepe", "r4");
            }

            Assert.IsTrue(h.Check("pepe"));
        }

        [TestMethod]
        public void should_be_ok_for_unknow_user()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);

            h.Add("riton", "r1");
            Assert.IsTrue(h.Check("riton"));
        }

        [TestMethod]
        public void should_reject_user_for_too_much_request()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);
            for (int i = 0; i < 6; i++)
            {
                h.Add("sammy", "r1");
            }
            Assert.IsFalse(h.Check("sammy"));
        }

        [TestMethod]
        public void should_accept_user_with_another_rejected()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);
            for (int i = 0; i < 6; i++)
            {
                h.Add("sammy", "r1");
            }
            for (int i = 0; i < 10; i++)
            {
                h.Add("pepe", "r1");
            }
            Assert.IsFalse(h.Check("sammy"));
            Assert.IsTrue(h.Check("pepe"));
        }

        [TestMethod]
        public void should_accept_user_for_unknow_route()
        {
            ThrottleHelper h = new ThrottleHelper(10000, sammy, pepe);
            for (int i = 0; i < 100; i++)
            {
                h.Add("sammy", "r5");
            }
            Assert.IsTrue(h.Check("sammy"));
        }

        [TestMethod]
        public void should_accept_slow_requests_cadency()
        {
            ThrottleHelper h = new ThrottleHelper(500, sammy, pepe);
            for (int i = 0; i < 10; i++)
            {
                h.Add("sammy", "r1");
                Thread.Sleep(100);
            }
            Assert.IsTrue(h.Check("sammy"));
        }

        [TestMethod]
        public void should_not_be_too_slow()
        {
            ThrottleHelper h = new ThrottleHelper(1, sammy, pepe);
            for (int i = 0; i < 10000; i++)
            {
                h.Add("sammy", "r1");
            }
            for (int i = 0; i < 5000; i++)
            {
                h.Add("sammy", "r2");
            }
            for (int i = 0; i < 20000; i++)
            {
                h.Add("pepe", "r1");
            }
            DateTime before = DateTime.Now;
            h.Add("sammy", "r1");
            h.Check("sammy");
            DateTime after = DateTime.Now;
            Assert.IsTrue((after - before).TotalMilliseconds < 10);
        }
    }
}
