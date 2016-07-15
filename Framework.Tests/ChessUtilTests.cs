using NUnit.Framework;
using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Tests
{
    [TestFixture()]
    public class ChessUtilTests
    {
        [Test()]
        [TestCase(21, 28, 24, true)]
        [TestCase(21, 24, 28, false)]
        [TestCase(27, 24, 21, false)]
        [TestCase(21, 28, 34, false)]
        [TestCase(21, 71, 41, true)]
        [TestCase(71, 91, 81, true)]
        [TestCase(21, 54, 43, true)]
        [TestCase(21, 54, 44, false)]
        [TestCase(22, 55, 31, false)]
        public void IsBeteenTest(int first, int second, int pos, bool expected)
        {
            Assert.AreEqual(expected, ChessUtil.IsBeteen(first, second, pos));
            Assert.AreEqual(expected, ChessUtil.IsBeteen(second, first, pos));  //Revers should be same in all cases
            
        }
    }
}