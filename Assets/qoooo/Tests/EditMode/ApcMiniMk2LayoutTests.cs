using NUnit.Framework;
using qoooo.Midi.Apc;

namespace qoooo.Tests.EditMode
{
    public sealed class ApcMiniMk2LayoutTests
    {
        [TestCase(0, 7, 0)]
        [TestCase(7, 7, 7)]
        [TestCase(56, 0, 0)]
        [TestCase(63, 0, 7)]
        public void GridNotes_RoundTripWithApplicationCoordinates(int note, int row, int col)
        {
            Assert.That(ApcMiniMk2Layout.TryGetGridPosition(note, out var actualRow, out var actualCol), Is.True);
            Assert.That((actualRow, actualCol), Is.EqualTo((row, col)));
            Assert.That(ApcMiniMk2Layout.TryGetNote(row, col, out var restoredNote), Is.True);
            Assert.That(restoredNote, Is.EqualTo(note));
        }

        [Test]
        public void ProtocolConstants_IdentifyAllNineFaders()
        {
            Assert.That(ApcMiniMk2Constants.TryGetFaderIndex(48, out var first), Is.True);
            Assert.That(first, Is.EqualTo(0));
            Assert.That(ApcMiniMk2Constants.TryGetFaderIndex(56, out var master), Is.True);
            Assert.That(master, Is.EqualTo(8));
            Assert.That(ApcMiniMk2Constants.TryGetFaderButtonIndex(122, out var shift), Is.True);
            Assert.That(shift, Is.EqualTo(8));
        }
    }
}
