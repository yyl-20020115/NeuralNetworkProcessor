// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.NodeTypeResolvers
{
    public class PreventUnknownTagsNodeTypeResolver : INodeTypeResolver
    {
        public Assembly Assembly { get; protected set; } 
            = typeof(PreventUnknownTagsNodeTypeResolver).Assembly;
        public string NamespacePrefix { get; protected set; }
        public PreventUnknownTagsNodeTypeResolver(string NamespacePrefix="", Assembly? Assembly = null)
        {
            this.Assembly = Assembly??Assembly.GetCallingAssembly();
            this.NamespacePrefix = NamespacePrefix;
        }
        bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
        {
            if (nodeEvent != null && !nodeEvent.Tag.IsEmpty)
            {
                var name = nodeEvent.Tag.Value.TrimStart('!').Trim();
                var nsp = this.NamespacePrefix;
                if (!string.IsNullOrEmpty(nsp))
                {
                    if (!nsp.EndsWith('.'))
                    {
                        nsp += '.';
                    }
                    name = nsp + name;
                }
                var type = this.Assembly.GetType(name);
                if (type != null)
                {
                    currentType = type;
                    return true;
                }
                else
                {
                    throw new YamlException(nodeEvent.Start, nodeEvent.End, $"Encountered an unresolved tag '{nodeEvent.Tag}'");
                }
            }
            return false;
        }
    }
}
