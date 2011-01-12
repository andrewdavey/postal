using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;

namespace Postal
{
    public class ParserUtilsTests
    {
        [Fact]
        public void Can_parse_header()
        {
            var input = "hello: world";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            using (var reader = new StringReader(input))
            {
                ParserUtils.ParseHeaders(reader, (key, value) => headers.Add(key, value));
            }

            Assert.Equal(headers["hello"], "world");
        }


    }
}
