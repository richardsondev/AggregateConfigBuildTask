using System;
using System.Diagnostics.CodeAnalysis;

namespace AggregateConfigBuildTask.Tests.Unit
{
    internal static class DemoData
    {
        [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Test data method containing sample data strings")]
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
      "type": "array",
      "value": [
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
              "number": 1002
            },
            {
              "name": "Nested option 2",
              "description": "Nested second option"
            }
          ]
        }
      ]
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

                "XML" => """
<?xml version="1.0" encoding="utf-8"?>
<root>
  <options>
    <options>
      <name>Option 1</name>
      <description>First option</description>
      <isTrue>True</isTrue>
      <number>100</number>
      <nested>
        <nested>
          <name>Nested option 1</name>
          <description>Nested first option</description>
          <isTrue>True</isTrue>
          <number>1002</number>
        </nested>
        <nested>
          <name>Nested option 2</name>
          <description>Nested second option</description>
        </nested>
      </nested>
    </options>
  </options>
</root>
""",

                _ => throw new InvalidOperationException($"Unknown type: {type}")
            };
        }
    }
}