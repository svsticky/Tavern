using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Represents a setting within the system, which consists of a name and a corresponding value. This entity is used to manage various configuration settings for the application, allowing for easy retrieval and modification of settings as needed. The Name property serves as the unique identifier for each setting, while the Value property holds the actual value associated with that setting. This structure allows for flexible and dynamic management of application settings, enabling administrators to customize and configure the behavior of the system without requiring code changes or redeployments.
/// </summary>
[PrimaryKey(nameof(Name))]
public class Setting
{
    /// <summary>
    /// The unique name of the setting, which serves as the primary key for this entity. The Name property is used to identify and retrieve specific settings within the system, allowing for easy access and management of configuration settings. Each setting must have a unique name to ensure that it can be accurately referenced and modified without ambiguity.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The value associated with this setting. The Value property holds the actual configuration value for the setting, which can be of any type (e.g., string, number, boolean) depending on the specific setting being represented. This property allows for flexible storage of various types of configuration data, enabling administrators to easily manage and modify settings as needed to customize the behavior of the system. The Value property is required to ensure that each setting has an associated value, which is essential for the proper functioning of the application based on the defined settings.
    /// </summary>
    public string Value { get; set; } = null!;
}