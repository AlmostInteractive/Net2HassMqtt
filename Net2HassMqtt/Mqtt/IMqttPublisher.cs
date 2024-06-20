﻿using NoeticTools.Net2HassMqtt.Configuration;


namespace NoeticTools.Net2HassMqtt.Mqtt;

internal interface IMqttPublisher
{
    /// <summary>
    ///     Can write received MQTT entity values to the model.
    /// </summary>
    bool CanCommand { get; }

    /// <summary>
    ///     Can read entity value from the model.
    /// </summary>
    bool CanRead { get; }

    Task PublishConfigAsync(DeviceConfig deviceConfig);
    Task PublishStateAsync();
    Task StartAsync();
    Task StopAsync();
}