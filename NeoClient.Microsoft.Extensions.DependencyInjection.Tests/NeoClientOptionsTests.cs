using Microsoft.Extensions.DependencyInjection;
using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NeoClient.Microsoft.Extensions.DependencyInjection.Options;
using Xunit;

namespace NeoClient.Microsoft.Extensions.DependencyInjection.Tests
{
    public class NeoClientConfigurationTest
    {
        [Fact]
        public void ShouldLoadOptionsCorrectly()
        {
            // Arrange
            var configurationRoot = GetConfigurationRoot();
            var uri = "bolt://localhost:7687";
            var userName = "user";
            var password = "password";
            var stripHyphens = true;

            string sectionName = null;

            // Act
            var options = configurationRoot.GetSection(sectionName ?? "NeoClient").Get<NeoClientOptions>();

            // Assert
            options.Uri.Should().Be(uri);
            options.UserName.Should().Be(userName);
            options.Password.Should().Be(password);
            options.StripHyphens.Should().Be(stripHyphens);
        }


        private IConfigurationRoot GetConfigurationRoot()
            => new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .Build();
    }
}
