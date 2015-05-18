using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DFO.Common.Tests
{
    [TestFixture]
    public class ScriptPvfPathTest
    {
        [Test]
        public void TestScriptPvfPath()
        {
            ScriptPvfPath path1 = new ScriptPvfPath("abc/def.i");
            ScriptPvfPath path2 = new ScriptPvfPath("///ABc///def.I//");
            ScriptPvfPath pathX = new ScriptPvfPath("///ABc///def.I//a");
            ScriptPvfPath pathY = new ScriptPvfPath("abc/def.i/a");
            ScriptPvfPath pathXBackslashes = new ScriptPvfPath(@"\\AbC/\/\def.i\a\");
            ScriptPvfPath slashes = new ScriptPvfPath(@"//\");
            ScriptPvfPath empty = new ScriptPvfPath("");

            Assert.That(path1, Is.EqualTo(path2));
            Assert.That(path1, Is.Not.EqualTo(pathX));
            Assert.That(path1, Is.Not.EqualTo(pathY));
            Assert.That(pathX, Is.EqualTo(pathXBackslashes));
            Assert.That(slashes, Is.EqualTo(empty));

            Assert.That(path1.GetHashCode(), Is.EqualTo(path2.GetHashCode()));
        }

        [Test]
        public void TestGetPathComponents()
        {
            ScriptPvfPath path1 = new ScriptPvfPath("///ABc///def.I//");
            // This is actually a stricter test than it should be. The path components are not
            // guaranteed to be the same case as in the original string. But the test is easier to write this way.
            Assert.That(path1.GetPathComponents(), Is.EqualTo(new List<ScriptPvfPath>() { "ABc", "def.I" }));

            ScriptPvfPath emptyPath = new ScriptPvfPath("/");
            Assert.That(emptyPath.GetPathComponents(), Is.EqualTo(new List<ScriptPvfPath>()));
        }

        [Test]
        public void TestGetContainingDirectory()
        {
            ScriptPvfPath path1 = new ScriptPvfPath(@"\\AbC/\/\def.i\a\");
            Assert.That(path1.GetContainingDirectory(), Is.EqualTo(new ScriptPvfPath(@"abc\\def.i\\")));

            ScriptPvfPath empty = new ScriptPvfPath("/");
            Assert.That(empty.GetContainingDirectory(), Is.EqualTo(empty));
        }

        [Test]
        public void TestGetFileName()
        {
            ScriptPvfPath path1 = new ScriptPvfPath(@"\\abc/\/def.i/a/");
            Assert.That(path1.GetFileName(), Is.EqualTo(new ScriptPvfPath(@"a")));

            ScriptPvfPath path2 = new ScriptPvfPath(@"abc/");
            Assert.That(path2.GetFileName(), Is.EqualTo(new ScriptPvfPath(@"abc")));

            ScriptPvfPath path3 = new ScriptPvfPath(@"");
            Assert.That(path3.GetFileName(), Is.EqualTo(new ScriptPvfPath(@"")));
        }

        //[Test]
        //public void TestScriptPvfPathPerf()
        //{
        //    const int numIterations = 1000000;
        //    TimeSpan timeAllowed = new TimeSpan(0, 0, 0, 0, 500);
        //    const string testPath = "equipment/character/gunner/weapon/bowgun/aqr.equ";

        //    Stopwatch timer = new Stopwatch();
        //    timer.Start();
        //    for (int i = 0; i < numIterations; i++)
        //    {
        //        ScriptPvfPath path = new ScriptPvfPath(testPath);
        //    }
        //    timer.Stop();

        //    Console.WriteLine("Time taken: {0}; Target time: {1}", timer.Elapsed, timeAllowed);

        //    // Gives an "inconclusive" result if the target time wasn't met. Maybe the machine was under
        //    // heavy load.
        //    NUnit.Framework.Assume.That(timer.Elapsed, Is.LessThan(timeAllowed));
        //}

        [Test]
        public void TestCombine()
        {
            ScriptPvfPath testPath = new ScriptPvfPath("abc/def/xyz.equ");
            ScriptPvfPath path1 = new ScriptPvfPath(ScriptPvfPath.Combine("abc/def", "xyz.equ"));
            ScriptPvfPath path2 = new ScriptPvfPath(ScriptPvfPath.Combine("/abc/def/", "xyz.equ"));
            ScriptPvfPath path3 = new ScriptPvfPath(ScriptPvfPath.Combine("", "abc//def/xyz.equ"));
            Assert.That(path1, Is.EqualTo(testPath));
            Assert.That(path2, Is.EqualTo(testPath));
            Assert.That(path3, Is.EqualTo(testPath));
        }

        [Test]
        public void TestCombineMember()
        {
            ScriptPvfPath testPath = new ScriptPvfPath("abc/def/xyz.equ");
            ScriptPvfPath path1 = new ScriptPvfPath("abc");
            ScriptPvfPath path2 = new ScriptPvfPath("def/xyz.equ");
            Assert.That(testPath, Is.EqualTo(ScriptPvfPath.Combine(path1, path2)));
            ScriptPvfPath empty = new ScriptPvfPath(@"\");
            Assert.That(ScriptPvfPath.Combine(empty, empty), Is.EqualTo(empty));
            Assert.That(ScriptPvfPath.Combine(path1, empty), Is.EqualTo(path1));
            Assert.That(ScriptPvfPath.Combine(empty, path2), Is.EqualTo(path2));
        }
    }
}
