using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Postal.AspNetCore;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Xunit;

namespace Postal
{
    public class ServiceCollectionExtensionTests
    {
        [Theory]
        [InlineData("smtp://username:password@localhost?enableSsl=true&fromAddress=test@test.com", "username", "password", "localhost", null, true, "test@test.com")]
        [InlineData("smtp://username:password@localhost:1234?enableSsl=true&fromAddress=test@test.com", "username", "password", "localhost", 1234, true, "test@test.com")]
        public void Connection_string_is_parsed(string connectionString, string userName, string password, string host, int? port, bool enableSsl, string fromAddress)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddPostal(connectionString);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var emailServiceOptions = serviceProvider.GetRequiredService<IOptions<EmailServiceOptions>>().Value;

            Assert.Equal(emailServiceOptions.UserName, userName);
            Assert.Equal(emailServiceOptions.Password, password);
            Assert.Equal(emailServiceOptions.Host, host);
            Assert.Equal(emailServiceOptions.Port, port);
            Assert.Equal(emailServiceOptions.EnableSSL, enableSsl);
            Assert.Equal(emailServiceOptions.FromAddress, fromAddress);
        }
    }
}
