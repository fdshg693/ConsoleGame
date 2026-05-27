using YamlDotNet.Serialization;

namespace GameEngine.Factory
{
    public static class YamlSpecLoader
    {
        public static Dictionary<string, TSpec> Load<TSpec>(
            string yamlPath,
            string specLabelTitle,
            Action<string, TSpec> validate,
            IEqualityComparer<string>? comparer = null)
        {
            try
            {
                if (!File.Exists(yamlPath))
                {
                    throw new FileNotFoundException(
                        $"{specLabelTitle} specs file not found at: {yamlPath}. " +
                        $"Please ensure the file exists in the application directory.");
                }

                string yaml = File.ReadAllText(yamlPath);

                if (string.IsNullOrWhiteSpace(yaml))
                {
                    throw new InvalidOperationException(
                        $"{specLabelTitle} specs file is empty: {yamlPath}");
                }

                var deserializer = new DeserializerBuilder().Build();
                var rawSpecs = deserializer.Deserialize<Dictionary<string, TSpec>>(yaml);

                if (rawSpecs == null || rawSpecs.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No {specLabelTitle.ToLowerInvariant()} specs found in file: {yamlPath}");
                }

                var specs = new Dictionary<string, TSpec>(comparer ?? StringComparer.Ordinal);
                foreach (var kvp in rawSpecs)
                {
                    validate(kvp.Key, kvp.Value);
                    specs[kvp.Key] = kvp.Value;
                }

                Console.WriteLine(
                    $"Successfully loaded {specs.Count} {specLabelTitle.ToLowerInvariant()} specs from {yamlPath}");
                return specs;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (YamlDotNet.Core.YamlException yamlEx)
            {
                throw new InvalidOperationException(
                    $"Failed to parse YAML file: {yamlPath}. Error: {yamlEx.Message}", yamlEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unexpected error loading {specLabelTitle.ToLowerInvariant()} specs from {yamlPath}: {ex.Message}", ex);
            }
        }
    }
}
