﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    /// <summary>
    /// Represents the GET http verb for json service requests.
    /// </summary>
    public class GetAttribute : VerbAttribute {
        /// <summary>
        /// Sets the request uri template for GET requests that this method will accept.
        /// </summary>
        /// <param name="UriTemplate">Sets the template for a service method call</param>
        public GetAttribute(string UriTemplate)
            : base(UriTemplate) {
        }
        /// <summary>
        /// Gets the http verb (GET)
        /// </summary>
        public override string Verb {
            get {
                return "GET";
            }
        }
    }
}
