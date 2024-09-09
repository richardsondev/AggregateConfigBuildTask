using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace AggregateConfig.Converters
{
    public class BooleanYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(bool).IsAssignableFrom(type);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            // Check if the current token is a scalar (simple value) and can be processed
            if (parser.Current is Scalar scalar)
            {
                var value = scalar.Value.ToLower();

                // Process boolean-like values
                if (value == "true" || value == "yes" || value == "on")
                {
                    return true;
                }
                else if (value == "false" || value == "no" || value == "off")
                {
                    return false;
                }
            }

            // For all other cases, delegate to the default deserialization
            return parser.MoveNext();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            // Only handle boolean serialization, delegate other cases to serializer
            if (value is bool boolValue)
            {
                emitter.Emit(new Scalar(boolValue ? "true" : "false"));
            }
            else
            {
                serializer(value, type);
            }
        }
    }
}
