using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.Services;

internal sealed class FilterConfigurationRepository
{
    private readonly string _filePath;

    public FilterConfigurationRepository()
    {
        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DEFOR Tools",
            "LiveFilter");

        Directory.CreateDirectory(configDirectory);
        _filePath = Path.Combine(configDirectory, "saved-configurations.xml");
    }

    public List<SavedFilterConfiguration> LoadAll()
    {
        if (!File.Exists(_filePath))
        {
            return new List<SavedFilterConfiguration>();
        }

        var fileInfo = new FileInfo(_filePath);
        if (fileInfo.Length == 0)
        {
            return new List<SavedFilterConfiguration>();
        }

        try
        {
            using (var stream = File.OpenRead(_filePath))
            {
                var serializer = new XmlSerializer(typeof(SavedFilterConfigurationStore));
                var store = serializer.Deserialize(stream) as SavedFilterConfigurationStore;
                return store != null ? store.Configurations : new List<SavedFilterConfiguration>();
            }
        }
        catch (InvalidOperationException)
        {
            return new List<SavedFilterConfiguration>();
        }
        catch (XmlException)
        {
            return new List<SavedFilterConfiguration>();
        }
    }

    public void SaveAll(IEnumerable<SavedFilterConfiguration> configurations)
    {
        var store = new SavedFilterConfigurationStore
        {
            Configurations = configurations.OrderBy(configuration => configuration.Name).ToList()
        };

        using (var stream = File.Create(_filePath))
        {
            var serializer = new XmlSerializer(typeof(SavedFilterConfigurationStore));
            serializer.Serialize(stream, store);
        }
    }
}
