using System;

namespace AggregateConfigBuildTask.Tests.Unit
{
    internal static class DemoData
    {
        public static string GetSampleDataForType(string type)
        {
            return type switch
            {
                "JSON" => """
{
  "options": [
    {
      "name": "Option 1",
      "description": "First option",
      "isTrue": true,
      "number": 100,
      "nested": [
        {
          "name": "Nested option 1",
          "description": "Nested first option",
          "isTrue": true,
          "number": 1001
        },
        {
          "name": "Nested option 2",
          "description": "Nested second option"
        }
      ]
    }
  ]
}
""",
                "ARM" => """
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "options": {
      "type": "object",
      "value": {
        "name": "Option 1",
        "description": "First option",
        "isTrue": true,
        "number": 100,
        "nested": [
          {
            "name": "Nested option 1",
            "description": "Nested first option",
            "isTrue": true,
            "number": 1002
          },
          {
            "name": "Nested option 2",
            "description": "Nested second option"
          }
        ]
      }
    }
  }
}
""",
                "YML" => @"options:
- name: Option 1
  description: First option
  isTrue: true
  number: 100
  nested:
  - name: Nested option 1
    description: Nested first option
    isTrue: true
    number: 1003
  - name: Nested option 2
    description: Nested second option
",
                "TOML" => """
[[options]]
name = "Option 1"
description = "First option"
isTrue = true
number = 100

[[options.nested]]
name = "Nested option 1"
description = "Nested first option"
isTrue = true
number = 1004

[[options.nested]]
name = "Nested option 2"
description = "Nested second option"
""",
                _ => throw new InvalidOperationException($"Unknown type: {type}")
            };
        }
    }
}
