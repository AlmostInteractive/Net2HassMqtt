using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NoeticTools.Net2HassMqtt.Configuration;
using NoeticTools.Net2HassMqtt.Entities.Framework;
using NoeticTools.Net2HassMqtt.Exceptions;
using NoeticTools.Net2HassMqtt.Mqtt;
using NoeticTools.Net2HassMqtt.Mqtt.Topics;


namespace NoeticTools.Net2HassMqtt.Entities;

internal abstract class EventEntityBase<T> : EntityBase<T>
    where T : EntityConfigBase {
    private readonly Delegate _eventHandlerDelegate;
    private readonly HaEvent _eventPublisher;
    private readonly EventInfo _eventInfo;
    private static SemaphoreSlim _lock;

    protected EventEntityBase(T config, string entityUniqueId, string deviceNodeId, INet2HassMqttClient mqttClient, ILogger logger)
        : base(config, entityUniqueId, deviceNodeId, mqttClient, logger) {
        (_eventPublisher, _eventInfo) = GetModelEventInfo();
        var type = _eventInfo.EventHandlerType!;
        _lock = new SemaphoreSlim(1);
        _eventHandlerDelegate = Delegate.CreateDelegate(type, this, config.PulseMode
                                                            ? nameof(OnPulseEvent)
                                                            : nameof(OnEvent));
    }

    public override Task StartAsync() {
        _eventInfo.AddEventHandler(_eventPublisher, _eventHandlerDelegate);
        return Task.CompletedTask;
    }

    public override Task StopAsync() {
        _eventInfo.RemoveEventHandler(_eventPublisher, _eventHandlerDelegate);
        return Task.CompletedTask;
    }

    internal void OnEvent(object sender, HaEvent.DictEventArgs dictEventArgs) {
        var topic = new TopicBuilder().WithComponent(Config.MqttTopicComponent)
                                      .WithNodeId(DeviceNodeId)
                                      .WithObjectId(Config.EntityNodeId);
        // ReSharper disable once UseDiscardAssignment
        var _ = MqttClient.PublishStatusAsync(topic, dictEventArgs.Arguments);
    }

    internal async void OnPulseEvent(object sender, HaEvent.DictEventArgs dictEventArgs) {
        await _lock.WaitAsync();
        Console.Write("Pulse+");

        try {
            var topic = new TopicBuilder().WithComponent(Config.MqttTopicComponent)
                                          .WithNodeId(DeviceNodeId)
                                          .WithObjectId(Config.EntityNodeId);
            // ReSharper disable once UseDiscardAssignment
            await MqttClient.PublishStatusAsync(topic, dictEventArgs.Arguments);

            await Task.Delay(1000);

            topic = new TopicBuilder().WithComponent(Config.MqttTopicComponent)
                                      .WithNodeId(DeviceNodeId)
                                      .WithObjectId(Config.EntityNodeId);
            var args = new Dictionary<string, string> { { "event_type", "unknown" } };
            // ReSharper disable once UseDiscardAssignment
            await MqttClient.PublishStatusAsync(topic, args);
        }
        finally {
            _lock.Release();
            Console.Write("Pulse-");
        }
    }

    private (HaEvent, EventInfo) GetModelEventInfo() {
        if (Config.Model == null) {
            ThrowConfigError("An event requires a model.");
        }

        if (string.IsNullOrWhiteSpace(Config.HaEventMemberName)) {
            ThrowConfigError("An event requires a model's HaEvent member name.");
        }

        var model = Config.Model;
        var haEventInstance =
            model.GetType()
                 .GetField(Config.HaEventMemberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                 ?.GetValue(model) as HaEvent;
        if (haEventInstance == null) {
            var message = $"Could not find public field '{Config.HaEventMemberName}' on model of type '{model.GetType()}'";
            Logger.LogError(message);
            throw new Net2HassMqttConfigurationException(message);
        }

        var eventInfo = haEventInstance.GetType().GetEvent("Event", BindingFlags.Instance | BindingFlags.Public);
        if (eventInfo == null) {
            var message = $"Could not find public event '{Config.HaEventMemberName}.Event' on model of type '{haEventInstance.GetType()}'";
            Logger.LogError(message);
            throw new Net2HassMqttConfigurationException(message);
        }

        return (haEventInstance, eventInfo);
    }

    [DoesNotReturn]
    private void ThrowConfigError(string message) {
        Logger.LogError(message);
        throw new Net2HassMqttConfigurationException(message);
    }
}