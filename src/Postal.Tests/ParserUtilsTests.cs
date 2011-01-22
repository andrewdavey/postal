using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;
using Should;

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

        KeyValuePair<string, string> ParseHeader(string line)
        {
            KeyValuePair<string, string> header = default(KeyValuePair<string,string>);
            using (var reader = new StringReader(line))
            {
                ParserUtils.ParseHeaders(reader, (key, value) => header = new KeyValuePair<string, string>(key, value));
            }
            return header;
        }
    }
}
