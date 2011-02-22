using System.Collections.Generic;
using System.IO;
using Should;
using Xunit;

namespace Postal
{
    public class ParserUtilsTests
    {
        [Fact]
        public void Can_parse_header()
        {
            var header= ParseHeader("hello: world");
            header.Key.ShouldEqual("hello");
            header.Value.ShouldEqual("world");
        }

        [Fact]
        public void White_space_is_removed()
        {
            var header = ParseHeader(" hello :   world  ");
            header.Key.ShouldEqual("hello");
            header.Value.ShouldEqual("world");
        }

        [Fact]
        public void Skips_initial_blank_lines() 
        {
            var header = ParseHeader("\n\nfirst: test");
            header.Key.ShouldEqual("first");
            header.Value.ShouldEqual("test");
        }

        [Fact]
        public void Can_parse_header_with_hyphen()
        {
            var header = ParseHeader("reply-to: foo@test.com");
            header.Key.ShouldEqual("reply-to");
            header.Value.ShouldEqual("foo@test.com");
        }

        KeyValuePair<string, string> ParseHeader(string line)
        {
            var header = default(KeyValuePair<string,string>);
            using (var reader = new StringReader(line))
            {
                ParserUtils.ParseHeaders(reader, (key, value) => header = new KeyValuePair<string, string>(key, value));
            }
            return header;
        }
    }
}
