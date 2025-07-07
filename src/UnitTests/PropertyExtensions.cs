using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AggregateConfigBuildTask.Tests.Unit
{
    internal static class PropertyExtensions
    {
        /// <summary>
        /// Creates an array of TaskItems from a dictionary of key-value pairs.
        /// Supports both legacy format (key=value in ItemSpec) and new format (key in ItemSpec, value in metadata).
        /// </summary>
        /// <param name="properties">Dictionary of key-value pairs to be converted to TaskItems.</param>
        /// <param name="legacyAdditionalProperties">If true, uses the legacy format for all items; otherwise, uses new format.</param>
        /// <returns>An array of TaskItems.</returns>
        public static ITaskItem[] CreateTaskItems(this IDictionary<string, string> properties, bool legacyAdditionalProperties)
        {
            return properties.Select((q) =>
            {
                if (legacyAdditionalProperties)
                {
                    // Legacy format: "Key=Value" in ItemSpec
                    return new TaskItem($"{q.Key}={q.Value}");
                }

                // New format: Key in ItemSpec, Value as metadata
                var taskItem = new TaskItem(q.Key);
                taskItem.SetMetadata("Value", q.Value);
                return taskItem;
            }).ToArray();
        }
    }
}
