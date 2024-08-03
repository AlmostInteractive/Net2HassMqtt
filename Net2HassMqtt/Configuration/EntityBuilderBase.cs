using System.ComponentModel;
using System.Reflection;
using NoeticTools.Net2HassMqtt.Configuration.UnitsOfMeasurement;
using NoeticTools.Net2HassMqtt.Entities.Framework;
using NoeticTools.Net2HassMqtt.Exceptions;


namespace NoeticTools.Net2HassMqtt.Configuration;

public abstract class EntityBuilderBase<T, TC>
    where T : EntityBuilderBase<T, TC>
    where TC : EntityConfigBase
{
    protected internal EntityBuilderBase(TC entityConfig)
    {
        EntityConfig = entityConfig;
    }

    public TC EntityConfig { get; }

    /// <summary>
    ///     The entity name that will be seen in Home Assistant.
    /// </summary>
    public T WithFriendlyName(string friendlyName)
    {
        EntityConfig.EntityFriendlyName = friendlyName;
        return (this as T)!;
    }

    public T OnModel(INotifyPropertyChanged model)
    {
        EntityConfig.Model = model;
        return (this as T)!;
    }

    /// <summary>
    ///     The name of the notifying property on the model that provides the entity status value.
    ///     Use <c>nameof(&lt;model&gt;.&lt;statusProperty&gt;)</c>.
    /// </summary>
    public T WithStatusProperty(string propertyName)
    {
        EntityConfig.StatusPropertyName = propertyName;
        return (this as T)!;
    }

    /// <summary>
    ///     The name of the model's event member. Only applicable to event entities.
    /// </summary>
    public T WithEvent(string haEventMemberName, bool pulseMode = false) 
    {
        EntityConfig.HaEventMemberName = haEventMemberName;
        EntityConfig.PulseMode = pulseMode;

        var model = EntityConfig.Model;
        if (model == null)
        {
            throw new Net2HassMqttConfigurationException("WithEvent requires a model");
        }
        var haEventInstance = model.GetType().GetField(haEventMemberName, BindingFlags.Instance  | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(model) as HaEvent;
        if (haEventInstance == null)
        {
            var message = $"Could not find public field '{haEventMemberName}' on model of type '{model.GetType()}'";
            throw new Net2HassMqttConfigurationException(message);
        }

        EntityConfig.EventTypes = haEventInstance.EventTypes.ToArray();
        
        return (this as T)!;
    }

    /// <summary>
    ///     <para>
    ///         The node ID is required.
    ///         The node must be unique within the entity domain on the current device.
    ///         This ID must not change.
    ///     </para>
    ///     <para>It will be combined with the device's ID to form the entity ID.</para>
    /// </summary>
    public T WithNodeId(string nodeId)
    {
        EntityConfig.SetEntityId(nodeId);
        return (this as T)!;
    }

    public T InCategory(EntityCategory category)
    {
        EntityConfig.EntityCategory = category;
        return (this as T)!;
    }

    public T WithAttribute(string? propertyName, string name, string unitOfMeasure = "")
    {
        EntityConfig.Attributes.Add(new AttributeConfiguration(name, propertyName, unitOfMeasure, EntityConfig.Model!));
        return (this as T)!;
    }

    // todo - generate attribute methods with units of measurement && DurationAttributeUoM

    public T WithDurationAttribute(string? propertyName, string name, DurationSensorUoM unitOfMeasure)
    {
        return WithAttribute(propertyName, name, unitOfMeasure.HassUnitOfMeasurement);
    }

    public T WithTemperatureAttribute(string? propertyName, string name, TemperatureSensorUoM unitOfMeasure)
    {
        return WithAttribute(propertyName, name, unitOfMeasure.HassUnitOfMeasurement);
    }

    public T WithHumidityAttribute(string? propertyName, string name, HumiditySensorUoM unitOfMeasure)
    {
        return WithAttribute(propertyName, name, unitOfMeasure.HassUnitOfMeasurement);
    }

    public T WithIcon(string name)
    {
        EntityConfig.Icon = name;
        return (this as T)!;
    }
}