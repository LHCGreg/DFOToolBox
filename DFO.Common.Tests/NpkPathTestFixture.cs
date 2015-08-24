using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DFO.Common.Tests
{
    [TestFixture]
    public class NpkPathTest
    {
        [Test]
        public void TestGetNpkName()
        {
            NpkPath testPath = "Interface/Emoticon/Against.img";
            Assert.That(testPath.GetImageNpkName(), Is.EqualTo("sprite_Interface_Emoticon.npk"));

            testPath = "sprite/Interface/Emoticon/Against.img";
            Assert.That(testPath.GetImageNpkName(), Is.EqualTo("sprite_Interface_Emoticon.npk"));

            testPath = "equip/armor/cloth_touch.wav";
            Assert.That(testPath.GetSoundNpkName(), Is.EqualTo("sounds_equip_armor.npk"));

            testPath = "sounds/equip/armor/cloth_touch.wav";
            Assert.That(testPath.GetSoundNpkName(), Is.EqualTo("sounds_equip_armor.npk"));

            testPath = "/";
            Assert.That(testPath.GetImageNpkName(), Is.EqualTo("sprite.npk"));
            Assert.That(testPath.GetSoundNpkName(), Is.EqualTo("sounds.npk"));
        }
    }
}
