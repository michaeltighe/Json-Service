﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JsonWebService
{
    /// <summary>
    /// Represents an http verb and template for json requests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class VerbAttribute : Attribute
    {
        const string REGEX = @"(?<Key>[^?&=]+)=\{(?<Value>[^&]*)\}";
        Dictionary<string, string> dict;

        public VerbAttribute(string UriTemplate)
        {
            this.Description = string.Empty;
            this.Describe = true;
            this.UriTemplate = UriTemplate.StartsWith("/") ? UriTemplate : "/" + UriTemplate;
            MatchCollection mc = Regex.Matches(this.UriTemplate, REGEX, RegexOptions.Singleline);

            if(this.UriTemplate.Contains('?'))
                Path = this.UriTemplate.Substring(0, this.UriTemplate.IndexOf('?'));
            else
                Path = this.UriTemplate;

            dict = mc.OfType<Match>().ToDictionary(k => k.Groups["Value"].Value, v => v.Groups["Key"].Value);
        }
        /// <summary>
        /// Gets the parameter name for a given placeholder
        /// </summary>
        /// <param name="Placeholder">Placeholder name, which should match the parameter name in the actual method.</param>
        /// <returns></returns>
        public string GetParameterName(string Placeholder)
        {
            if(dict.ContainsKey(Placeholder))
                return dict[Placeholder];
            return Placeholder;
        }
        /// <summary>
        /// Gets the placeholder for a given parameter name
        /// </summary>
        /// <param name="ParameterName">The parameter name in the query string.</param>
        /// <returns></returns>
        public string GetPlaceholder(string ParameterName)
        {
            return dict.Where(kvp => kvp.Value.Equals(ParameterName, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault().Key ?? ParameterName;
        }
        /// <summary>
        /// Gets the template for a service method call
        /// </summary>
        public string UriTemplate
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the path for the method call
        /// </summary>
        public string Path
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets and sets a brief description of what the method does, to be shown on self-describe requests
        /// </summary>
        public string Description
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets an example url for the method, to be shown on self-describe requests
        /// </summary>
        public string Example
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets whether or not this method should be included in the service description.  Defaults to true
        /// </summary>
        public bool Describe
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets whether or not to enforce authorization for this particular method call
        /// </summary>
        public bool AllowUnauthorized
        {
            get;
            set;
        }
        /// <summary>
        /// Gets the names of the parameters for the method call
        /// </summary>
        public string[] ParameterNames
        {
            get
            {
                return dict.Values.ToArray();
            }
        }
        /// <summary>
        /// Gets the placeholder values for the method call
        /// </summary>
        public string[] Placeholders
        {
            get
            {
                return dict.Keys.ToArray();
            }
        }
        /// <summary>
        /// Gets the http verb
        /// </summary>
        public abstract string Verb
        {
            get;
        }
    }
}