using System.ComponentModel;
using NoeticTools.Net2HassMqtt.Configuration.UnitsOfMeasurement;
using NoeticTools.Net2HassMqtt.Exceptions;
using NoeticTools.Net2HassMqtt.Mqtt.Topics;


namespace NoeticTools.Net2HassMqtt.Configuration;

public abstract class EntityConfigBase
{
    protected EntityConfigBase(HassDomains domain, string? hassDeviceClass)
    {
        Domain = domain;
        MqttTopicComponent = domain.HassDomainName;
        HassDeviceClassName = hassDeviceClass;
    }

    internal bool CommandHandlerIsRequired { get; init; } = false;

    /// <summary>
    ///     Optional name of the method on <a cref="Model">Model</a> to set the entity's value from received MQTT set.
    ///     One or both of <a cref="StatusPropertyName">GetterPropertyName</a> and
    ///     <a cref="CommandMethodName">GetterPropertyName</a> is required.
    /// </summary>
    public string? CommandMethodName { get; internal set; }

    /// <summary>
    ///     The Home Assistant entity domain (e.g: switch). This is only used for entities (not used by attributes).
    ///     Provides both the HomeAssistant/MQTT snake_case version and the dotnet UpperCamelCase version.
    /// </summary>
    public HassDomains Domain { get; }

    /// <summary>
    ///     Home Assistant device class name.
    /// </summary>
    public string? HassDeviceClassName { get; protected init; }

    public EntityCategory EntityCategory { get; internal set; } = EntityCategory.None;

    /// <summary>
    ///     Name of the property on <a cref="Model"></a> to get the entity's status or attribute value.
    /// </summary>
    public string? StatusPropertyName { get; protected internal set; }

    /// <summary>
    /// The name of the model's HaEvent member. Only applicable to event entities.
    /// </summary>
    public string? HaEventMemberName { get; protected internal set; }

    /// <summary>
    /// The name of the model's HaEvent member. Only applicable to event entities.
    /// </summary>
    public bool PulseMode { get; protected internal set; } = false;

    /// <summary>
    ///     The list of allowed event types. Only applicable to event entities.
    /// </summary>
    public string[] EventTypes { get; protected internal set; } = [];

    /// <summary>
    ///     Optional HASS entity icon.
    /// </summary>
    /// <remarks>
    ///     Home Assistant using material design icons listed <a href="https://pictogrammers.com/library/mdi/">here</a>.
    ///     Naming convention is "mdi:&lt;icon_name&gt;".
    /// </remarks>
    public string? Icon { get; protected internal set; }

    /// <summary>
    ///     The model that has the entity's <a cref="StatusPropertyName">StatusPropertyName</a> property.
    /// </summary>
    public INotifyPropertyChanged? Model { get; internal set; }

    /// <summary>
    ///     The entity's Home Assistant domain (and MQTT component topic) such as 'switch' or 'sensor'.
    /// </summary>
    public string MqttTopicComponent { get; internal set; }

    /// <summary>
    ///     Human friendly name for entity name in HASS.
    /// </summary>
    public string EntityFriendlyName { get; protected internal set; } = "";

    /// <summary>
    ///     <para>
    ///         The entity's node ID.
    ///         This will be combined with the device's ID to form both unique_id and object_id.
    ///         This must be unique within entities of the same domain on the device
    ///         and must not change.
    ///     </para>
    ///     <para>
    ///         See 'object_id' <a href="https://www.home-assistant.io/integrations/mqtt/">here</a>.
    ///     </para>
    /// </summary>
    public string EntityNodeId { get; private set; } = "";

    public UnitOfMeasurement? UnitOfMeasurement { get; internal set; }

    internal List<AttributeConfiguration> Attributes { get; } = [];

    internal void SetEntityId(string entityId)
    {
        EntityNodeId = entityId.ToMqttTopicSnakeCase();
    }

    public virtual void Validate()
    {
        if (Model == null)
        {
            throw new Net2HassMqttConfigurationException($"An entity model is required. Type: {GetType().Name}");
        }

        var hasGetter = !string.IsNullOrWhiteSpace(StatusPropertyName);
        var hasSetter = !string.IsNullOrWhiteSpace(CommandMethodName);
        var hasEvent = !string.IsNullOrWhiteSpace(HaEventMemberName);
        if (!hasGetter && !hasSetter && !hasEvent)
        {
            throw new Net2HassMqttConfigurationException("One or more of statusPropertyName, commandMethodName, or eventMemberName is required.");
        }

        if (string.IsNullOrWhiteSpace(EntityNodeId))
        {
            throw new Net2HassMqttConfigurationException($"An EntityId is required. Type: {GetType().Name}");
        }

        if (string.IsNullOrEmpty(EntityFriendlyName))
        {
            throw new Net2HassMqttConfigurationException($"Name is required. Type: {GetType().Name}");
        }

        if (UnitOfMeasurement == null)
        {
            throw new Net2HassMqttConfigurationException($"A UnitOfMeasurement is required. Type: {GetType().Name}");
        }

        if (CommandHandlerIsRequired && string.IsNullOrWhiteSpace(CommandMethodName))
        {
            throw new Net2HassMqttConfigurationException($"A CommandMethodName is required for entities of this type. Type: {GetType().Name}");
        }
    }
}