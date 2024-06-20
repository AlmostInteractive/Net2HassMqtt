﻿using Microsoft.Extensions.Logging;
using NoeticTools.Net2HassMqtt.Configuration;
using NoeticTools.Net2HassMqtt.Mqtt;
using NoeticTools.Net2HassMqtt.Mqtt.Topics;


namespace NoeticTools.Net2HassMqtt.Entities.Framework;

internal sealed class EntityFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly INet2HassMqttClient _mqttClient;

    public EntityFactory(INet2HassMqttClient mqttClient, ILoggerFactory loggerFactory)
    {
        _mqttClient = mqttClient;
        _loggerFactory = loggerFactory;
    }

    internal IMqttPublisher Create(string entityUniqueId, EntityConfigBase entityConfig, DeviceConfig deviceConfig)
    {
        var lookup = new Dictionary<string, Func<EntityConfigBase, string, string, IMqttPublisher>>
        {
            { HassDomains.BinarySensor.HassDomainName, CreateBinarySensor },
            { HassDomains.Cover.HassDomainName, CoverButton },
            { HassDomains.Humidifier.HassDomainName, EntityNotSupported }, //todo
            { HassDomains.Number.HassDomainName, CreateNumberEntity },
            { HassDomains.Sensor.HassDomainName, CreateSensorEntity },
            { HassDomains.Switch.HassDomainName, CreateSwitchEntity },
            { HassDomains.Update.HassDomainName, EntityNotSupported }, // todo
            { HassDomains.Valve.HassDomainName, CreateValveEntity }
        };
        entityConfig.Validate();
        return lookup[entityConfig.Domain.HassDomainName](entityConfig, entityUniqueId, deviceConfig.DeviceId.ToMqttTopicSnakeCase());
    }

    private IMqttPublisher CreateNumberEntity(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new NumberEntity((NumberConfig)config, entityUniqueId, deviceNodeId, _mqttClient, CreateLogger<NumberEntity>(config));
    }

    private IMqttPublisher CoverButton(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new CoverEntity((CoverConfig)config, entityUniqueId, deviceNodeId, _mqttClient, CreateLogger<CoverEntity>(config));
    }

    private IMqttPublisher CreateBinarySensor(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new BinarySensorEntity((BinarySensorConfig)config, entityUniqueId, deviceNodeId, _mqttClient,
                                      CreateLogger<BinarySensorEntity>(config));
    }

    private ILogger CreateLogger<T>(EntityConfigBase config)
    {
        return _loggerFactory.CreateLogger($"{typeof(T).FullName}({config.EntityFriendlyName})");
    }

    private IMqttPublisher CreateSensorEntity(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new SensorEntity((SensorConfig)config, entityUniqueId, deviceNodeId, _mqttClient, CreateLogger<SensorEntity>(config));
    }

    private IMqttPublisher CreateSwitchEntity(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new SwitchEntity((SwitchConfig)config, entityUniqueId, deviceNodeId, _mqttClient, CreateLogger<SwitchEntity>(config));
    }

    private IMqttPublisher CreateValveEntity(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        return new ValveEntity((ValveConfig)config, entityUniqueId, deviceNodeId, _mqttClient, CreateLogger<ValveEntity>(config));
    }

    private IMqttPublisher EntityNotSupported(EntityConfigBase config, string entityUniqueId, string deviceNodeId)
    {
        throw new InvalidOperationException($"Entity domain '{config.Domain.HassDomainName}' not yet implemented.");
    }
}