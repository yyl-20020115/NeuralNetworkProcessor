using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YamlDotNet.Serialization
{
    /// <summary>
    /// Instructs the YamlSerializer to serialize the public field or public read/write property with value inside only by links.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class YamlHasLinksAttribute : Attribute { }

}
