using System.ComponentModel.DataAnnotations;
using com.jobsite.chat.Shared.Messaging;
using RabbitMQ.Client;

namespace com.jobsite.chat.Tests.Shared.Messaging;

public class RabbitMqConnectionTests
{
    private static RabbitMqOptions DiscreteOptions() => new()
    {
        Host = "rabbitmq",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/",
        RequestsQueue = "stock.requests",
        RepliesQueue = "stock.replies"
    };

    [Fact]
    public void BuildFactory_NoUri_UsesDiscreteHostAndNoTls()
    {
        RabbitMqOptions options = DiscreteOptions();

        ConnectionFactory factory = RabbitMqConnection.BuildFactory(options);

        Assert.Equal(options.Host, factory.HostName);
        Assert.Equal(options.Port, factory.Port);
        Assert.Equal(options.VirtualHost, factory.VirtualHost);
        Assert.Equal(options.UserName, factory.UserName);
        Assert.Equal(options.Password, factory.Password);
        Assert.False(factory.Ssl.Enabled);
    }

    [Fact]
    public void BuildFactory_NoUri_KeepsRecoveryEnabled()
    {
        RabbitMqOptions options = DiscreteOptions();

        ConnectionFactory factory = RabbitMqConnection.BuildFactory(options);

        Assert.True(factory.AutomaticRecoveryEnabled);
        Assert.True(factory.TopologyRecoveryEnabled);
    }

    [Fact]
    public void BuildFactory_AmqpsUri_UsesUriWithTls()
    {
        RabbitMqOptions options = DiscreteOptions() with
        {
            Uri = "amqps://u:p@example.cloudamqp.com/myvhost"
        };

        ConnectionFactory factory = RabbitMqConnection.BuildFactory(options);

        Assert.Equal("example.cloudamqp.com", factory.HostName);
        Assert.Equal(5671, factory.Port);
        Assert.Equal("myvhost", factory.VirtualHost);
        Assert.True(factory.Ssl.Enabled);
    }

    [Fact]
    public void BuildFactory_AmqpsUri_KeepsRecoveryEnabled()
    {
        RabbitMqOptions options = DiscreteOptions() with
        {
            Uri = "amqps://u:p@example.cloudamqp.com/myvhost"
        };

        ConnectionFactory factory = RabbitMqConnection.BuildFactory(options);

        Assert.True(factory.AutomaticRecoveryEnabled);
        Assert.True(factory.TopologyRecoveryEnabled);
    }

    [Fact]
    public void RabbitMqOptions_DefaultUri_IsEmpty()
    {
        RabbitMqOptions options = new();

        Assert.Equal(string.Empty, options.Uri);
    }

    [Fact]
    public void RabbitMqOptions_UriUnset_PassesValidation()
    {
        RabbitMqOptions options = DiscreteOptions();

        List<ValidationResult> results = [];
        bool valid = Validator.TryValidateObject(
            options, new ValidationContext(options), results, validateAllProperties: true);

        Assert.True(valid);
        Assert.Empty(results);
    }
}
