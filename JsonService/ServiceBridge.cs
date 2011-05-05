﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace JsonWebService {
    /// <summary>
    /// Acts as a bridge between the query and the service method
    /// </summary>
    class ServiceBridge {
        /// <summary>
        /// Returns whether or not this method matches the method that was requested.
        /// </summary>
        /// <param name="path">Path the client requested</param>
        /// <param name="verb">The http verb used to invoke the method</param>
        /// <param name="qsKeys">Query string keys the client specified</param>
        /// <returns></returns>
        public bool IsMatch(string path, string verb, string[] qsKeys) {
            return Attribute.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase) &&
                GetRequiredParameters().All(p => qsKeys.Contains(p, StringComparer.InvariantCultureIgnoreCase)) &&
                Attribute.Verb.Equals(verb, StringComparison.InvariantCultureIgnoreCase);
        }
        string[] GetRequiredParameters() {
            return (from p in MethodInfo.GetParameters()
                    let k = Attribute.GetParameterName(p.Name)
                    where k != null && p.DefaultValue == System.DBNull.Value
                    select k).ToArray();
        }
        /// <summary>
        /// Serves as a hash function to determine template collisions.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            int hash = Attribute.Path.ToLower().GetHashCode();
            hash ^= Attribute.Verb.ToLower().GetHashCode();
            for(int i = 0; i < Attribute.ParameterNames.Length; i++)
                hash ^= Attribute.ParameterNames[i].GetHashCode();
            return hash;
        }
        /// <summary>
        /// Returns a service method parameter information based on its query string parameter name.
        /// </summary>
        /// <param name="ParameterName">Parameter name in the query string.</param>
        /// <returns></returns>
        public ParameterInfo GetParameterInfo(string ParameterName) {
            return MethodInfo.GetParameters().SingleOrDefault(p => p.Name == Attribute.GetPlaceholder(ParameterName));
        }
        /// <summary>
        /// Maps query string values to the service method parameters.
        /// </summary>
        /// <param name="qs">Query string the client passed in.</param>
        /// <param name="postedDocument">The json document posted by the client, if any</param>
        /// <exception cref="System.ArgumentException">Missing required parameter or parameter is invalid type</exception>
        /// <returns>Item1: parameter values, Item2: parameter names, Item3: logging strings</returns>
        public Tuple<object[], string[], string[]> MapParameters(System.Collections.Specialized.NameValueCollection qs, dynamic postedDocument) {
            var parms = MethodInfo.GetParameters().OrderBy(p => p.Position).ToArray();
            var result = Tuple.Create(
                new object[parms.Length],
                parms.Select(p => p.Name).ToArray(),
                new string[parms.Length]
            );

            for(int i = 0; i < parms.Length; i++) {
                var pm = parms[i];
                bool hasDefault = pm.DefaultValue != System.DBNull.Value;
                string key = Attribute.GetParameterName(pm.Name);

                if(key == null && Attribute is EntityAttribute) {
                    string pd = ((EntityAttribute)Attribute).EntityDocument;
                    if(pm.Name.Equals(pd))
                        result.Item1[i] = postedDocument;
                } else if(!qs.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase) && !hasDefault) {                    
                    throw ParameterError(string.Format("{0} is required.", key), key, null, pm.ParameterType, new Exception("Missing required parameter"));
                } else {
                    string val = qs[key];

                    if(val == null && hasDefault) {
                        result.Item1[i] = pm.DefaultValue;
                    } else {
                        try {
                            result.Item1[i] = Convert.ChangeType(val, pm.ParameterType);
                        } catch(Exception e) {
                            throw ParameterError(string.Format("Cannot convert value '{0}' to {1}", val, pm.ParameterType.Name.ToLower()), key, val, pm.ParameterType, e);
                        }
                    }

                    result.Item3[i] = string.Format("{0}={1}", result.Item2[i], result.Item1[i]);
                }
            }

            return result;
        }
        /// <summary>
        /// Returns the Uri for an example use, if an example was specified.
        /// </summary>
        /// <param name="ServiceUri">Uri the service is running on</param>
        /// <returns></returns>
        public Uri GetExampleUri(Uri ServiceUri) {
            Uri result = null;

            if(string.IsNullOrWhiteSpace(Attribute.Example) || !Uri.TryCreate(ServiceUri, Attribute.Example, out result))
                return null;           
                
            return result;
        }

        ArgumentException ParameterError(string message, string parameter, object value, Type expected, Exception inner) {
            ArgumentException ae = new ArgumentException(message, parameter, inner);
            ae.Data["value"] = value;
            ae.Data["expected_type"] = expected;
            return ae;
        }

        /// <summary>
        /// Gets the metadata for the service method
        /// </summary>
        public MethodInfo MethodInfo {
            get;
            set;
        }
        /// <summary>
        /// Gets the attribute information for the service method
        /// </summary>
        public VerbAttribute Attribute {
            get;
            set;
        }
        /// <summary>
        /// Gets the total number of VerbAttributes decorating the method; only 1 is supported at this time.
        /// </summary>
        public int AttributeCount {
            get;
            set;
        }
        /// <summary>
        /// Gets the qualified name of the method, formatted as ClassName.MethodName
        /// </summary>
        public string QualifiedName {
            get {
                if(this.MethodInfo != null) {
                    return string.Format("{0}.{1}", this.MethodInfo.DeclaringType.Name, this.MethodInfo.Name);
                }
                return null;
            }
        }
        /// <summary>
        /// Returns all placeholders defined in a UriTemplate that have no matching method parameter.
        /// </summary>
        /// <returns></returns>
        public string[] InvalidPlaceholders() {
            return Attribute.Placeholders.Except(MethodInfo.GetParameters().Select(p => p.Name), StringComparer.InvariantCultureIgnoreCase).ToArray();
        }
    }
}
