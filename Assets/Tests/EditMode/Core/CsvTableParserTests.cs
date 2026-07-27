using NUnit.Framework;
using WuxiaRoguelite.Application.Configuration;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class CsvTableParserTests
    {
        [Test]
        public void QuotedCommaAndCrLfAreParsedWithoutLosingText()
        {
            const string csv = "id,display_name,description\r\nskill_sword_qi,剑气诀,\"命中三次后, 追加剑气\"\r\n";

            CsvTable table = new CsvTableParser().Parse(csv);

            Assert.That(table.Rows.Count, Is.EqualTo(1));
            Assert.That(table.Rows[0]["id"], Is.EqualTo("skill_sword_qi"));
            Assert.That(table.Rows[0]["description"], Is.EqualTo("命中三次后, 追加剑气"));
        }

        [Test]
        public void InconsistentColumnCountIsRejected()
        {
            const string csv = "id,name\nplayer_wuxia";

            Assert.Throws<CsvFormatException>(() => new CsvTableParser().Parse(csv));
        }
    }
}
