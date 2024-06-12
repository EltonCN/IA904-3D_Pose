using IA904_3DPose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    [RequireComponent(typeof(FixedLengthScenario))]
    public class ScenarioController : MonoBehaviour
    {
        private FixedLengthScenario _Scenario;

        private void Awake()
        {
            _Scenario = GetComponent<FixedLengthScenario>();

            if (DataManager.Instance != null)
            {
                var scenarioJson = Resources.Load<TextAsset>(DataManager.Instance.Selected_Scenario);
                Debug.Log($"scenarioJson: {scenarioJson}");

                _Scenario.configuration = scenarioJson;
                DeserializeConfiguration();
            }
        }

        private void Start()
        {
            _Scenario.enabled = true;
        }

        /// <summary>
        /// Overwrites this scenario's randomizer settings and scenario constants from a JSON serialized configuration
        /// </summary>
        private void DeserializeConfiguration()
        {
            if (_Scenario.configuration != null)
                ScenarioSerializer.Deserialize(_Scenario, _Scenario.configuration.text);
        }
    }

    static class ScenarioSerializer
    {
        #region Deserialization
        public static void Deserialize(ScenarioBase scenario, string json)
        {
            var jsonData = JObject.Parse(json);
            if (jsonData.ContainsKey("constants"))
                DeserializeConstants(scenario.genericConstants, (JObject)jsonData["constants"]);
            if (jsonData.ContainsKey("randomizers"))
                DeserializeTemplateIntoScenario(
                    scenario, jsonData["randomizers"].ToObject<TemplateConfigurationOptions>());
        }

        static void DeserializeConstants(ScenarioConstants constants, JObject constantsData)
        {
            JsonUtility.FromJsonOverwrite(constantsData.ToString(), constants);
        }

        static void DeserializeTemplateIntoScenario(ScenarioBase scenario, TemplateConfigurationOptions template)
        {
            DeserializeRandomizers(scenario.randomizers, template.randomizerGroups);
        }

        static void DeserializeRandomizers(IEnumerable<Randomizer> randomizers, List<Group> groups)
        {
            var randomizerTypeMap = new Dictionary<string, Randomizer>();
            foreach (var randomizer in randomizers)
                randomizerTypeMap.Add(randomizer.GetType().Name, randomizer);

            foreach (var randomizerData in groups)
            {
                if (!randomizerTypeMap.ContainsKey(randomizerData.randomizerId))
                    continue;
                var randomizer = randomizerTypeMap[randomizerData.randomizerId];
                DeserializeRandomizer(randomizer, randomizerData);
            }
        }

        static void DeserializeRandomizer(Randomizer randomizer, Group randomizerData)
        {
            if (randomizerData.state != null)
            {
                randomizer.enabled = randomizerData.state.enabled;
                randomizer.enabledStateCanBeSwitchedByUser = randomizerData.state.canBeSwitchedByUser;
            }

            foreach (var pair in randomizerData.items)
            {
                var field = randomizer.GetType().GetField(pair.Key);
                if (field == null)
                    continue;
                if (pair.Value is Parameter parameterData)
                    DeserializeParameter((Randomization.Parameters.Parameter)field.GetValue(randomizer), parameterData);
                else
                    DeserializeScalarValue(randomizer, field, (Scalar)pair.Value);
            }
        }

        static void DeserializeParameter(Randomization.Parameters.Parameter parameter, Parameter parameterData)
        {
            foreach (var pair in parameterData.items)
            {
                var field = parameter.GetType().GetField(pair.Key);
                if (field == null)
                    continue;
                if (pair.Value is SamplerOptions samplerOptions)
                    field.SetValue(parameter, DeserializeSampler(samplerOptions.defaultSampler));
                else
                    DeserializeScalarValue(parameter, field, (Scalar)pair.Value);
            }
        }

        static ISampler DeserializeSampler(ISamplerOption samplerOption)
        {
            if (samplerOption is ConstantSampler constantSampler)
                return new Samplers.ConstantSampler
                {
                    value = (float)constantSampler.value,
                    minAllowed = constantSampler.limits != null ? (float)constantSampler.limits.min : 0,
                    maxAllowed = constantSampler.limits != null ? (float)constantSampler.limits.max : 0,
                    shouldCheckValidRange = constantSampler.limits != null
                };
            if (samplerOption is UniformSampler uniformSampler)
                return new Samplers.UniformSampler
                {
                    range = new FloatRange
                    {
                        minimum = (float)uniformSampler.min,
                        maximum = (float)uniformSampler.max,
                    },
                    minAllowed = uniformSampler.limits != null ? (float)uniformSampler.limits.min : 0,
                    maxAllowed = uniformSampler.limits != null ? (float)uniformSampler.limits.max : 0,
                    shouldCheckValidRange = uniformSampler.limits != null
                };
            if (samplerOption is NormalSampler normalSampler)
                return new Samplers.NormalSampler
                {
                    range = new FloatRange
                    {
                        minimum = (float)normalSampler.min,
                        maximum = (float)normalSampler.max
                    },
                    mean = (float)normalSampler.mean,
                    standardDeviation = (float)normalSampler.stddev,
                    minAllowed = normalSampler.limits != null ? (float)normalSampler.limits.min : 0,
                    maxAllowed = normalSampler.limits != null ? (float)normalSampler.limits.max : 0,
                    shouldCheckValidRange = normalSampler.limits != null
                };
            throw new ArgumentException($"Cannot deserialize unsupported sampler type {samplerOption.GetType()}");
        }

        static void DeserializeScalarValue(object obj, FieldInfo field, Scalar scalar)
        {
            var rangeAttributes = field.GetCustomAttributes(typeof(RangeAttribute));
            RangeAttribute rangeAttribute = null;

            if (rangeAttributes.Any())
            {
                rangeAttribute = (RangeAttribute)rangeAttributes.First();
            }

            var readScalar = ReadScalarValue(obj, scalar);
            var tolerance = 0.00001f;

            if (readScalar.Item1 is double num)
            {
                if (rangeAttribute != null)
                {
                    if (readScalar.Item2 != null &&
                        (Math.Abs(rangeAttribute.min - readScalar.Item2.min) > tolerance || Math.Abs(rangeAttribute.max - readScalar.Item2.max) > tolerance))
                    {
                        //the field has a range attribute and the json has a limits block for this field, but the numbers don't match
                        Debug.LogError($"The limits provided in the Scenario JSON for the field \"{field.Name}\" of \"{obj.GetType().Name}\" do not match this field's range set in the code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");
                    }
                    else if (readScalar.Item2 == null)
                    {
                        //the field has a range attribute but the json has no limits block for this field
                        Debug.LogError($"The provided Scenario JSON specifies limits for the field \"{field.Name}\" of \"{obj.GetType().Name}\", while the field has no Range attribute in the code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");
                    }

                    if (num < rangeAttribute.min || num > rangeAttribute.max)
                    {
                        Debug.LogError($"The provided value for the field \"{field.Name}\" of \"{obj.GetType().Name}\" exceeds the allowed valid range. Clamping to valid range.");
                        var clamped = Mathf.Clamp((float)num, rangeAttribute.min, rangeAttribute.max);
                        field.SetValue(obj, Convert.ChangeType(clamped, field.FieldType));
                    }
                    else
                        field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
                }
                else
                {
                    if (readScalar.Item2 != null)
                        //the field does not have a range attribute but the json has a limits block for this field
                        Debug.LogError($"The provided Scenario JSON specifies limits for the field \"{field.Name}\" of \"{obj.GetType().Name}\", but the field has no Range attribute in code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");

                    field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
                }
            }
            else
            {
                field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
            }
        }

        static (object, Limits) ReadScalarValue(object obj, Scalar scalar)
        {
            object value;
            Limits limits = null;
            if (scalar.value is StringScalarValue stringValue)
                value = stringValue.str;
            else if (scalar.value is BooleanScalarValue booleanValue)
                value = booleanValue.boolean;
            else if (scalar.value is DoubleScalarValue doubleValue)
            {
                value = doubleValue.num;
                limits = doubleValue.limits;
            }
            else
                throw new ArgumentException(
                    $"Cannot deserialize unsupported scalar type {scalar.value.GetType()}");

            return (value, limits);
        }

        #endregion
    }

    #region Interfaces
    interface IGroupItem { }

    interface IParameterItem { }

    interface ISamplerOption { }

    interface IScalarValue { }
    #endregion

    #region GroupedObjects
    class TemplateConfigurationOptions
    {
        public List<Group> randomizerGroups = new List<Group>();
    }

    class StandardMetadata
    {
        public string name = string.Empty;
        public string description = string.Empty;
        public string imageLink = string.Empty;
    }

    class RandomizerStateData
    {
        public bool enabled;
        public bool canBeSwitchedByUser;
    }

    class Limits
    {
        public double min;
        public double max;
    }

    class Group
    {
        public string randomizerId;
        public StandardMetadata metadata = new StandardMetadata();
        public RandomizerStateData state = null;
        [JsonConverter(typeof(GroupItemsConverter))]
        public Dictionary<string, IGroupItem> items = new Dictionary<string, IGroupItem>();
    }

    class Parameter : IGroupItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        [JsonConverter(typeof(ParameterItemsConverter))]
        public Dictionary<string, IParameterItem> items = new Dictionary<string, IParameterItem>();
    }
    #endregion

    #region SamplerOptions
    [JsonConverter(typeof(SamplerOptionsConverter))]
    class SamplerOptions : IParameterItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        public ISamplerOption defaultSampler;
    }

    class UniformSampler : ISamplerOption
    {
        public double min;
        public double max;
        public Limits limits;
    }

    class NormalSampler : ISamplerOption
    {
        public double min;
        public double max;
        public double mean;
        public double stddev;
        public Limits limits;
    }

    class ConstantSampler : ISamplerOption
    {
        public double value;
        public Limits limits;
    }
    #endregion

    #region ScalarValues
    [JsonConverter(typeof(ScalarConverter))]
    class Scalar : IGroupItem, IParameterItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        public IScalarValue value;
    }

    class StringScalarValue : IScalarValue
    {
        public string str;
    }

    class DoubleScalarValue : IScalarValue
    {
        public double num;
        public Limits limits;
    }

    class BooleanScalarValue : IScalarValue
    {
        [JsonProperty("bool")]
        public bool boolean;
    }
    #endregion

    class SamplerOptionsConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SamplerOptions);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var options = (SamplerOptions)value;
            var output = new JObject { ["metadata"] = JObject.FromObject(options.metadata) };

            string key;
            if (options.defaultSampler is ConstantSampler)
                key = "constant";
            else if (options.defaultSampler is UniformSampler)
                key = "uniform";
            else if (options.defaultSampler is NormalSampler)
                key = "normal";
            else
                throw new TypeAccessException($"Cannot serialize type ${options.defaultSampler.GetType()}");
            output[key] = JObject.FromObject(options.defaultSampler, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            output.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var samplerOption = new SamplerOptions { metadata = jsonObject["metadata"].ToObject<StandardMetadata>() };

            if (jsonObject.ContainsKey("constant"))
                samplerOption.defaultSampler = jsonObject["constant"].ToObject<ConstantSampler>();
            else if (jsonObject.ContainsKey("uniform"))
                samplerOption.defaultSampler = jsonObject["uniform"].ToObject<UniformSampler>();
            else if (jsonObject.ContainsKey("normal"))
                samplerOption.defaultSampler = jsonObject["normal"].ToObject<NormalSampler>();
            else
                throw new KeyNotFoundException("No valid SamplerOption key type found");

            return samplerOption;
        }
    }

    class ScalarConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Scalar);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var value = (JObject)jsonObject["value"];
            var scalar = new Scalar { metadata = jsonObject["metadata"].ToObject<StandardMetadata>() };

            if (value.ContainsKey("str"))
                scalar.value = new StringScalarValue { str = value["str"].Value<string>() };
            else if (value.ContainsKey("num"))
            {
                Limits limits = null;
                if (value.ContainsKey("limits"))
                {
                    limits = value["limits"].ToObject<Limits>();
                }
                scalar.value = new DoubleScalarValue { num = value["num"].Value<double>(), limits = limits };
            }
            else if (value.ContainsKey("bool"))
                scalar.value = new BooleanScalarValue { boolean = value["bool"].Value<bool>() };
            else
                throw new KeyNotFoundException("No valid ScalarValue key type found");

            return scalar;
        }
    }

    class GroupItemsConverter : JsonConverter
    {
        public override bool CanWrite => true;

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IGroupItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            var output = new JObject();
            var groupItems = (Dictionary<string, IGroupItem>)value;
            foreach (var itemKey in groupItems.Keys)
            {
                var itemValue = groupItems[itemKey];
                var newObj = new JObject();
                if (itemValue is Parameter)
                    newObj["param"] = JObject.FromObject(itemValue);
                else
                    newObj["scalar"] = JObject.FromObject(itemValue, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                output[itemKey] = newObj;
            }
            output.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var groupItems = new Dictionary<string, IGroupItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                IGroupItem groupItem;
                if (value.ContainsKey("param"))
                    groupItem = serializer.Deserialize<Parameter>(value["param"].CreateReader());
                else if (value.ContainsKey("scalar"))
                    groupItem = serializer.Deserialize<Scalar>(value["scalar"].CreateReader());
                else
                    throw new KeyNotFoundException("No GroupItem key found");
                groupItems.Add(property.Name, groupItem);
            }
            return groupItems;
        }
    }

    class ParameterItemsConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IParameterItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            var output = new JObject();
            var parameterItems = (Dictionary<string, IParameterItem>)value;
            foreach (var itemKey in parameterItems.Keys)
            {
                var itemValue = parameterItems[itemKey];
                var newObj = new JObject();
                if (itemValue is SamplerOptions)
                    newObj["samplerOptions"] = JObject.FromObject(itemValue);
                else
                    newObj["scalar"] = JObject.FromObject(itemValue);
                output[itemKey] = newObj;
            }
            output.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var parameterItems = new Dictionary<string, IParameterItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                IParameterItem parameterItem;
                if (value.ContainsKey("samplerOptions"))
                    parameterItem = serializer.Deserialize<SamplerOptions>(value["samplerOptions"].CreateReader());
                else if (value.ContainsKey("scalar"))
                    parameterItem = serializer.Deserialize<Scalar>(value["scalar"].CreateReader());
                else
                    throw new KeyNotFoundException("No ParameterItem key found");
                parameterItems.Add(property.Name, parameterItem);
            }
            return parameterItems;
        }
    }
}